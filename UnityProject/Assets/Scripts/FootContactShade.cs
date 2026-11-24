using UnityEngine;
using UnityEngine.Rendering;

// Both soft shoe shadows share one small mesh and one draw call.
[DefaultExecutionOrder(100)]
public sealed class FootContactShade : MonoBehaviour
{
    const int Sectors=24, Rings=4, PerFoot=1+Sectors*Rings;
    readonly Vector3[] points=new Vector3[2*PerFoot];
    readonly Color32[] colors=new Color32[2*PerFoot];
    readonly Vector2[] circle=new Vector2[Sectors];
    static readonly float[] Opacity={.85f,.44f,.12f,0};
    FlowerCompanion owner;
    public Transform Floor { get; set; }
    Mesh mesh;
    Material ink;

    public void Initialize(FlowerCompanion body,Transform floor)
    {
        owner=body;Floor=floor;
        mesh=new Mesh {name="鞋底接触影"};
        mesh.MarkDynamic();
        ink=new Material(Shader.Find("Sprites/Default"));
        var node=new GameObject("接触影");
        node.transform.SetParent(transform,false);
        node.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=ink;
        renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        var triangles=new int[2*Sectors*(1+2*(Rings-1))*3];
        int n=0;
        for(int i=0;i<Sectors;i++)
        {
            float angle=i*2*Mathf.PI/Sectors;
            circle[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            for(int foot=0;foot<2;foot++)
            {
                int c=foot*PerFoot;
                int a=c+1+i,b=c+1+(i+1)%Sectors;
                triangles[n++]=c;triangles[n++]=b;triangles[n++]=a;
                for(int r=1;r<Rings;r++,a+=Sectors,b+=Sectors)
                {
                    triangles[n++]=a;triangles[n++]=b;triangles[n++]=b+Sectors;
                    triangles[n++]=a;triangles[n++]=b+Sectors;triangles[n++]=a+Sectors;
                }
            }
        }
        mesh.vertices=points;mesh.triangles=triangles;
        Draw();
    }

    void LateUpdate()=>Draw();
    public void Draw()
    {
        if(!owner || !Floor)return;
        Vector3 up=Floor.up;
        var forward=Vector3.ProjectOnPlane(transform.forward,up).normalized;
        var right=Vector3.Cross(up,forward);
        float h=owner.Height*transform.lossyScale.y;
        for(int foot=0;foot<2;foot++)
        {
            Vector3 ankle=owner.Gait.Ankle(foot);
            float above=Vector3.Dot(ankle-Floor.position,up);
            float lift=Mathf.Clamp01((above-owner.Gait.AnkleRestHeight(foot)*transform.lossyScale.y)/(h*.22f));
            Vector3 center=ankle-up*(above-.001f)+forward*h*.018f;
            float width=h*Mathf.Lerp(.075f,.11f,lift), length=h*Mathf.Lerp(.11f,.15f,lift);
            var color=new Color32(57,35,48,(byte)Mathf.Lerp(84,12,lift));
            int c=foot*PerFoot;
            points[c]=transform.InverseTransformPoint(center);
            colors[c]=color;
            for(int r=0;r<Rings;r++)
                for(int i=0;i<Sectors;i++)
                {
                    int k=c+1+r*Sectors+i;
                    var offset=right*(circle[i].x*width)+forward*(circle[i].y*length);
                    points[k]=transform.InverseTransformPoint(center+offset*((r+1f)/Rings));
                    colors[k]=new Color32(color.r,color.g,color.b,(byte)(color.a*Opacity[r]));
                }
        }
        mesh.vertices=points;mesh.colors32=colors;mesh.RecalculateBounds();
    }
    void OnDestroy()
    {
        if(Application.isPlaying){Destroy(mesh);Destroy(ink);}
        else{DestroyImmediate(mesh);DestroyImmediate(ink);}
    }
}
