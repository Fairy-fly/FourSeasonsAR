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
        page.BuildInterface();
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
        string folder = Path.GetFullPath("../reports/evidence/ui-1.7-editor");
        Directory.CreateDirectory(folder);
        try
        {
            Capture(camera,folder,"cover");
            CheckTitle(safe.Find("封面/书名").GetComponent<InkTitle>(),camera,"Home");
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
            CheckStoryEntrance(page,story,camera,folder);
            guides.alpha = 0;
            foreach (var character in page.visit.characters)
            {
                page.SetStory(character);
                story.GetComponent<StoryPetalEntrance>().Sample(StoryPetalEntrance.Duration);
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

    static void CheckTitle(InkTitle title,Camera camera,string name)
    {
        if (!title || title.mainTexture.width < 500) throw new System.InvalidOperationException(name+" 题字素材缺失");
        string frames=Path.GetFullPath("Library/TitleFrames/"+name);
        Directory.CreateDirectory(frames);
        var full=camera.targetTexture;
        var preview=RenderTexture.GetTemporary(540,1200,24);
        try
        {
            camera.targetTexture=preview;
            for(int i=0;i<=30;i++)
            {
                title.SetProgress(i/30f);
                Capture(camera,frames,"title-"+i.ToString("D3"));
                int n=title.canvasRenderer.GetMesh()?.vertexCount ?? 0;
                if ((i==0 && n!=0) || (i==15 && n<=4) || (i==30 && n!=4))
                    throw new System.InvalidOperationException(name+" 题字动画阶段错误");
            }
        }
        finally
        {
            title.SetProgress(1);
            camera.targetTexture=full;
            RenderTexture.ReleaseTemporary(preview);
        }
        Debug.Log(name+" title assembly verified");
    }

    static void CheckStoryEntrance(SpringPage page,GameObject story,Camera camera,string folder)
    {
        var entrance = story.GetComponent<StoryPetalEntrance>();
        var paper = story.transform.Find("笺面").GetComponent<CanvasGroup>();
        var words = story.transform.Find("笺文").GetComponent<CanvasGroup>();
        var petals = System.Array.FindAll(story.GetComponentsInChildren<Image>(true),p => p.name == "飞瓣");
        var character = page.visit.characters[0];
        void Check(bool ok,string reason)
        {
            if (!ok) throw new System.InvalidOperationException(reason);
        }
        int VisiblePetals() => System.Array.FindAll(petals,p => p.enabled && p.gameObject.activeInHierarchy).Length;
        page.SetStory(character);
        Check(words.alpha == 0 && paper.alpha == 0 && petals.Length == 10,"花笺初始状态错误");
        entrance.Sample(.07f);
        Check(VisiblePetals() > 0 && paper.alpha == 0,"花瓣应先于笺面出现");
        entrance.Sample(.40f);
        Check(paper.alpha > words.alpha && words.alpha > 0 && paper.alpha < 1,"花笺没有分层渐显");
        float partial = words.alpha;
        page.SetStory(character);
        Check(words.alpha == partial,"连点打断了花笺入场");
        string frames = Path.GetFullPath("Library/StoryEntranceFrames");
        Directory.CreateDirectory(frames);
        for (int i=0; i<=30; i++)
        {
            entrance.Sample(i / 25f);
            Capture(camera,frames,"frame-" + i.ToString("D3"));
        }
        Check(paper.alpha == 1 && words.alpha == 1 && VisiblePetals() == 0,"花笺收尾状态错误");
        page.SetStory(character);
        entrance.Sample(.22f);
        Check(VisiblePetals() > 0 && words.alpha == 1 && paper.alpha == 1,"重按时文案闪烁");
        page.SetTracking(false,false,true);
        Check(!story.activeSelf && VisiblePetals() == 0,"目标丢失后残留花瓣");
        page.SetStory(page.visit.characters[1]);
        page.SetTracking(true,true,true);
        Check(words.alpha == 0 && paper.alpha == 0,"新角色沿用了旧动画");
        entrance.Sample(.37f);
        Capture(camera,folder,"story-gathering");
        page.ShowScanner(false);
        Check(!story.activeInHierarchy && VisiblePetals() == 0,"离开扫描后残留花瓣");
        page.ShowScanner(true);
        page.SetTracking(true,true,true);
        Debug.Log("Story petal reveal, repeated tap, target change and scanner exit checks passed");
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
            var events = page.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
            var touch = new UnityEngine.EventSystems.PointerEventData(events);
            open.OnPointerDown(touch);
            Check(open.transform.localScale.x < 1,"按下按钮没有反馈");
            open.OnPointerUp(touch);
            Check(open.transform.localScale == Vector3.one,"抬起后按钮未复原");
            open.onClick.Invoke();
            Check(count.text.EndsWith("0 / 4") && grid.gameObject.activeInHierarchy,"首次打开花册失败");
            foreach(string title in new[]{"时见","星眠"})
            {
                var seat=grid.Find(title);
                Check(seat && seat.gameObject.activeInHierarchy && !seat.GetComponentInChildren<Button>() &&
                    seat.GetComponentInChildren<AlbumScenery>().texture,"隐藏席位配置错误");
            }
            Check(grid.GetComponentsInChildren<AlbumAtmosphere>().Length == 6,"目录应有六处常驻动效");
            CheckTitle(grid.Find("册名").GetComponent<InkTitle>(),camera,"Album");
            foreach (var character in visit.characters)
            {
                var card = grid.Find(character.referenceName).GetComponent<Button>();
                Check(!card.interactable,"未收集的角色可点选");
                card.onClick.Invoke();
                Check(!detail.gameObject.activeInHierarchy,"未收集的详情被打开");
            }
            Capture(camera,folder,"album-empty");
            var inset = (RectTransform)safe;
            var min = inset.anchorMin;
            var max = inset.anchorMax;
            inset.anchorMin = new Vector2(0,.025f);
            inset.anchorMax = new Vector2(1,.95f);
            var paper = (RectTransform)safe.parent.Find("花册底纸");
            Canvas.ForceUpdateCanvases();
            Check(paper.gameObject.activeInHierarchy && paper.rect.size == ((RectTransform)safe.parent).rect.size,
                "花册底色没有覆盖完整画布");
            var entrance = grid.GetComponent<FlowerPageEntrance>();
            entrance.SetProgress(.25f);
            Check(grid.GetComponent<CanvasGroup>().alpha > 0 && grid.GetComponent<CanvasGroup>().alpha < 1,"花册过渡未生效");
            entrance.SetProgress(1);
            Capture(camera,folder,"album-safe-area");
            inset.anchorMin = min;
            inset.anchorMax = max;
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
                Canvas.ForceUpdateCanvases();
                var scenery=detail.GetComponentInChildren<AlbumScenery>();
                Check(scenery.texture && scenery.uvRect.width > 0 &&
                    (scenery.uvRect.center.x >= .5f) == ((int)character.season % 2 == 1) &&
                    (scenery.uvRect.center.y >= .5f) == ((int)character.season < 2),"角色场景绑定错误");
                Check(detail.gameObject.activeInHierarchy && !grid.gameObject.activeSelf &&
                    detail.Find("题名").GetComponent<Text>().text == character.characterName &&
                    detail.Find("立绘").GetComponent<Image>().sprite == character.portrait.sprite &&
                    detail.Find("角色花笺/花语").GetComponent<Text>().text == character.excerpt,"详情映射错误");
                if (n == 1) Capture(camera,folder,"album-detail");
                if (n == 2 || n == 3) Capture(camera,folder,"album-detail-" + character.referenceName);
                if (character.referenceName == "winter_xueling") Capture(camera,folder,"album-detail-winter");
                detail.Find("返回花册").GetComponent<Button>().onClick.Invoke();
                if (n == 1 || n == visit.characters.Length) Capture(camera,folder,"album-" + n);
            }
            var artFrames = Path.GetFullPath("Library/AlbumAtmosphereFrames");
            Directory.CreateDirectory(artFrames);
            var large = camera.targetTexture;
            var small = RenderTexture.GetTemporary(432,960,24);
            try
            {
                camera.targetTexture=small;
                var air=grid.GetComponentsInChildren<AlbumAtmosphere>();
                Vector3 firstVertex=default;
                for(int i=0;i<48;i++)
                {
                    foreach(var a in air) a.Sample(i/12f);
                    Capture(camera,artFrames,"album-"+i.ToString("D3"));
                    var mesh=air[0].canvasRenderer.GetMesh();
                    Check(mesh && mesh.vertexCount > 0,"春季常驻花瓣网格为空");
                    if(i==0) firstVertex=mesh.vertices[0];
                    if(i==12) Check((mesh.vertices[0]-firstVertex).sqrMagnitude>.01f,"春季花瓣没有移动");
                }
            }
            finally { camera.targetTexture=large; RenderTexture.ReleaseTemporary(small); }
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
