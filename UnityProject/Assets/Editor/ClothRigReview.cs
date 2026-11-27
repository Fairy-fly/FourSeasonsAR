using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ClothRigReview
{
    public static void Run()
    {
        bool baseline=Environment.GetEnvironmentVariable("FOUR_SEASONS_CLOTH_BASELINE")=="1";
        Check("summer_huajian","PalmR",.079f,.146f,baseline);
        Check("autumn_manwu","PalmL",.102f,.171f,baseline);
    }

    static void Check(string role,string wristName,float lower,float upper,bool baseline)
    {
        var actor=new GameObject("Cloth review actor");
        var camera=new GameObject("Cloth review camera").AddComponent<Camera>();
        var target=RenderTexture.GetTemporary(432,550,24);
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        var pose=new Mesh();
        var previous=RenderTexture.active;
        try
        {
            actor.transform.position=new Vector3(60,0,0);
            var body=actor.AddComponent<FlowerCompanion>();
            body.Initialize(role,FlowerCompanion.WorldHeight);
            var skin=body.GetComponentInChildren<SkinnedMeshRenderer>();
            var wrist=Array.Find(skin.bones,bone=>bone.name==wristName);
            var proxy=new GameObject("Baked cloth");
            proxy.transform.SetParent(skin.transform,false);
            proxy.transform.localScale=Vector3.one/skin.transform.lossyScale.x;
            proxy.AddComponent<MeshFilter>().sharedMesh=pose;
            proxy.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
            skin.forceRenderingOff=true;
            body.Sample(2.3f,4,0);
            skin.BakeMesh(pose);
            Vector3 Local(Vector3 point)=>actor.transform.InverseTransformPoint(proxy.transform.TransformPoint(point));
            var bind=Array.ConvertAll(pose.vertices,Local);
            float side=Mathf.Sign(actor.transform.InverseTransformPoint(wrist.position).x);

            var edges=new List<(int a,int b,float length)>();
            var visited=new HashSet<(int,int)>();
            var triangles=pose.triangles;
            for(int i=0;i<triangles.Length;i+=3)
                for(int j=0;j<3;j++)
                {
                    int a=triangles[i+j], b=triangles[i+(j+1)%3];
                    if(!visited.Add((Math.Min(a,b),Math.Max(a,b))))continue;
                    Vector3 center=(bind[a]+bind[b])*.5f;
                    if(center.y<lower || center.y>upper || side*center.x<.021f)continue;
                    float length=Vector3.Distance(bind[a],bind[b]);
                    if(length>=.00145f)edges.Add((a,b,length));
                }
            if(edges.Count<300)throw new InvalidOperationException("Cloth review region is too small for "+role);

            camera.orthographic=true;
            camera.orthographicSize=.155f;
            camera.targetTexture=target;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.247f,.223f,.253f);
            camera.nearClipPlane=.01f;
            var focus=actor.transform.position+Vector3.up*.137f;
            string label=baseline?"baseline":"repaired";
            string folder=Path.GetFullPath($"../reports/evidence/cloth-rc11-{label}-{role}");
            Directory.CreateDirectory(folder);
            var vertices=new List<Vector3>();
            for(int gesture=0;gesture<2;gesture++)
            {
                var expansion=new float[edges.Count];
                int frames=gesture==0?35:50;
                for(int i=0;i<=frames;i++)
                {
                    body.Sample(2.3f,i/24f,0,gesture==1);
                    skin.BakeMesh(pose);
                    pose.GetVertices(vertices);
                    float bakedScale=proxy.transform.lossyScale.x;
                    for(int k=0;k<edges.Count;k++)
                    {
                        var e=edges[k];
                        float strain=Vector3.Distance(vertices[e.a],vertices[e.b])*bakedScale/e.length;
                        expansion[k]=Mathf.Max(expansion[k],strain);
                    }
                    if(i%2!=0)continue;
                    camera.transform.position=focus+new Vector3(gesture==0?.020f:.16f,.035f,.48f);
                    camera.transform.LookAt(focus);
                    camera.Render();
                    RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,$"gesture-{gesture}-{i/2:D3}.jpg"),pixels.EncodeToJPG(88));
                }
                Array.Sort(expansion);
                int longEdges=Array.FindAll(expansion,x=>x>1.9f).Length;
                float percentile=expansion[(int)(.98f*expansion.Length)];
                Debug.Log($"Cloth {label} {role} gesture {gesture}: edges={edges.Count}, p98={percentile:F2}, above 1.9x={longEdges}, peak={expansion[^1]:F2}");
                bool lantern=role=="summer_huajian";
                if(!baseline && (percentile>(lantern?1.6f:4.95f) || longEdges>(lantern?150:185)))
                    throw new InvalidOperationException("Excessive stretching in "+role+" during gesture "+gesture);
            }
        }
        finally
        {
            RenderTexture.active=previous;
            camera.targetTexture=null;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(pose);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
