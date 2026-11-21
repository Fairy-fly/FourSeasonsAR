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
        canvas.scaleFactor = 1080f / 480f;
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
        camera.targetTexture = RenderTexture.GetTemporary(1080,2400,24);
        canvas.worldCamera = camera;
        string folder = Path.GetFullPath("../reports/evidence/ui-1.4-editor");
        Directory.CreateDirectory(folder);
        try
        {
            Capture(camera,folder,"cover");
            CheckAlbum(page,safe,camera,folder);
            page.ShowScanner(true);
            page.SetTracking(false,false,true);
            Capture(camera,folder,"scan");
            page.SetTracking(true,true,true);
            var guides = safe.Find("扫描/取景提示").GetComponent<CanvasGroup>();
            // Batch edit mode has no player clock. Capture the completed visual state.
            guides.alpha = 0;
            var story = safe.Find("扫描/花笺位置").GetChild(0).gameObject;
            if (!story.activeInHierarchy) throw new System.InvalidOperationException("点击后花笺未显示");
            foreach (var character in page.visit.characters)
            {
                page.SetStory(character);
                page.AnnounceCollection(character);
                Capture(camera,folder,character.referenceName);
            }
            page.HideAnnouncement();
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

    static void CheckAlbum(SpringPage page,Transform safe,Camera camera,string folder)
    {
        var prefix = "fourSeasons.review." + System.Guid.NewGuid().ToString("N") + ".";
        var visit = page.visit;
        var live = visit.Album;
        var grid = safe.Find("花册/目录");
        var detail = safe.Find("花册/角色详情");
        var count = grid.Find("进度").GetComponent<Text>();
        var open = safe.Find("封面/四季花册").GetComponent<Button>();
        void Check(bool value,string why)
        {
            if (!value) throw new System.InvalidOperationException(why);
        }
        try
        {
            visit.Album = new SeasonAlbum(prefix);
            open.onClick.Invoke();
            Check(count.text.EndsWith("0 / 4") && grid.gameObject.activeInHierarchy,"首次打开花册失败");
            foreach (var character in visit.characters)
            {
                var card = grid.Find(character.referenceName).GetComponent<Button>();
                Check(!card.interactable,"未收集的角色可点选");
                card.onClick.Invoke();
                Check(!detail.gameObject.activeInHierarchy,"未收集的详情被打开");
            }
            Capture(camera,folder,"album-empty");
            int n = 0;
            foreach (var character in visit.characters)
            {
                grid.Find("返回封面").GetComponent<Button>().onClick.Invoke();
                Check(open.gameObject.activeInHierarchy,"花册返回封面失败");
                Check(visit.Album.Collect(character.referenceName),"首次收集失败");
                Check(!visit.Album.Collect(character.referenceName),"同一卡被重复收集");
                visit.Album = new SeasonAlbum(prefix);
                open.onClick.Invoke();
                Check(count.text.EndsWith((++n) + " / 4"),"读取已保存进度失败");
                var card = grid.Find(character.referenceName).GetComponent<Button>();
                Check(card.interactable && card.GetComponentInChildren<Image>().sprite == character.portrait.sprite,"卡位立绘错误");
                card.onClick.Invoke();
                Check(detail.gameObject.activeInHierarchy && !grid.gameObject.activeSelf &&
                    detail.Find("题名").GetComponent<Text>().text == character.characterName &&
                    detail.Find("立绘").GetComponent<Image>().sprite == character.portrait.sprite &&
                    detail.Find("角色花笺/花语").GetComponent<Text>().text == character.excerpt,"详情映射错误");
                if (n == 1) Capture(camera,folder,"album-detail");
                if (character.referenceName == "winter_xueling") Capture(camera,folder,"album-detail-winter");
                detail.Find("返回花册").GetComponent<Button>().onClick.Invoke();
                if (n == 1 || n == visit.characters.Length) Capture(camera,folder,"album-" + n);
            }
            grid.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            Debug.Log("Album persistence, duplicate collection, detail mapping and navigation checks passed");
        }
        finally
        {
            visit.Album = live;
            foreach (var character in visit.characters) PlayerPrefs.DeleteKey(prefix + character.referenceName);
            PlayerPrefs.Save();
        }
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
        foreach (var label in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            foreach (char glyph in label.text)
                if (!char.IsWhiteSpace(glyph) && !label.font.HasCharacter(glyph))
                    throw new System.InvalidOperationException("字库缺字 " + glyph + " / " + label.name);
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
