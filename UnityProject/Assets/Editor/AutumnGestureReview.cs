using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AutumnGestureReview
{
    public static void Run()
    {
        var actor = new GameObject("Autumn gesture review");
        var camera = new GameObject("Gesture camera").AddComponent<Camera>();
        var posed = new Mesh();
        var frame = RenderTexture.GetTemporary(540, 620, 24);
        var pixels = new Texture2D(frame.width, frame.height, TextureFormat.RGB24, false);
        var previousFrame = RenderTexture.active;

        try
        {
            actor.transform.position = new Vector3(40, 0, 0);
            var body = actor.AddComponent<FlowerCompanion>();
            body.Initialize("autumn_manwu", .26f);
            var skin = body.GetComponentInChildren<SkinnedMeshRenderer>();
            var head = Array.Find(skin.bones, bone => bone.name == "Head");
            var proxy = new GameObject("Baked companion");
            proxy.transform.SetParent(skin.transform, false);
            proxy.transform.localScale = Vector3.one / skin.transform.lossyScale.x;
            proxy.AddComponent<MeshFilter>().sharedMesh = posed;
            proxy.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
            skin.forceRenderingOff = true;

            skin.BakeMesh(posed);
            var face = new List<int>();
            var vertices = new List<Vector3>();
            posed.GetVertices(vertices);
            for (int i = 0; i < vertices.Count; i++)
            {
                var point = actor.transform.InverseTransformPoint(proxy.transform.TransformPoint(vertices[i]));
                if (point.y >= .161f && point.y <= .209f && Mathf.Abs(point.x) < .037f && point.z >= -.006f)
                    face.Add(i);
            }
            if (face.Count < 700) throw new InvalidOperationException($"Autumn face sample incomplete ({face.Count})");

            var rest = new Vector3[face.Count];
            for (int i = 0; i < face.Count; i++)
                rest[i] = head.InverseTransformPoint(proxy.transform.TransformPoint(vertices[face[i]]));

            camera.targetTexture = frame;
            camera.orthographic = true;
            camera.orthographicSize = .14f;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 2;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.29f,.255f,.28f);
            var focus = actor.transform.position + Vector3.up * .132f;
            camera.transform.position = focus + new Vector3(.035f,.014f,.34f);
            camera.transform.LookAt(focus);
            string dir = Path.GetFullPath("../reports/evidence/autumn-gesture-alpha6");
            Directory.CreateDirectory(dir);

            float peak = 0;
            Vector3 home = body.Palm;
            Vector3 released = Vector3.zero;
            for (int index = 0; index <= 45; index++)
            {
                float seconds = index / 30f;
                body.Sample(2.3f, seconds, 0);
                skin.BakeMesh(posed);
                posed.GetVertices(vertices);
                for (int i = 0; i < face.Count; i++)
                {
                    Vector3 point = head.InverseTransformPoint(proxy.transform.TransformPoint(vertices[face[i]]));
                    peak = Mathf.Max(peak, head.TransformVector(point-rest[i]).magnitude);
                }
                if (index == Mathf.CeilToInt(30 * body.Profile.Release))
                    released = actor.transform.InverseTransformPoint(body.Palm);

                camera.Render();
                RenderTexture.active = frame;
                pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(dir,$"gesture-{index:D3}.jpg"),pixels.EncodeToJPG(93));
            }

            float reach = Vector3.Distance(actor.transform.TransformPoint(released),home);
            Debug.Log($"Autumn gesture: facial samples={face.Count}, max face motion relative to head={peak*1000:F4} mm, palm travel={reach*1000:F2} mm, palm release height={released.y*1000:F2} mm");
            if (peak > .0004f || reach < .008f || released.y > .145f || Mathf.Abs(released.x) < .029f)
                throw new InvalidOperationException("Autumn gesture moved the face or placed the palm too close to it");
        }
        finally
        {
            RenderTexture.active = previousFrame;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(frame);
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(posed);
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}
