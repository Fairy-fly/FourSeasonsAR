using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CompanionActionReview
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

    static void Review(CompanionProfile profile,Sprite motif)
    {
        var actor=new GameObject(profile.Name+" action review");
        var camera=new GameObject("Action camera").AddComponent<Camera>();
        var target=RenderTexture.GetTemporary(528,624,24);
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        var pose=new Mesh();
        try
        {
            actor.transform.position=new Vector3(65,0,0);
            var companion=actor.AddComponent<FlowerCompanion>();
            companion.Initialize(profile.Id,FlowerCompanion.WorldHeight);
            var skin=companion.GetComponentInChildren<SkinnedMeshRenderer>();
            var left=Array.Find(skin.bones,bone=>bone.name=="PalmL");
            var right=Array.Find(skin.bones,bone=>bone.name=="PalmR");
            var footL=Array.Find(skin.bones,bone=>bone.name=="FootL");
            var footR=Array.Find(skin.bones,bone=>bone.name=="FootR");

            var effects=new GameObject("Action particles").AddComponent<CompanionPetals>();
            effects.transform.SetParent(actor.transform,false);
            effects.Initialize(companion,camera,motif);
            var particles=Array.FindAll(effects.GetComponentsInChildren<SpriteRenderer>(true),r=>r.name=="季节印记");
            if(!companion.TryRespond(0) || companion.AlternateReaction || companion.TryRespond(.4f) ||
                !companion.TryRespond(1.6f) || !companion.AlternateReaction ||
                companion.TryRespond(2.5f) || companion.ReactionCount!=2)
                throw new InvalidOperationException("Repeated taps disrupted the gesture order for "+profile.Id);

            const float idlePhase=2.3f;
            companion.Sample(idlePhase,.83f,0);
            Vector3 originalPalm=left.position;
            Vector3 originalRight=right.position;
            companion.Sample(idlePhase,3,0);
            Vector3 quietL=left.position, quietR=right.position;
            Vector3 plantedL=footL.position, plantedR=footR.position;
            Quaternion restWrist=left.localRotation;

            var baked=new GameObject("Baked pose");
            baked.transform.SetParent(skin.transform,false);
            baked.transform.localScale=Vector3.one/skin.transform.lossyScale.x;
            baked.AddComponent<MeshFilter>().sharedMesh=pose;
            baked.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;
            skin.forceRenderingOff=true;
            camera.orthographic=true;
            camera.orthographicSize=.161f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.221f,.193f,.236f);
            camera.targetTexture=target;
            var focus=actor.transform.position+Vector3.up*.148f;
            camera.transform.position=focus+new Vector3(.039f,.045f,.48f);
            camera.transform.LookAt(focus);
            string folder=Path.GetFullPath("../reports/evidence/actions-rc9-"+profile.Id);
            Directory.CreateDirectory(folder);
            float handTravel=0, distinct=0;
            int peakParticles=0;

            for(int frame=0;frame<=53;frame++)
            {
                float elapsed=frame/24f;
                companion.Sample(idlePhase,elapsed,0,true);
                effects.SampleActionEffect(elapsed,elapsed);
                handTravel=Mathf.Max(handTravel,Mathf.Max(Vector3.Distance(left.position,quietL),Vector3.Distance(right.position,quietR)));
                if(elapsed>.76f && elapsed<1.26f)
                    distinct=Mathf.Max(distinct,Mathf.Max(Vector3.Distance(left.position,originalPalm),Vector3.Distance(right.position,originalRight)));
                int active=Array.FindAll(particles,p=>p.gameObject.activeSelf).Length;
                peakParticles=Mathf.Max(peakParticles,active);
                if(elapsed<companion.ReactionRelease && active!=0)
                    throw new InvalidOperationException("Particles released before the gesture for "+profile.Id);
                if(Vector3.Distance(footL.position,plantedL)>.006f || Vector3.Distance(footR.position,plantedR)>.006f)
                    throw new InvalidOperationException("In-place pose moved the feet for "+profile.Id);

                skin.BakeMesh(pose);
                camera.Render();
                RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder,$"action-{frame:D3}.jpg"),pixels.EncodeToJPG(90));
            }
            companion.Sample(idlePhase,3,0,true);
            effects.SampleActionEffect(3,3);
            if(handTravel<.009f || distinct<.009f || peakParticles!=13 ||
                Array.Exists(particles,p=>p.gameObject.activeSelf) ||
                Vector3.Distance(left.position,quietL)>.0003f || Vector3.Distance(right.position,quietR)>.0003f ||
                Quaternion.Angle(left.localRotation,restWrist)>.2f)
                throw new InvalidOperationException("Alternate action, effect or rest pose check failed for "+profile.Id+
                    $": travel={handTravel:F4}, difference={distinct:F4}, burst={peakParticles}");

            if(!companion.TryRespond(4,false) || companion.AlternateReaction || companion.ReactionCount!=2 ||
                !companion.TryRespond(5.6f) || companion.AlternateReaction ||
                !companion.TryRespond(7.2f) || !companion.AlternateReaction)
                throw new InvalidOperationException("Landing changed the tap alternation for "+profile.Id);
            Debug.Log($"Alternate action {profile.Id}: hands moved {handTravel*1000:F1} mm, difference {distinct*1000:F1} mm, {peakParticles} particles, feet and rest pose passed");
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
