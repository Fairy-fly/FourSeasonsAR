using System;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SpringGaitReview
{
    public static void Run()
    {
        var actor=new GameObject("Spring gait review");
        var camera=new GameObject("Side camera").AddComponent<Camera>();
        var frame=RenderTexture.GetTemporary(480,560,24);
        var pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
        var prev=RenderTexture.active;
        var mesh=new Mesh();
        try
        {
            actor.transform.position=new Vector3(60,0,0);
            var body=actor.AddComponent<FlowerCompanion>();
            body.Initialize(SpringVisit.ReferenceName,.26f);
            body.EnableGroundGait();
            var gait=body.Gait;
            int stamps=0;
            gait.Landed+=point=>{if(Mathf.Abs(point.y)>.0002f)throw new Exception("Footprint above floor");stamps++;};
            Vector3[] soles={gait.Ankle(0),gait.Ankle(1)};
            Vector3[] start={soles[0],soles[1]};
            float drift=0, rise=0;
            int held=0, previousSwing=-1;
            string dir=Path.GetFullPath("../reports/evidence/spring-gait-alpha3");
            Directory.CreateDirectory(dir);
            camera.targetTexture=frame;camera.orthographic=true;camera.orthographicSize=.153f;
            camera.nearClipPlane=.01f;camera.farClipPlane=2;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.29f,.255f,.28f);
            // Batch-mode skinned renders can reuse the previous GPU skin pose.
            // Bake the actual weighted mesh for each frame of the visual review.
            var skin=body.GetComponentInChildren<SkinnedMeshRenderer>();
            var proxy=new GameObject("Posed mesh");
            proxy.transform.SetParent(skin.transform,false);
            proxy.transform.localScale=Vector3.one/skin.transform.lossyScale.x;
            proxy.AddComponent<MeshFilter>().sharedMesh=mesh;
            proxy.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
            skin.forceRenderingOff=true;
            int count=0;
            for(int i=0;i<165;i++)
            {
                const float dt=1f/30;
                body.Walking=i<120?1:0;
                actor.transform.position+=actor.transform.forward*(.10f*dt*body.Walking);
                body.Sample(i*dt,5,dt);
                int swing=gait.SwingFoot;
                for(int side=0;side<2;side++)
                {
                    var foot=gait.Ankle(side);
                    if(swing>=0 && previousSwing==swing && side!=swing)
                    {
                        if(held==0)start[side]=soles[side];
                        float error=Vector3.Distance(start[side],foot);
                        if(error>drift && error>.002f)Debug.Log($"Gait reach frame={i} side={side} error={error:F5} start={start[side]:F4} current={foot:F4} root={actor.transform.position:F4}");
                        drift=Mathf.Max(drift,error);
                        held++;
                    }
                    if(side==swing)rise=Mathf.Max(rise,foot.y-soles[side].y);
                    soles[side]=foot;
                }
                if(previousSwing!=swing)held=0;
                previousSwing=swing;
                if(i%2!=0)continue;
                var focus=actor.transform.position+Vector3.up*.129f;
                camera.transform.position=focus+new Vector3(.31f,.016f,.12f);
                camera.transform.LookAt(focus);
                skin.BakeMesh(mesh);
                if(i==0)Debug.Log($"Baked mesh bounds={mesh.bounds} skin scale={skin.transform.lossyScale:F4}");
                camera.Render();
                RenderTexture.active=frame;
                pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(dir,$"walk-{count++:D3}.jpg"),pixels.EncodeToJPG(91));
            }
            Debug.Log($"Spring gait: stance drift={drift:F5} m, peak frame rise={rise:F5} m, contacts={stamps}, frames={count}");
            if(stamps<5 || stamps>32 || drift>.003f || rise<.00035f || gait.SwingFoot!=-1)
                throw new InvalidOperationException("Spring gait failed contact, clearance or stopping checks");
        }
        finally
        {
            RenderTexture.active=prev;camera.targetTexture=null;
            RenderTexture.ReleaseTemporary(frame);
            Object.DestroyImmediate(mesh);Object.DestroyImmediate(pixels);Object.DestroyImmediate(actor);Object.DestroyImmediate(camera.gameObject);
        }
    }
}
