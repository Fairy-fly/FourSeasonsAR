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
        string folder = Path.GetFullPath("../reports/evidence/ui-2.0-rc.8-editor");
        Directory.CreateDirectory(folder);
        try
        {
            Capture(camera,folder,"cover");
            CheckTitle(safe.Find("封面/书名").GetComponent<InkTitle>(),camera,"Home");
            CheckAlbum(page,safe,camera,folder);
            CheckMovement(page,safe,camera,folder);
            CheckAppearance(page,safe,camera,folder);
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

    static void CheckMovement(SpringPage page,Transform safe,Camera camera,string folder)
    {
        var key=MovementPreference.Key;
        bool stored=PlayerPrefs.HasKey(key);
        int original=PlayerPrefs.GetInt(key);
        var options=new[] {MovementPreference.Speed,MovementPreference.Turn,MovementPreference.Clearance,MovementPreference.Amplitude};
        var titles=new[] {"移动速度","转向速度","离地高度","起伏幅度"};
        var saved=System.Array.ConvertAll(options,x=>PlayerPrefs.GetInt(x.Key,int.MinValue));
        var settings=safe.Find("移动设置");
        var hover=settings.Find("漂浮");
        var walk=settings.Find("步行");
        var speedUp=settings.Find("移动速度/增加").GetComponent<Button>();
        var turnUp=settings.Find("转向速度/增加").GetComponent<Button>();
        var heightUp=settings.Find("离地高度/增加").GetComponent<Button>();
        var bobUp=settings.Find("起伏幅度/增加").GetComponent<Button>();
        try
        {
            PlayerPrefs.DeleteKey(key);
            foreach(var option in options)PlayerPrefs.DeleteKey(option.Key);
            var albumEntry=(RectTransform)safe.Find("封面/四季花册/漆面");
            var settingsEntry=(RectTransform)safe.Find("封面/设置/漆面");
            var scanSlot=(RectTransform)safe.Find("封面/开始扫描位置");
            var settingsButton=(RectTransform)settingsEntry.parent;
            var albumButton=(RectTransform)albumEntry.parent;
            if(albumEntry.offsetMin!=settingsEntry.offsetMin ||
                albumEntry.GetComponent<SpringSurface>().color!=settingsEntry.GetComponent<SpringSurface>().color)
                throw new System.InvalidOperationException("Cover buttons must share the same finish and border");
            if(scanSlot.anchorMax.y>.25f || settingsButton.anchorMax.y>=scanSlot.anchorMin.y ||
                settingsButton.anchorMax.y!=albumButton.anchorMax.y || settingsButton.anchorMax.x>=albumButton.anchorMin.x)
                throw new System.InvalidOperationException("Cover actions must occupy two separate rows at the bottom");
            safe.Find("封面/设置").GetComponent<Button>().onClick.Invoke();
            if(!settings.gameObject.activeSelf || MovementPreference.ExperimentalWalk ||
                !hover.Find("笺纸/状态").gameObject.activeSelf ||
                walk.Find("笺纸/说明").GetComponent<Text>().text!="实验功能 · 测试阶段")
                throw new System.InvalidOperationException("Default movement and experimental label failed");
            foreach(var text in settings.GetComponentsInChildren<Text>(true))
            foreach(char glyph in text.text)
                if(!char.IsWhiteSpace(glyph) && !page.readingFont.HasCharacter(glyph))
                    throw new System.InvalidOperationException("Movement settings font missing "+glyph);
            settings.GetComponent<FlowerPageEntrance>().SetProgress(1);
            Capture(camera,folder,"movement-settings-hover");

            for(int i=0;i<options.Length;i++)
            {
                var option=options[i];
                var row=settings.Find(titles[i]);
                var increase=row.Find("增加").GetComponent<Button>();
                var decrease=row.Find("减少").GetComponent<Button>();
                var text=row.Find("数值").GetComponent<Text>();
                if(text.text!=option.Default+option.Unit)
                    throw new System.InvalidOperationException("Unexpected default adjustment "+row.name);
                for(int presses=0;presses<20;presses++)increase.onClick.Invoke();
                if(option.Value!=option.Max || increase.interactable || text.text!=option.Max+option.Unit)
                    throw new System.InvalidOperationException("Upper tuning limit failed "+row.name);
                for(int presses=0;presses<20;presses++)decrease.onClick.Invoke();
                if(option.Value!=option.Min || decrease.interactable || text.text!=option.Min+option.Unit)
                    throw new System.InvalidOperationException("Lower tuning limit failed "+row.name);
                increase.onClick.Invoke();
            }
            walk.GetComponent<Button>().onClick.Invoke();
            settings.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            safe.Find("封面/设置").GetComponent<Button>().onClick.Invoke();
            if(!MovementPreference.ExperimentalWalk || !walk.Find("笺纸/状态").gameObject.activeSelf ||
                !speedUp.interactable || !turnUp.interactable || heightUp.interactable || bobUp.interactable)
                throw new System.InvalidOperationException("Experimental gait selection did not persist");
            heightUp.onClick.Invoke();
            for(int i=0;i<options.Length;i++)
                if(options[i].Value!=options[i].Min+options[i].Step)
                    throw new System.InvalidOperationException("Settings were not restored on reopen or walk changed a hover setting");
            settings.GetComponent<FlowerPageEntrance>().SetProgress(1);
            Capture(camera,folder,"movement-settings-walk");
            hover.GetComponent<Button>().onClick.Invoke();
            if(MovementPreference.ExperimentalWalk)throw new System.InvalidOperationException("Hover toggle failed");
            settings.Find("返回封面").GetComponent<Button>().onClick.Invoke();
        }
        finally
        {
            if(stored)PlayerPrefs.SetInt(key,original);
            else PlayerPrefs.DeleteKey(key);
            for(int i=0;i<options.Length;i++)
                if(saved[i]==int.MinValue)PlayerPrefs.DeleteKey(options[i].Key);
                else PlayerPrefs.SetInt(options[i].Key,saved[i]);
            PlayerPrefs.Save();
        }
        Debug.Log("Movement settings defaults, bounds, mode availability, persistence, button styling and glyphs passed");
    }

    static void CheckAppearance(SpringPage page,Transform safe,Camera camera,string folder)
    {
        const int unset=-1;
        string cardKey=SpringPage.CardStyleKey, buttonKey=SpringPage.ButtonStyleKey;
        int priorCard=PlayerPrefs.GetInt(cardKey,unset), priorButton=PlayerPrefs.GetInt(buttonKey,unset);
        var settings=safe.Find("移动设置");
        var appearance=safe.Find("外观设置");
        var settingsButton=safe.Find("封面/设置").GetComponent<Button>();
        var albumButton=safe.Find("封面/四季花册").GetComponent<Button>();
        var album=safe.Find("花册/目录");
        var cardRim=album.Find(page.visit.characters[0].referenceName).GetComponent<SpringSurface>();
        int Vertices(SpringSurface surface)
        {
            Canvas.ForceUpdateCanvases();
            return surface.canvasRenderer.GetMesh().vertexCount;
        }
        void Choose(string path)=>appearance.Find(path).GetComponent<Button>().onClick.Invoke();
        try
        {
            settingsButton.onClick.Invoke();
            settings.Find("外观样式").GetComponent<Button>().onClick.Invoke();
            Choose("人物卡框/圆角");
            Choose("按钮样式/折角");
            appearance.GetComponent<FlowerPageEntrance>().SetProgress(1);
            Capture(camera,folder,"appearance-options");
            Choose("返回设置");
            settings.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            if(Vertices(albumButton.GetComponent<SpringSurface>())!=9)
                throw new System.InvalidOperationException("The cover button did not apply the chosen bevel");
            Capture(camera,folder,"cover-beveled");
            albumButton.onClick.Invoke();
            if(Vertices(cardRim)!=101 || !cardRim.transform.Find("笺纸").GetComponent<Mask>())
                throw new System.InvalidOperationException("The card frame did not use the chosen rounding and clipping");
            Capture(camera,folder,"album-rounded");
            album.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            settingsButton.onClick.Invoke();
            settings.Find("外观样式").GetComponent<Button>().onClick.Invoke();
            if(PlayerPrefs.GetInt(cardKey)!=(int)FrameStyle.Rounded ||
                PlayerPrefs.GetInt(buttonKey)!=(int)FrameStyle.Beveled ||
                !appearance.Find("人物卡框/圆角/笺纸/状态").gameObject.activeSelf ||
                !appearance.Find("按钮样式/折角/笺纸/状态").gameObject.activeSelf)
                throw new System.InvalidOperationException("Card and button choices were not saved independently");
            Choose("人物卡框/折角");
            Choose("按钮样式/圆角");
            Choose("返回设置");
            settings.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            albumButton.onClick.Invoke();
            if(Vertices(cardRim)!=9 || Vertices(album.Find("返回封面").GetComponent<SpringSurface>())!=101)
                throw new System.InvalidOperationException("Card and button frame geometry did not update after reversing the choices");
            Capture(camera,folder,"album-beveled");
            album.Find("返回封面").GetComponent<Button>().onClick.Invoke();
        }
        finally
        {
            if(priorCard==unset)PlayerPrefs.DeleteKey(cardKey);else PlayerPrefs.SetInt(cardKey,priorCard);
            if(priorButton==unset)PlayerPrefs.DeleteKey(buttonKey);else PlayerPrefs.SetInt(buttonKey,priorButton);
            PlayerPrefs.Save();
        }
        Debug.Log("Cover layout, all frame geometries and independent appearance selections passed");
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
                Check(seat && seat.gameObject.activeInHierarchy && seat.GetComponent<Button>() &&
                    seat.GetComponentInChildren<AlbumScenery>().texture,"隐藏席位配置错误");
                string id=title=="时见" ? AlbumModelViewer.TimeKeeper : AlbumModelViewer.StarDreamer;
                seat.GetComponent<Button>().onClick.Invoke();
                Check(detail.gameObject.activeInHierarchy && detail.Find("题名").GetComponent<Text>().text==title &&
                    detail.Find("立绘").GetComponent<Image>().sprite==Resources.Load<Sprite>("Gallery/"+id+"/Portrait"),"隐藏款详情映射错误");
                var portraitImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Gallery/"+id+"/Portrait.png");
                Check(portraitImporter.DoesSourceTextureHaveAlpha() && portraitImporter.GetPlatformTextureSettings("Android").format == TextureImporterFormat.ETC2_RGBA8,"隐藏立绘缺少透明通道");
                Check(!detail.Find("画境").gameObject.activeSelf && !detail.Find("花钿位置") && !grid.Find("花钿位置") && !detail.Find("查看 3D/嵌金") &&
                    detail.Find("角色花笺/笺纸").GetComponent<SpringSurface>(),"隐藏款花笺或场景配置错误");
                Capture(camera,folder,id+"-portrait");
                var face = detail.Find("查看 3D/漆面").GetComponent<SpringSurface>();
                var colors = face.canvasRenderer.GetMesh().colors32;
                Check(colors.Length > 2 && System.Array.Exists(colors,c => !c.Equals(colors[0])),"模式切换按钮的渐变未绘制");
                var label = (RectTransform)detail.Find("查看 3D/按钮文字");
                Check(Mathf.Abs(label.anchorMin.x + label.anchorMax.x - 1) < .001f,"模式切换按钮未居中");
                CheckEnamelMotion(detail,camera,id,touch);
                CheckModelView(detail,detail.Find("查看 3D").GetComponent<Button>(),camera,folder,id,touch);
                detail.Find("返回花册").GetComponent<Button>().onClick.Invoke();
                Check(!visit.Album.Contains(id) && count.text.EndsWith("0 / 4"),"隐藏预览改变了收藏进度");
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
                var cardPortrait = card.transform.Find("笺纸/季景/立绘").GetComponent<Image>();
                Check(card.interactable && cardPortrait.isActiveAndEnabled && cardPortrait.sprite == character.portrait.sprite,"卡位立绘错误");
                card.onClick.Invoke();
                Canvas.ForceUpdateCanvases();
                var scenery=detail.GetComponentInChildren<AlbumScenery>();
                var cardPaper=card.transform.Find("笺纸/季景").GetComponent<AlbumScenery>();
                Check(scenery.texture && scenery.uvRect.width>0 && scenery.texture==cardPaper.texture &&
                    scenery.color==cardPaper.color && scenery.texture==paper.GetComponent<AlbumScenery>().texture,
                    "角色卡片与详情底纸不匹配");
                Check(detail.gameObject.activeInHierarchy && !grid.gameObject.activeSelf &&
                    detail.Find("题名").GetComponent<Text>().text == character.characterName &&
                    detail.Find("立绘").GetComponent<Image>().sprite == character.portrait.sprite &&
                    detail.Find("角色花笺/花语").GetComponent<Text>().text == character.excerpt,"详情映射错误");
                if (n == 1) Capture(camera,folder,"album-detail");
                Check(detail.Find("来到现实").gameObject.activeSelf==FlowerCompanion.IsAvailable(character.referenceName),
                    "现实同行入口与可动角色不一致");
                var modelButton = detail.Find("查看 3D").GetComponent<Button>();
                Check(modelButton.gameObject.activeSelf,
                    "3D 入口与模型映射不一致");
                if (modelButton.gameObject.activeSelf)
                {
                    CheckModelView(detail,modelButton,camera,folder,character.referenceName,touch);
                    // Also leave the detail page directly from 3D.
                    modelButton.onClick.Invoke();
                }
                if (n == 2 || n == 3) Capture(camera,folder,"album-detail-" + character.referenceName);
                if (character.referenceName == "winter_xueling") Capture(camera,folder,"album-detail-winter");
                detail.Find("返回花册").GetComponent<Button>().onClick.Invoke();
                Check(!detail.Find("模型展窗").GetComponent<RawImage>().texture,"返回花册未释放模型画面");
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
            var hiddenPortrait=detail.Find("立绘").GetComponent<Button>();
            var invite=detail.Find("来到现实");
            var back=detail.Find("返回花册").GetComponent<Button>();
            grid.Find("时见").GetComponent<Button>().onClick.Invoke();
            Check(hiddenPortrait.interactable && !invite.gameObject.activeSelf,"时见的相遇与现实同行权限错误");
            hiddenPortrait.onClick.Invoke();
            hiddenPortrait.onClick.Invoke();
            Check(visit.Album.Contains(AlbumModelViewer.TimeKeeper) && !visit.Album.Contains(AlbumModelViewer.StarDreamer) && invite.gameObject.activeSelf,"隐藏身份收录串联");
            back.onClick.Invoke();
            string seasonId=visit.characters[0].referenceName;
            visit.Album.Reset(new[]{seasonId});
            grid.Find("星眠").GetComponent<Button>().onClick.Invoke();
            hiddenPortrait.onClick.Invoke();
            Check(!hiddenPortrait.interactable && !visit.Album.Contains(AlbumModelViewer.StarDreamer) && visit.Album.Contains(AlbumModelViewer.TimeKeeper),"部分重置后的隐藏资格不正确");
            back.onClick.Invoke();
            visit.Album.Collect(seasonId);
            grid.Find("星眠").GetComponent<Button>().onClick.Invoke();
            hiddenPortrait.onClick.Invoke();
            back.onClick.Invoke();
            Check(visit.Album.Contains(AlbumModelViewer.StarDreamer) && count.text.EndsWith("4 / 4"),"第二隐藏相遇或四季计数错误");
            Check(CompanionProfile.OwnedCount(visit.Album)==6 &&
                CompanionProfile.NextOwned("winter_xueling",visit.Album).Id==AlbumModelViewer.TimeKeeper &&
                CompanionProfile.NextOwned(AlbumModelViewer.StarDreamer,visit.Album).Id==SpringVisit.ReferenceName,
                "原地轮换应经过六名已收录角色并回到春知意");
            grid.Find("返回封面").GetComponent<Button>().onClick.Invoke();
            Debug.Log("Album persistence, duplicate collection, detail mapping and navigation checks passed");
        }
        finally
        {
            visit.Album = live;
            foreach (var character in visit.characters) PlayerPrefs.DeleteKey(prefix + character.referenceName);
            PlayerPrefs.DeleteKey(prefix+AlbumModelViewer.TimeKeeper);
            PlayerPrefs.DeleteKey(prefix+AlbumModelViewer.StarDreamer);
            PlayerPrefs.Save();
        }
    }

    static void CheckModelView(Transform detail,Button button,Camera camera,string folder,string id,
        UnityEngine.EventSystems.PointerEventData touch)
    {
        button.onClick.Invoke();
        var viewer=detail.Find("模型展窗").GetComponent<AlbumModelViewer>();
        var output=viewer.GetComponent<RawImage>();
        if (!viewer.gameObject.activeInHierarchy || detail.Find("立绘").gameObject.activeSelf)
            throw new System.InvalidOperationException("卡片未切入 3D");
        var small=new Texture2D(64,64,TextureFormat.RGBA32,false);
        Color32[] ReadModel()
        {
            var frame=output.texture as RenderTexture;
            if (!frame || !frame.IsCreated()) throw new System.InvalidOperationException("模型相机没有输出");
            var target=RenderTexture.GetTemporary(64,64,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;
            try {
                Graphics.Blit(frame,target);
                RenderTexture.active=target;
                small.ReadPixels(new Rect(0,0,64,64),0,0);
                small.Apply();
                var pixels=small.GetPixels32();
                int covered=System.Array.FindAll(pixels,c=>c.a>180).Length;
                if (covered<380 || covered>3600) throw new System.InvalidOperationException("模型轮廓异常 "+covered);
                return pixels;
            } finally { RenderTexture.active=previous; RenderTexture.ReleaseTemporary(target); }
        }
        void Changed(Color32[] first,Color32[] second)
        {
            int changed=0;
            for(int i=0;i<first.Length;i++)
                if(Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].a-second[i].a)>12) changed++;
            if (changed<100) throw new System.InvalidOperationException("模型视角或缩放没有变化");
        }
        try
        {
            var front=ReadModel();
            Capture(camera,folder,id+"-model-front");
            touch.pointerId=1;
            touch.position=new Vector2(140,200);
            viewer.OnPointerDown(touch);
            touch.position+=new Vector2(150,0);
            viewer.OnDrag(touch);
            var dragged=ReadModel();
            Changed(front,dragged);
            var other=new UnityEngine.EventSystems.PointerEventData(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()) {pointerId=2,position=touch.position+new Vector2(60,0)};
            viewer.OnPointerDown(other);
            other.position+=new Vector2(32,0);
            viewer.OnDrag(other);
            Changed(dragged,ReadModel());
            viewer.OnPointerUp(other);
            viewer.OnPointerUp(touch);
            foreach (var pose in new[]{(35,"angle"),(90,"side"),(180,"back")})
            {
                viewer.SetView(pose.Item1,0,1);
                Changed(front,ReadModel());
                Capture(camera,folder,id+"-model-"+pose.Item2);
            }
            button.onClick.Invoke();
            if (output.texture || !detail.Find("立绘").gameObject.activeSelf)
                throw new System.InvalidOperationException("切回立绘失败");
            Debug.Log(id + " 3D front, side, reverse, drag, pinch and 2D return verified");
        } finally { Object.DestroyImmediate(small); }
    }

    static void CheckEnamelMotion(Transform detail,Camera camera,string id,UnityEngine.EventSystems.PointerEventData touch)
    {
        var button=detail.Find("查看 3D").GetComponent<FlowerButton>();
        var air=detail.GetComponentInChildren<AlbumAtmosphere>();
        var full=camera.targetTexture;
        var small=RenderTexture.GetTemporary(432,960,24);
        string folder=Path.GetFullPath("Library/EnamelFrames/"+id);
        Directory.CreateDirectory(folder);
        try
        {
            camera.targetTexture=small;
            for (int i=0;i<72;i++)
            {
                float t=i/24f;
                Shader.SetGlobalFloat("_AlbumReviewClock",t+.15f);
                air.Sample(t);
                if (i==52) button.OnPointerDown(touch);
                if (i>=52 && i<=62) button.flare.Sample((i-52)/24f);
                if (i==57) button.OnPointerUp(touch);
                Capture(camera,folder,"frame-"+i.ToString("D3"));
                if (i==54 && (!button.flare.isActiveAndEnabled || button.flare.canvasRenderer.GetMesh().vertexCount<9))
                    throw new System.InvalidOperationException("按钮金屑未绘制");
            }
        }
        finally
        {
            Shader.SetGlobalFloat("_AlbumReviewClock",0);
            button.OnPointerUp(touch);
            camera.targetTexture=full;
            RenderTexture.ReleaseTemporary(small);
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

