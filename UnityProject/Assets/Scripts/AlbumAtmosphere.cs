using UnityEngine;
using UnityEngine.UI;

// One untextured UI mesh per stage. Shapes stay inside the picture, away from its caption.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class AlbumAtmosphere : MaskableGraphic
{
    public enum Motif { Petal, Pond, Maple, Snow, Pearl, Star }
    Motif motif;
    public AlbumAtmosphere() { useLegacyMeshGeneration = false; }
    float phase, nextFrame;
    static readonly Color[] Inks = {
        new Color32(185,54,106,255),new Color32(39,107,86,255),new Color32(185,67,30,255),
        new Color32(56,99,169,255),new Color32(165,105,37,255),new Color32(111,63,167,255)
    };
    static readonly Color[] Highlights = {
        new Color32(255,218,231,255),new Color32(252,255,210,255),new Color32(255,194,118,255),
        new Color32(245,252,255,255),new Color32(255,233,169,255),new Color32(241,221,255,255)
    };

    public void Select(Motif value)
    {
        motif = value;
        raycastTarget = false;
        Sample(0);
    }

    public void Sample(float seconds)
    {
        phase = seconds;
        SetVerticesDirty();
    }

    void Update()
    {
        if (Time.unscaledTime < nextFrame) return;
        nextFrame = Time.unscaledTime + 1f/30;
        Sample(Time.unscaledTime);
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        float unit = Mathf.Min(r.width,r.height);
        if (unit <= 0) return;
        Color ink = Inks[(int)motif];
        Color light = Highlights[(int)motif];
        bool hidden = motif == Motif.Pearl || motif == Motif.Star;
        bool marginLights = hidden && r.height > r.width;
        if (hidden && !marginLights)
            Foil(mesh,r,Mathf.Repeat(phase / 8.6f + (motif == Motif.Star ? .45f : 0),1),light);
        for (int i=0;i<6;i++)
        {
            float t = Mathf.Repeat(phase*(.073f+.009f*(i%3)) + i*.618f,1);
            float wave = Mathf.Sin(Mathf.PI*t);
            float lane = Mathf.Repeat(i*.382f+.11f,1);
            float drift = .036f*Mathf.Sin(2*Mathf.PI*t+i);
            float x = (i%2 == 0 ? .10f : .76f) + .14f*lane + drift;
            if (marginLights) x = (i%2 == 0 ? .06f : .80f) + .12f*lane + .50f*drift;
            float y = motif == Motif.Pond ? .18f+t*.71f : .94f-t*.88f;
            Vector2 c = new(r.xMin+r.width*x,r.yMin+r.height*y);
            float size = unit*(.016f + .003f*(i%3));
            ink.a = .82f*wave;
            light.a = .90f*wave;
            switch (motif)
            {
                case Motif.Petal:
                    Leaf(mesh,c,size*1.35f,t*135+i*23,ink,false);
                    Leaf(mesh,c,size*.77f,t*135+i*23,light,false);
                    break;
                case Motif.Maple:
                    Leaf(mesh,c,size*1.55f,t*155+i*31,ink,true);
                    Leaf(mesh,c,size*.84f,t*155+i*31,light,true);
                    break;
                case Motif.Snow:
                    for(int k=0;k<3;k++)
                    {
                        float angle=k*60+i*7+phase*7;
                        Diamond(mesh,c,size,size*.19f,angle,ink);
                        Diamond(mesh,c,size*.70f,size*.075f,angle,light);
                    }
                    break;
                case Motif.Pond:
                    if(i%4==0)
                    {
                        float radius=unit*(.028f+.078f*t);
                        Vector2 water=new(c.x,r.yMin+r.height*(.12f+.12f*(i%3)));
                        Ripple(mesh,water,radius,ink);
                        Ripple(mesh,water,radius*.69f,light);
                    }
                    else LitShine(mesh,c,size*(.62f+.56f*wave),ink,light);
                    break;
                default:
                    c.y = r.yMin + r.height*(.12f + .76f*Mathf.Repeat(i*.713f,.99f) + .04f*wave);
                    LitShine(mesh,c,size*(.58f+1.30f*wave)*(marginLights ? .61f : 1),ink,light);
                    break;
            }
        }
    }

    static void LitShine(VertexHelper mesh,Vector2 center,float size,Color ink,Color light)
    {
        Shine(mesh,center,size,ink);
        Shine(mesh,center,size*.53f,light);
    }

    static void Shine(VertexHelper m,Vector2 c,float size,Color tint)
    {
        Diamond(m,c,size*1.32f,size*.22f,90,tint);
        Diamond(m,c,size,size*.17f,0,tint);
    }

    static void Foil(VertexHelper m,Rect r,float phase,Color tint)
    {
        // A translucent diagonal reflection crosses the metalwork, then rests between passes.
        if (phase >= .55f) return;
        float x=Mathf.Lerp(-.35f,1.20f,phase/.55f);
        int n=m.currentVertCount;
        for(int k=-1;k<=1;k++)
        {
            tint.a=k==0 ? .08f : 0;
            float band=x+.08f*k;
            m.AddVert(new Vector2(r.xMin+r.width*Mathf.Clamp01(band),r.yMin),tint,Vector2.zero);
            m.AddVert(new Vector2(r.xMin+r.width*Mathf.Clamp01(band+.20f),r.yMax),tint,Vector2.zero);
        }
        m.AddTriangle(n,n+1,n+2); m.AddTriangle(n+2,n+1,n+3);
        m.AddTriangle(n+2,n+3,n+4); m.AddTriangle(n+4,n+3,n+5);
    }

    static void Diamond(VertexHelper m,Vector2 c,float length,float width,float degrees,Color tint)
    {
        float a = degrees*Mathf.Deg2Rad;
        Vector2 axis = new(Mathf.Cos(a),Mathf.Sin(a));
        Vector2 along=axis*length, side=Vector2.Perpendicular(axis)*width;
        int n=m.currentVertCount;
        m.AddVert(c+along,tint,Vector2.zero); m.AddVert(c+side,tint,Vector2.zero);
        m.AddVert(c-along,tint,Vector2.zero); m.AddVert(c-side,tint,Vector2.zero);
        m.AddTriangle(n,n+1,n+2); m.AddTriangle(n,n+2,n+3);
    }

    static readonly Vector2[] Petal = { new(0,-1),new(-.54f,-.22f),new(-.68f,.47f),
        new(-.27f,.90f),new(0,.66f),new(.31f,.94f),new(.58f,.50f),new(.46f,-.16f) };
    static readonly Vector2[] Maple = { new(0,-1),new(-.16f,-.36f),new(-.73f,-.38f),
        new(-.54f,-.04f),new(-1,.29f),new(-.43f,.22f),new(-.55f,.71f),new(-.2f,.46f),
        new(0,1),new(.20f,.46f),new(.55f,.71f),new(.43f,.22f),new(1,.29f),
        new(.54f,-.04f),new(.73f,-.38f),new(.16f,-.36f) };

    static void Leaf(VertexHelper m,Vector2 center,float size,float angle,Color tint,bool maple)
    {
        var outline=maple ? Maple : Petal;
        float rad=angle*Mathf.Deg2Rad;
        Vector2 up=new(Mathf.Sin(rad),Mathf.Cos(rad)),side=new(up.y,-up.x);
        int n=m.currentVertCount;
        m.AddVert(center,tint,Vector2.zero);
        foreach (Vector2 p in outline) m.AddVert(center+size*(p.x*side+p.y*up),tint,Vector2.zero);
        for(int i=0;i<outline.Length;i++) m.AddTriangle(n,n+i+1,n+1+(i+1)%outline.Length);
    }

    static void Ripple(VertexHelper m,Vector2 c,float radius,Color tint)
    {
        int n=m.currentVertCount;
        for(int i=0;i<=32;i++)
        {
            float a=i*Mathf.PI/16;
            Vector2 axis=new(Mathf.Cos(a),.26f*Mathf.Sin(a));
            m.AddVert(c+radius*axis,tint,Vector2.zero);
            m.AddVert(c+(radius+1.3f)*axis,tint,Vector2.zero);
            if(i==0) continue;
            int j=n+i*2;
            m.AddTriangle(j-2,j-1,j); m.AddTriangle(j,j-1,j+1);
        }
    }
}
