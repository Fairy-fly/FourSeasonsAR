using UnityEngine;
using UnityEngine.UI;

// The moving flecks sample the actual lettering: blank space between strokes stays empty.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class InkTitle : MaskableGraphic
{
    const int Columns=60, Rows=12;
    Texture2D lettering;
    Color dust;
    float duration, progress=1;
    public InkTitle() { useLegacyMeshGeneration=false; }
    public override Texture mainTexture => lettering ? lettering : base.mainTexture;

    public void Configure(Texture2D texture,Color ink,Color flecks,float seconds)
    {
        lettering=texture;
        color=ink;
        dust=flecks;
        duration=seconds;
        raycastTarget=false;
        SetAllDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetProgress(Application.isPlaying ? 0 : 1);
    }

    void Update()
    {
        if (Application.isPlaying && progress<1 && duration>0)
            SetProgress(progress+Time.unscaledDeltaTime/duration);
    }

    public void SetProgress(float t)
    {
        progress=Mathf.Clamp01(t);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!lettering || progress<=0) return;
        Rect slot=rectTransform.rect;
        float scale=Mathf.Min(slot.width/lettering.width,slot.height/lettering.height);
        Vector2 size=new Vector2(lettering.width,lettering.height)*scale;
        if(size.x<=0 || size.y<=0) return;
        Rect area=new(slot.center-size*.5f,size);
        if(progress>=1)
        {
            Strip(mesh,area,0,1,1,1);
            return;
        }
        for(int col=0;col<Columns;col++)
        {
            float x0=(float)col/Columns, x1=(col+1f)/Columns;
            float left=InkOpacity(x0),right=InkOpacity(x1);
            if(left+right>0) Strip(mesh,area,x0,x1,left,right);
            for(int row=0;row<Rows;row++)
                Fleck(mesh,area,col,row);
        }
    }

    float InkOpacity(float x) =>
        Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f+.51f*x,.44f+.51f*x,progress));

    void Strip(VertexHelper m,Rect area,float x0,float x1,float left,float right)
    {
        Color a=color,b=color;
        a.a*=left; b.a*=right;
        int n=m.currentVertCount;
        float l=area.xMin+area.width*x0, r=area.xMin+area.width*x1;
        m.AddVert(new Vector2(l,area.yMin),a,new Vector2(x0,0));
        m.AddVert(new Vector2(l,area.yMax),a,new Vector2(x0,1));
        m.AddVert(new Vector2(r,area.yMax),b,new Vector2(x1,1));
        m.AddVert(new Vector2(r,area.yMin),b,new Vector2(x1,0));
        m.AddTriangle(n,n+1,n+2); m.AddTriangle(n,n+2,n+3);
    }

    void Fleck(VertexHelper m,Rect area,int col,int row)
    {
        int seed=(col*43+row*71+col*row*11)%103;
        float noise=seed/102f, x=(col+.5f)/Columns, y=(row+.5f)/Rows;
        float t=(progress-.50f*x-.045f*noise)/.40f;
        if(t<=0 || t>=1) return;
        float remaining=1-t;
        Vector2 center=area.min+new Vector2(x*area.width,y*area.height);
        center.x-=area.width*(.05f+.10f*noise)*remaining*remaining;
        center.y+=area.height*(noise-.5f)*remaining*(1.2f+.5f*Mathf.Sin(t*Mathf.PI));
        float angle=remaining*(noise-.5f)*2.7f;
        float k=.24f+.76f*t;
        Vector2 along=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(area.width/Columns*.5f*k);
        Vector2 up=new Vector2(-Mathf.Sin(angle),Mathf.Cos(angle))*(area.height/Rows*.5f*k);
        Color tint=Color.Lerp(dust,color,Mathf.Clamp01(t*.72f+noise*.23f));
        tint.a*=Mathf.SmoothStep(0,1,t/.21f)*Mathf.SmoothStep(0,1,remaining/.22f);
        float u=(float)col/Columns, v=(float)row/Rows, du=1f/Columns, dv=1f/Rows;
        int n=m.currentVertCount;
        m.AddVert(center-along-up,tint,new Vector2(u,v));
        m.AddVert(center-along+up,tint,new Vector2(u,v+dv));
        m.AddVert(center+along+up,tint,new Vector2(u+du,v+dv));
        m.AddVert(center+along-up,tint,new Vector2(u+du,v));
        m.AddTriangle(n,n+1,n+2); m.AddTriangle(n,n+2,n+3);
    }
}
