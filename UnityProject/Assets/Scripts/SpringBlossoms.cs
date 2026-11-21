using UnityEngine;
using UnityEngine.UI;

// The branch, petals, stamens and buds share one resolution-independent UI mesh.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class SpringBlossoms : MaskableGraphic
{
    public bool sprig;
    static readonly Color32 Twig = new(130,78,75,255);
    static readonly Color32 Rose = new(196,104,124,255);
    static readonly Color32 Blush = new(255,215,216,255);
    static readonly Color32 Pollen = new(198,145,75,255);
    protected SpringBlossoms() { useLegacyMeshGeneration = false; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        float unit = Mathf.Min(r.width / 100, r.height / 52);
        Vector2 Point(float x,float y) => r.center + new Vector2(x-50,y-26)*unit;
        if (!sprig)
        {
            Flower(mesh,r.center,Mathf.Min(r.width,r.height)*.47f,10);
            return;
        }
        Stem(mesh,Point(98,42),Point(66,26),Point(18,14),2.2f*unit);
        Stem(mesh,Point(73,31),Point(70,36),Point(61,46),1.3f*unit);
        Stem(mesh,Point(55,24),Point(47,22),Point(39,32),1.1f*unit);
        Stem(mesh,Point(38,19),Point(26,18),Point(12,8),unit);
        Flower(mesh,Point(74,33),12.5f*unit,-8);
        Flower(mesh,Point(46,22),9.3f*unit,18);
        Flower(mesh,Point(60,44),5.3f*unit,24);
        Disc(mesh,Point(38,32),2.8f*unit,Rose);
        Disc(mesh,Point(12,8),2.9f*unit,Rose);
    }

    static void Vertex(VertexHelper mesh,Vector2 p,Color32 tint) =>
        mesh.AddVert(p,tint,Vector2.zero);

    static void Stem(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,float width)
    {
        int start = mesh.currentVertCount;
        for (int i=0;i<=12;i++)
        {
            float t=i/12f, s=1-t;
            Vector2 p=s*s*a + 2*s*t*b + t*t*c;
            Vector2 normal=Vector2.Perpendicular((s*(b-a)+t*(c-b)).normalized);
            Vector2 offset=normal*width*Mathf.Lerp(.5f,.12f,t);
            Vertex(mesh,p+offset,Twig);
            Vertex(mesh,p-offset,Twig);
            if (i==0) continue;
            int j=start+2*i;
            mesh.AddTriangle(j-2,j-1,j); mesh.AddTriangle(j,j-1,j+1);
        }
    }

    static void Flower(VertexHelper mesh,Vector2 center,float radius,float angle)
    {
        for (int k=0;k<5;k++)
        {
            float a=(angle+k*72)*Mathf.Deg2Rad;
            Vector2 up=new(Mathf.Cos(a),Mathf.Sin(a)), side=Vector2.Perpendicular(up);
            int first=mesh.currentVertCount;
            Vertex(mesh,center+up*radius*.42f,Rose);
            const int steps=36;
            for (int i=0;i<steps;i++)
            {
                float t=i*2*Mathf.PI/steps, v=(1-Mathf.Cos(t))*.5f;
                float notch=.13f*Mathf.Pow(Mathf.Max(0,1-Mathf.Abs(t-Mathf.PI)/.45f),2);
                float x=Mathf.Sin(t)*(.22f+.20f*v);
                Vertex(mesh,center+radius*(side*x+up*(v-notch)),Color.Lerp(Rose,Blush,.23f+.77f*v));
            }
            for (int i=0;i<steps;i++) mesh.AddTriangle(first,first+1+i,first+1+(i+1)%steps);
            Disc(mesh,center+up*radius*.18f,radius*.052f,Pollen);
        }
        Disc(mesh,center,radius*.108f,Pollen);
    }

    static void Disc(VertexHelper mesh,Vector2 center,float radius,Color32 tint)
    {
        int first=mesh.currentVertCount;
        Vertex(mesh,center,tint);
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI/8;
            Vertex(mesh,center+radius*new Vector2(Mathf.Cos(a),Mathf.Sin(a)),tint);
        }
        for(int i=0;i<16;i++) mesh.AddTriangle(first,first+1+i,first+1+(i+1)%16);
    }
}
