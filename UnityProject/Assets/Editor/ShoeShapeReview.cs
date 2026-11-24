using System.Collections.Generic;
using UnityEngine;

public sealed class ShoeShapeReview
{
    struct Edge
    {
        public int A, B;
        public float Length;
    }

    readonly CompanionProfile profile;
    readonly Transform proxy;
    readonly List<Edge> edges=new();
    readonly List<Vector3> points=new();
    float extraLength, longestRatio=1;

    public ShoeShapeReview(CompanionProfile profile, SkinnedMeshRenderer skin, Mesh mesh,Transform proxy,Transform actor)
    {
        this.profile=profile;
        this.proxy=proxy;
        float cuff=profile.Style switch
        {
            CompanionProfile.Kind.Summer => .0155f,
            CompanionProfile.Kind.Winter => .0295f,
            CompanionProfile.Kind.Time or CompanionProfile.Kind.Star => .011f,
            _ => 0
        };
        if(cuff==0)return;
        skin.BakeMesh(mesh);
        mesh.GetVertices(points);
        var belowCuff=new bool[points.Count];
        for(int i=0;i<points.Count;i++)
        {
            float y=actor.InverseTransformPoint(proxy.TransformPoint(points[i])).y;
            belowCuff[i]=y<=cuff;
        }
        var seen=new HashSet<ulong>();
        var triangles=skin.sharedMesh.GetIndices(0);
        for(int i=0;i<triangles.Length;i+=3)
        {
            Add(triangles[i],triangles[i+1]);
            Add(triangles[i+1],triangles[i+2]);
            Add(triangles[i+2],triangles[i]);
        }

        void Add(int a,int b)
        {
            if(!belowCuff[a] || !belowCuff[b])return;
            ulong key=((ulong)(uint)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
            if(!seen.Add(key))return;
            float length=proxy.TransformVector(points[a]-points[b]).magnitude;
            if(length>.0003f)edges.Add(new Edge { A=a,B=b,Length=length });
        }
    }

    public void Measure(Mesh posed)
    {
        if(edges.Count==0)return;
        posed.GetVertices(points);
        foreach(var edge in edges)
        {
            float length=proxy.TransformVector(points[edge.A]-points[edge.B]).magnitude;
            extraLength=Mathf.Max(extraLength,length-edge.Length);
            if(length-edge.Length>.00020f)longestRatio=Mathf.Max(longestRatio,length/edge.Length);
        }
    }

    public bool Passed()
    {
        if(profile.Style is CompanionProfile.Kind.Spring or CompanionProfile.Kind.Autumn)return true;
        Debug.Log($"Shoe shape {profile.Id}: measured edges={edges.Count}, max added length={extraLength*1000:F3} mm, max ratio={longestRatio:F2}");
        return edges.Count>=30 && extraLength<=.0015f && longestRatio<1.8f;
    }
}
