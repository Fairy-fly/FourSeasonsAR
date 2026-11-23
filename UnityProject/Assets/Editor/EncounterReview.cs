using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EncounterReview
{
    public static void CheckSpring()
    {
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
