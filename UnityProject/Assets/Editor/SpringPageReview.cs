using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Render the actual uGUI hierarchy on a neutral field to review it without a camera or card.
public static class SpringPageReview
{
    public static void Render()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SpringVisit.unity");
        var page = Object.FindAnyObjectByType<SpringPage>();
        page.SendMessage("Awake");
        var canvas = page.GetComponentInChildren<Canvas>();
        var frame = (RectTransform)canvas.transform;
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        frame.sizeDelta = new Vector2(480,1067);
        frame.localScale = Vector3.one;
        frame.position = Vector3.zero;
        var safe = (RectTransform)frame.Find("安全区域");
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        var art = frame.Find("花房").GetComponent<RawImage>();
        art.uvRect = SpringPage.CoverUV(480f/1067,(float)page.cover.width/page.cover.height);

        var camera = new GameObject("Review camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 1067 * .5f;
        camera.transform.position = Vector3.back * 10;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.32f,.32f,.33f);
        camera.targetTexture = RenderTexture.GetTemporary(720,1600,24);
        canvas.worldCamera = camera;
        string folder = Path.GetFullPath("../reports/evidence/ui-editor");
        Directory.CreateDirectory(folder);
        try
        {
            Capture(camera,folder,"cover");
            page.ShowScanner(true);
            page.SetTracking(false,false,true);
            Capture(camera,folder,"scan");
            page.SetTracking(true,true,true);
            var guides = safe.Find("扫描/取景提示").GetComponent<CanvasGroup>();
            // Batch edit mode has no player clock. Capture the completed visual state.
            guides.alpha = 0;
            var story = safe.Find("扫描/春知意 · 把第一缕春风捎给赶路的人位置").GetChild(0).gameObject;
            if (!story.activeInHierarchy) throw new System.InvalidOperationException("点击后花笺未显示");
            Capture(camera,folder,"story");
            page.SetTracking(false,false,true);
            if (story.activeSelf) throw new System.InvalidOperationException("丢失目标后花笺仍可见");
            page.ShowScanner(false);
            if (!art.gameObject.activeSelf || safe.Find("扫描").gameObject.activeSelf)
                throw new System.InvalidOperationException("返回封面失败");
        }
        finally
        {
            var target = camera.targetTexture;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
        }
        Debug.Log("Spring UI editor review completed");
    }

    static void Capture(Camera camera,string folder,string name)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var surface in Object.FindObjectsByType<SpringSurface>(FindObjectsSortMode.None))
        {
            var mesh = surface.canvasRenderer.GetMesh();
            if (!mesh || mesh.vertexCount < 3)
                throw new System.InvalidOperationException("界面网格为空 " + surface.name);
        }
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        var target = camera.targetTexture;
        RenderTexture.active = target;
        var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try
        {
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(pixels);
        }
    }
}
