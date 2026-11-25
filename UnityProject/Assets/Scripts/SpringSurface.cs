using UnityEngine;
using UnityEngine.UI;

public enum FrameStyle { Original, Rounded, Beveled }

// Shapes are tessellated at their current screen size. Ticket corners use a fixed design-unit radius.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class SpringSurface : MaskableGraphic
{
    public bool paper;
    FrameStyle frameStyle;
    bool useGradient;
    Color gradientEnd;

    public SpringSurface SetGradient(Color start, Color end)
    {
        useGradient = true;
        gradientEnd = end;
        color = start;
        SetVerticesDirty();
        return this;
    }

    public void SetFrameStyle(FrameStyle style)
    {
        if(frameStyle==style)return;
        frameStyle=style;
        SetVerticesDirty();
    }
    protected SpringSurface() { useLegacyMeshGeneration = false; }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        mesh.AddVert(rect.center,color,Vector2.zero);
        float radius = Mathf.Min(rect.width,rect.height)*.5f;
        bool angled=frameStyle==FrameStyle.Beveled;
        bool ticket=frameStyle==FrameStyle.Original && paper;
        for (int corner=0;corner<4;corner++)
        {
            float x = corner==0 || corner==3 ? rect.xMax : rect.xMin;
            float y = corner<2 ? rect.yMax : rect.yMin;
            Vector2 edge = new(x,y);
            Vector2 inward = new(x==rect.xMax ? -1:1,y==rect.yMax ? -1:1);
            if(ticket || angled)
            {
                float cut=angled ? Mathf.Min(16,radius*.61f) : Mathf.Min(11,radius*.25f);
                int divisions=angled?1:12;
                for(int i=0;i<=divisions;i++)
                {
                    float angle=(corner*90+90-i*90f/divisions)*Mathf.Deg2Rad;
                    Vector2 p=edge-cut*new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    mesh.AddVert(p,color,Vector2.zero);
                }
            }
            else
            {
                float cornerRadius=frameStyle==FrameStyle.Rounded ? Mathf.Min(23,radius*.78f) : radius;
                Vector2 center=edge+cornerRadius*inward;
                for(int i=0;i<=24;i++)
                {
                    float angle=(corner*90+i*3.75f)*Mathf.Deg2Rad;
                    mesh.AddVert(center+cornerRadius*new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),color,Vector2.zero);
                }
            }
        }
        if (useGradient)
        {
            var vertex = UIVertex.simpleVert;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex,i);
                float x = (vertex.position.x - rect.xMin) / rect.width;
                float y = (rect.yMax - vertex.position.y) / rect.height;
                vertex.color = Color.Lerp(color,gradientEnd,.76f*x + .24f*y);
                mesh.SetUIVertex(vertex,i);
            }
        }
        int count=mesh.currentVertCount-1;
        for(int i=1;i<=count;i++) mesh.AddTriangle(0,i,i%count+1);
    }
}
