using UnityEngine;
using UnityEngine.UI;
using Motif=AlbumAtmosphere.Motif;

[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class AlbumEngraving : MaskableGraphic
{
    Motif motif;
    static readonly Color32[] Inks={
        new(173,99,123,255),new(73,125,112,255),new(178,110,59,255),
        new(103,137,179,255),new(161,121,62,255),new(125,91,155,255)
    };

    public AlbumEngraving(){useLegacyMeshGeneration=false;}

    public void Select(Motif value)
    {
        motif=value;
        raycastTarget=false;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect=rectTransform.rect;
        float unit=Mathf.Min(rect.width,rect.height);
        if(unit<1)return;
        Vector2 At(float x,float y)=>new(rect.xMin+rect.width*x,rect.yMin+rect.height*y);
        Color ink=Inks[(int)motif];
        bool hidden=motif is Motif.Pearl or Motif.Star;
        ink.a=hidden?.53f:.43f;
        float stroke=Mathf.Max(.65f,unit*.005f);

        // The rails leave the middle and the caption unobstructed.
        for(int i=0;i<4;i++)
        {
            float x=i%2==0?.046f:.954f, y=i<2?.065f:.935f;
            float side=i%2==0?1:-1, rise=i<2?1:-1;
            Line(mesh,At(x,y),At(x+.116f*side,y),stroke,ink);
            Line(mesh,At(x,y),At(x,y+.118f*rise),stroke,ink);
            if(hidden)
                Line(mesh,At(x+.020f*side,y+.030f*rise),At(x+.085f*side,y+.030f*rise),stroke*.65f,ink);
        }
        if(hidden)
        {
            bool ticket=rect.width>rect.height;
            Vector2 center=At(ticket?.74f:.50f,.50f);
            float xRadius=rect.width*(ticket?.176f:.303f), yRadius=rect.height*.393f;
            float alpha=ink.a;
            ink.a=.34f;
            Arc(mesh,center,xRadius,yRadius,0,Mathf.PI*2,stroke,ink);
            Arc(mesh,center,xRadius*.85f,yRadius*.95f,.30f,5.67f,stroke*.63f,ink);
            ink.a=alpha;
            Diamond(mesh,center+Vector2.up*(yRadius*.99f),unit*.045f,unit*.020f,ink);
            Diamond(mesh,center+Vector2.down*(yRadius*.99f),unit*.031f,unit*.016f,ink);
            if(motif==Motif.Star)
            {
                Snowflake(mesh,center+new Vector2(xRadius*.91f,yRadius*.34f),unit*.042f,stroke,ink,2);
                Snowflake(mesh,center+new Vector2(-xRadius*.84f,-yRadius*.29f),unit*.032f,stroke,ink,2);
            }
            else
                for(int i=0;i<5;i++)
                {
                    float theta=(i+1)*1.0472f;
                    Vector2 direction=new(Mathf.Cos(theta),Mathf.Sin(theta));
                    Vector2 at=center+new Vector2(xRadius*direction.x,yRadius*direction.y);
                    Line(mesh,at,at+direction*unit*.023f,stroke,ink);
                }
            return;
        }

        ink.a=.38f;
        Vector2 top=At(.15f,.83f), bottom=At(.85f,.17f);
        float size=unit*.046f;
        switch(motif)
        {
            case Motif.Petal:
                Blossom(mesh,top,size,stroke,ink);
                Blossom(mesh,bottom,size*.78f,stroke,ink);
                break;
            case Motif.Pond:
                Arc(mesh,bottom,size*1.8f,size*.53f,0,Mathf.PI*2,stroke,ink);
                Arc(mesh,bottom,size*1.18f,size*.34f,0,Mathf.PI*2,stroke,ink);
                Arc(mesh,top,size*1.38f,size*.46f,0,Mathf.PI*2,stroke,ink);
                break;
            case Motif.Maple:
                Leaf(mesh,top,size,stroke,ink);
                Leaf(mesh,bottom,size*.76f,stroke,ink);
                break;
            case Motif.Snow:
                Snowflake(mesh,top,size*1.12f,stroke,ink,3);
                Snowflake(mesh,bottom,size*.82f,stroke,ink,3);
                break;
        }
    }

    static void Blossom(VertexHelper mesh,Vector2 center,float size,float stroke,Color ink)
    {
        Vector2 Point(float phase)
        {
            float radius=size*(.79f+.19f*Mathf.Cos(5*phase));
            return center+radius*new Vector2(Mathf.Sin(phase),Mathf.Cos(phase));
        }
        for(int i=0;i<25;i++)
            Line(mesh,Point(i*Mathf.PI*.08f),Point((i+1)*Mathf.PI*.08f),stroke,ink);
        Diamond(mesh,center,size*.20f,size*.16f,ink);
    }

    static void Leaf(VertexHelper mesh,Vector2 center,float size,float stroke,Color ink)
    {
        Vector2 tips=new(size*.72f,size*.83f);
        Vector2 stem=center-tips;
        Line(mesh,stem,center+tips,stroke,ink);
        Line(mesh,center,center+new Vector2(-size*.12f,size*.84f),stroke,ink);
        Line(mesh,center,center+new Vector2(size*.85f,-size*.05f),stroke,ink);
        Line(mesh,center-tips*.48f,center+new Vector2(-size*.66f,size*.29f),stroke,ink);
        Line(mesh,center-tips*.48f,center+new Vector2(size*.29f,-size*.66f),stroke,ink);
    }

    static void Snowflake(VertexHelper mesh,Vector2 center,float size,float stroke,Color ink,int axes)
    {
        for(int i=0;i<axes;i++)
        {
            float angle=i*Mathf.PI/axes;
            Vector2 span=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size;
            Line(mesh,center-span,center+span,stroke,ink);
        }
        Diamond(mesh,center,size*.25f,size*.22f,ink);
    }

    static void Arc(VertexHelper mesh,Vector2 center,float rx,float ry,float start,float end,float stroke,Color ink)
    {
        Vector2 Point(float angle)=>center+new Vector2(rx*Mathf.Cos(angle),ry*Mathf.Sin(angle));
        Vector2 last=Point(start);
        for(int i=1;i<=32;i++)
        {
            Vector2 p=Point(Mathf.Lerp(start,end,i/32f));
            Line(mesh,last,p,stroke,ink);
            last=p;
        }
    }

    static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float stroke,Color ink)
    {
        Vector2 normal=Vector2.Perpendicular((b-a).normalized)*(stroke*.5f);
        Quad(mesh,a+normal,b+normal,b-normal,a-normal,ink);
    }

    static void Diamond(VertexHelper mesh,Vector2 c,float up,float side,Color ink)=>
        Quad(mesh,c+Vector2.up*up,c+Vector2.right*side,c-Vector2.up*up,c-Vector2.right*side,ink);

    static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color ink)
    {
        int index=mesh.currentVertCount;
        mesh.AddVert(a,ink,Vector2.zero);
        mesh.AddVert(b,ink,Vector2.zero);
        mesh.AddVert(c,ink,Vector2.zero);
        mesh.AddVert(d,ink,Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2);
        mesh.AddTriangle(index,index+2,index+3);
    }
}
