using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class HoverReview
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SpringVisit.unity");
        var visit=Object.FindAnyObjectByType<SpringVisit>();
        foreach(var profile in CompanionProfile.All)
        {
            var card=Array.Find(visit.characters,entry=>entry.referenceName==profile.Id);
            Review(profile,card ? card.petal : null);
        }
    }

    static void Review(CompanionProfile profile,Sprite seasonSprite)
    {
        var actor=new GameObject("Hover review actor");
        var marks=new GameObject("Anchored trail review");
        var camera=new GameObject("Hover review camera").AddComponent<Camera>();
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
        var ground=new Material(Shader.Find("Unlit/Color")){color=new Color(.245f,.230f,.256f)};
        floor.GetComponent<MeshRenderer>().sharedMaterial=ground;
        var target=RenderTexture.GetTemporary(480,560,24);
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        var mesh=new Mesh();
        try
        {
            actor.transform.position=new Vector3(80,0,0);
            floor.transform.position=actor.transform.position;
            var body=actor.AddComponent<FlowerCompanion>();
            body.Initialize(profile.Id,FlowerCompanion.WorldHeight);
            body.EnableHover(MovementPreference.Clearance.Max,MovementPreference.Amplitude.Min);
            body.Sample(2,4,0);
            if(Mathf.Abs(body.HoverHeight-.070f)>.0003f)
                throw new InvalidOperationException("Zero amplitude did not preserve the chosen hover height");
            body.EnableHover(MovementPreference.Clearance.Min,MovementPreference.Amplitude.Max);
            for(int i=20;i<120;i++)
            {
                body.Sample(i/30f,4,0);
                if(body.HoverHeight<.009f || body.HoverHeight>.041f)
                    throw new InvalidOperationException("Lowest height and highest amplitude exceeded the floor limits");
            }
            body.EnableHover(MovementPreference.Clearance.Default,MovementPreference.Amplitude.Default);
            body.Sample(2,4,1);
            float original=body.HoverHeight;
            body.Held=true;
            body.Sample(2,4,1);
            if(body.HoverHeight-original<.020f)
                throw new InvalidOperationException("Drag did not lift the companion above its current hover pose");
            body.Held=false;
            body.Sample(2,4,1);
            var aura=actor.AddComponent<HoverAura>();
            aura.Initialize(body,floor.transform);
            marks.transform.position=floor.transform.position;
            var effects=marks.AddComponent<CompanionPetals>();
            effects.Initialize(body,camera,seasonSprite);
            var trail=Array.FindAll(marks.GetComponentsInChildren<SpriteRenderer>(true),
                particle=>particle.name=="同行拖尾粒子");
            if(body.Gait!=null || !body.PoseRoot.GetComponent<CapsuleCollider>() || !aura.Mark.sprite)
                throw new InvalidOperationException("Hover must move the touch collider and use its own ground shadow");

            camera.targetTexture=target;
            camera.orthographic=true;
            camera.orthographicSize=.174f;
            camera.nearClipPlane=.01f;
            camera.farClipPlane=4f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.245f,.230f,.256f);

            var skin=body.GetComponentInChildren<SkinnedMeshRenderer>();
            var proxy=new GameObject("Posed mesh");
            proxy.transform.SetParent(skin.transform,false);
            proxy.transform.localScale=Vector3.one/skin.transform.lossyScale.x;
            proxy.AddComponent<MeshFilter>().sharedMesh=mesh;
            proxy.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
            skin.forceRenderingOff=true;

            string folder=Path.GetFullPath("../reports/evidence/hover-rc6-"+profile.Id);
            Directory.CreateDirectory(folder);
            float min=float.MaxValue, max=0, peakLean=0, maxHeld=0;
            int frame=0;
            for(int i=0;i<176;i++)
            {
                const float dt=1f/30;
                float seconds=i*dt;
                body.Glide=i>=70 && i<116 ? Mathf.Sin((i-70)/46f*Mathf.PI) : 0;
                body.Held=i>=132 && i<155;
                actor.transform.position+=body.Glide*dt*body.Height*profile.Pace*
                    MovementPreference.DefaultPaceFactor*actor.transform.forward;
                actor.transform.rotation=Quaternion.Euler(0,Mathf.SmoothStep(0,-65,Mathf.InverseLerp(73,110,i)),0);
                body.Sample(seconds,i>=116 && i<132?(i-116)*dt:4,dt);
                aura.Sample(seconds);

                float floorError=Vector3.Dot(aura.Mark.transform.position-floor.transform.position,floor.transform.up);
                if(Mathf.Abs(floorError-.0016f)>.0001f || Mathf.Abs(Vector3.Dot(aura.Mark.transform.forward,floor.transform.up))<.98f)
                    throw new InvalidOperationException("Hover light departed from the plane");
                if(i>=20 && i<68)
                {
                    min=Mathf.Min(min,body.HoverHeight);
                    max=Mathf.Max(max,body.HoverHeight);
                }
                if(i>75 && i<110)peakLean=Mathf.Max(peakLean,Vector3.Angle(body.PoseRoot.up,actor.transform.up));
                if(body.Held)maxHeld=Mathf.Max(maxHeld,body.HoverHeight);

                if(i%2!=0)continue;
                Vector3 focus=actor.transform.position+Vector3.up*.153f;
                camera.transform.position=focus+new Vector3(.20f,.132f,.40f);
                camera.transform.LookAt(focus);
                effects.SampleTrail(seconds);
                if(i==50 || i==146)
                {
                    if(Array.Exists(trail,p=>p.gameObject.activeSelf))
                        throw new InvalidOperationException("Stationary trail emitted or failed to expire "+profile.Id);
                }
                if(i==110 && Array.FindAll(trail,p=>p.gameObject.activeSelf &&
                    p.transform.position.y>.035f &&
                    Vector3.ProjectOnPlane(p.transform.position-actor.transform.position,Vector3.up).magnitude>.024f).Length<2)
                    throw new InvalidOperationException("Movement did not leave a detached trail "+profile.Id);
                skin.BakeMesh(mesh);
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,$"float-{frame++:D3}.jpg"),pixels.EncodeToJPG(90));
            }

            float settled=Vector3.Angle(body.PoseRoot.up,actor.transform.up);
            Debug.Log($"Hover {profile.Id}: idle {min*100:F1}–{max*100:F1} cm, peak lean {peakLean:F1}°, drag {maxHeld*100:F1} cm, settled {settled:F1}°");
            if(min<.025f || max>.045f || max-min<.015f || peakLean<3.5f || settled>2.8f ||
                maxHeld>.077f)
                throw new InvalidOperationException("Hover motion limit failed for "+profile.Id);
        }
        finally
        {
            RenderTexture.active=previous;
            camera.targetTexture=null;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(marks);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(floor);
            Object.DestroyImmediate(ground);
        }
    }
}
