using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EncounterReview
{
    public static void CheckSpring()
    {
        using(var polygon=new Unity.Collections.NativeArray<Vector2>(new[]{new Vector2(0,0),new Vector2(2,0),new Vector2(2,2),new Vector2(1.4f,2),new Vector2(1.4f,.7f),new Vector2(.6f,.7f),new Vector2(.6f,2),new Vector2(0,2)},Unity.Collections.Allocator.Temp))
        {
            var from=new Vector2(.3f,1.5f);
            var end=FloorBoundary.ReachableEnd(polygon,from,new Vector2(1.7f,1.5f),.035f);
            if(end.x>.58f || end.x<.5f || FloorBoundary.Contains(polygon,new Vector2(1,1),.035f))
                throw new InvalidOperationException("Walk crossed a concave table boundary");
            var atEdge=new Vector2(.58f,1.5f);
            if(!FloorBoundary.TryRecover(polygon,atEdge,.058f,out var recovered) ||
                (recovered-atEdge).magnitude>.08f || !FloorBoundary.Contains(polygon,recovered,.058f) ||
                FloorBoundary.TryRecover(polygon,new Vector2(1,1.5f),.058f,out _))
                throw new InvalidOperationException("Placement recovery moved to the wrong part of the surface");
        }
        using(var polygon=new Unity.Collections.NativeArray<Vector2>(new[]{
            new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(.5007f,1),
            new Vector2(.5007f,.4f),new Vector2(.5003f,.4f),new Vector2(.5003f,1),new Vector2(0,1)
        },Unity.Collections.Allocator.Temp))
        {
            var start=new Vector2(.1f,.65f);
            var otherSide=new Vector2(.91f,.65f);
            var end=FloorBoundary.ReachableEnd(polygon,start,otherSide,0);
            if(FloorBoundary.CanTravel(polygon,start,otherSide,0) || end.x<.499f || end.x>=.5003f ||
                !FloorBoundary.CanTravel(polygon,new Vector2(.1f,.2f),new Vector2(.91f,.2f),.058f))
                throw new InvalidOperationException("Narrow table gap was skipped");
        }
        Debug.Log("Real-world boundary tests passed: concave route, narrow gap, local re-placement");
        var album=new SeasonAlbum("test.field."+Guid.NewGuid()+".");
        try
        {
            album.Collect("a");album.Collect("b");album.Reset(new[]{"a"});
            if(album.Contains("a") || !album.Contains("b") || album.Collect("b"))throw new InvalidOperationException("Selective reset failed");
        }
        finally {album.Reset(new[]{"a","b"});}
        var sample=AssetDatabase.LoadAssetAtPath<SpringSpirit>("Assets/Prefabs/SpringSpirit.prefab");
        var spirit=UnityEngine.Object.Instantiate(sample);
        var camera=new GameObject("Encounter review camera",typeof(Camera)).GetComponent<Camera>();
        var frame=RenderTexture.GetTemporary(512,640,24);
        var previous=RenderTexture.active;
        try
        {
            spirit.transform.position=new Vector3(48,0,0);
            camera.transform.position=spirit.transform.position+new Vector3(0,.057f,-.22f);
            camera.transform.rotation=Quaternion.identity;
            camera.nearClipPlane=.01f;
            camera.farClipPlane=1;
            camera.fieldOfView=34;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.23f,.19f,.22f);
            camera.targetTexture=frame;
            spirit.ShowCompanion(camera);
            var skin=spirit.GetComponentInChildren<SkinnedMeshRenderer>();
            if(!skin || skin.bones.Length < 18 || spirit.portrait.enabled || spirit.portrait.GetComponent<BoxCollider>().enabled)
                throw new InvalidOperationException("Spring encounter skin or collision mismatch");
            var hit=spirit.GetComponentInChildren<CapsuleCollider>();
            if(!hit || Mathf.Abs(hit.height-.105f)>.001f)throw new InvalidOperationException("Missing interaction volume");
            var folder=Path.GetFullPath("../reports/evidence/encounter-alpha1");
            Directory.CreateDirectory(folder);
            var companion=spirit.GetComponentInChildren<FlowerCompanion>();
            companion.Sample(3,4,0);
            Vector3 neutral=companion.Palm;
            companion.Sample(3,.60f,0);
            if(Vector3.Distance(neutral,companion.Palm)<.0015f)throw new InvalidOperationException("Palm gesture too small or rig bone names changed");
            companion.Sample(3,4,0);
            byte[] first=null;
            foreach(int angle in new[]{0,70,180})
            {
                spirit.transform.rotation=Quaternion.Euler(0,angle,0);
                camera.Render();RenderTexture.active=frame;
                var pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);pixels.Apply();
                var encoded=pixels.EncodeToJPG(92);
                File.WriteAllBytes(Path.Combine(folder,"spring-"+angle+".jpg"),encoded);
                var colors=pixels.GetPixels32();
                var bg=colors[0];
                int visible=Array.FindAll(colors,c=>Math.Abs(c.r-bg.r)+Math.Abs(c.g-bg.g)+Math.Abs(c.b-bg.b)>35).Length;
                if(visible < colors.Length*.035f || visible>colors.Length*.88f || (first!=null && System.Linq.Enumerable.SequenceEqual(first,encoded)))
                    throw new InvalidOperationException("Encounter preview silhouette failed "+angle+" pixels="+visible+" bounds="+skin.bounds.ToString("F4"));
                if(first==null)first=encoded;
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            Debug.Log("Spring encounter skin, collider and three-view review passed");
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(spirit.gameObject);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            RenderTexture.ReleaseTemporary(frame);
        }
    }
}
