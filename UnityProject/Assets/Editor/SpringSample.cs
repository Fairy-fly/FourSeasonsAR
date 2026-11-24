using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public static class SpringSample
{
    const string Art = "Assets/Art/";
    const string Scene = "Assets/Scenes/SpringVisit.unity";

    // 已生成的场景、角色预制体和参考图库可直接在 Inspector 中编辑和重建。
    public static void ReviewAndBuildApk()
    {
        EncounterReview.CheckSpring();
        SpringGaitReview.Run();
        SpringPageReview.Render();
        BuildApk();
    }

    public static void BuildApk()
    {
        CheckTrackingPolicy();
        CheckCardSwitch();
        if (!File.Exists(Scene)) { CreateScene(); ConfigureSeasons(); }
        var scene = EditorSceneManager.OpenScene(Scene);
        var visit = Object.FindAnyObjectByType<SpringVisit>();
        if (!visit || !visit.page || !visit.page.cover || !visit.page.brushFont || !visit.page.readingFont || visit.characters == null || visit.characters.Length != 4 ||
            visit.images.referenceLibrary is not XRReferenceImageLibrary library ||
            library.count != 4 || library[0].name != SpringVisit.ReferenceName ||
            Mathf.Abs(library[0].width - .080f) > .0001f)
            throw new BuildFailedException("春季场景或识别图库配置不完整");
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var character in visit.characters)
        {
            if (!character || !character.portrait.sprite || !character.petal || !ids.Add(character.referenceName) ||
                !character.portrait.TryGetComponent<BoxCollider>(out var hit) || hit.size.y <= 0)
                throw new BuildFailedException("无效或重复的角色映射");
            bool found = false;
            foreach (var reference in library)
                if (reference.name == character.referenceName && Mathf.Abs(reference.width - .08f) < .0001f) found = true;
            if (!found) throw new BuildFailedException("找不到 80 mm 识别图 " + character.referenceName);
            foreach (char ch in character.characterName)
                if (!visit.page.brushFont.HasCharacter(ch)) throw new BuildFailedException("人名字库缺字 " + ch);
            foreach (char ch in character.excerpt)
                if (!visit.page.readingFont.HasCharacter(ch)) throw new BuildFailedException("花笺字库缺字 " + ch);
            Debug.Log("Season mapping OK " + character.referenceName + " / " + character.characterName);
        }
        var art = visit.page.cover;
        float aspect = (float)art.width / art.height;
        if (Mathf.Abs(aspect - 841f / 1870) > .002f)
            throw new BuildFailedException("导入后的封面比例发生变化");
        foreach (float screen in new[] {9f/16, 9f/20, 9f/21})
        {
            Rect uv = SpringPage.CoverUV(screen, aspect);
            if (Mathf.Abs(aspect * uv.width / uv.height - screen) > .0001f ||
                uv.xMin < 0 || uv.xMax > 1.0001f || uv.yMin < 0 || uv.yMax > 1.0001f)
                throw new BuildFailedException("封面适配验证失败");
        }
        Debug.Log($"Spring portrait checks passed: {art.width}x{art.height}, 9:16 / 9:20 / 9:21");
        if (visit.characters[0].portrait.sprite.texture.height != 1777 ||
            Mathf.Abs(visit.characters[0].portrait.sprite.bounds.size.y - .09547f) > .0002f)
            throw new BuildFailedException("春知意高清立绘尺寸不正确");
        foreach (var hidden in new[]{AlbumModelViewer.TimeKeeper,AlbumModelViewer.StarDreamer})
        {
            if (!ids.Add(hidden) || !Resources.Load<Sprite>("Gallery/"+hidden+"/Portrait"))
                throw new BuildFailedException("隐藏席位或卡面错误 " + hidden);
        }
        foreach (var id in ids)
        {
            if (!AlbumModelViewer.HasModel(id)) throw new BuildFailedException("缺少展窗入口 " + id);
            string path = "Gallery/" + id + "/";
            var prefab = Resources.Load<GameObject>(path + "Figure");
            var filters = prefab ? prefab.GetComponentsInChildren<MeshFilter>() : null;
            if (filters == null || filters.Length != 1 || !filters[0].sharedMesh)
                throw new BuildFailedException("展示模型无效 " + path);
            var mesh = filters[0].sharedMesh;
            var triangles = mesh.GetIndexCount(0) / 3;
            if (mesh.subMeshCount != 1 || triangles < 10000 || triangles > 72000 ||
                !Resources.Load<Texture2D>(path + "Color") || !Resources.Load<Texture2D>(path + "Normal") ||
                !Resources.Load<Texture2D>(path + "Surface"))
                throw new BuildFailedException("展示模型预算或材质错误 " + path);
            Debug.Log($"Gallery model OK {id}: {triangles} triangles");
        }
        var shader = Resources.Load<Shader>("Gallery/FigureStudio");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new BuildFailedException("花册着色器错误");
        SpringBuild.Build(scene.path, "SeasonsFlowerJourney-2.0-alpha.5");
    }

    public static void ConfigurePolish()
    {
        var sprite = LoadSprite("spring_front_hd.png",1777f / (506f / 5300f),new Vector2(.5f,0));
        var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "spring_front_hd.png");
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 2048;
        android.format = TextureImporterFormat.RGBA32;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "spring_front_hd.png");
        const string prefab = "Assets/Prefabs/SpringSpirit.prefab";
        using (var edit = new PrefabUtility.EditPrefabContentsScope(prefab))
        {
            var portrait = edit.prefabContentsRoot.GetComponent<SpringSpirit>().portrait;
            portrait.sprite = sprite;
            var hit = portrait.GetComponent<BoxCollider>();
            var bounds = sprite.bounds;
            hit.center = bounds.center;
            hit.size = new Vector3(bounds.size.x,bounds.size.y,.004f);
        }
        var target = NamedBuildTarget.Android;
        Texture2D Icon(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Launcher/" + name + ".png");
        var legacy = Icon("flower_book");
        var foreground = Icon("flower_book_foreground");
        var background = Icon("flower_book_background");
        if (!legacy || !foreground || !background) throw new BuildFailedException("启动图标素材缺失");
        var adaptive = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
        foreach (var kind in new[] {adaptive, UnityEditor.Android.AndroidPlatformIconKind.Round, UnityEditor.Android.AndroidPlatformIconKind.Legacy})
        {
            var slots = PlayerSettings.GetPlatformIcons(target,kind);
            foreach (var slot in slots)
            {
                if (kind == adaptive)
                {
                    slot.SetTexture(background,0);
                    slot.SetTexture(foreground,1);
                }
                else slot.SetTexture(legacy);
            }
            PlayerSettings.SetPlatformIcons(target,kind,slots);
        }
        AssetDatabase.SaveAssets();
    }

    static void CheckCardSwitch()
    {
        var focus = new CardFocus();
        var cards = new[] { new TrackableId(1, 1), new TrackableId(2, 1), new TrackableId(3, 1), new TrackableId(4, 1) };
        foreach (var card in cards)
        {
            focus.Observe(card, CardObservation.FromMethod(1));
            focus.FinishChanges();
            if (focus.Current != card) throw new BuildFailedException("新卡未取得显示权");
            // The provider can repeat full updates for previously acquired targets.
            foreach (var prior in cards)
                if (prior.subId1 < card.subId1) focus.Observe(prior, TrackingState.Tracking);
            focus.FinishChanges();
            if (focus.Current != card) throw new BuildFailedException("旧卡重复更新抢占了角色");
        }
        focus.Observe(cards[0], CardObservation.FromMethod(2));
        focus.Observe(cards[3], CardObservation.FromMethod(2));
        focus.FinishChanges();
        if (focus.State != TrackingState.Limited) throw new BuildFailedException("旧姿态被视为直接识别");
        focus.Observe(cards[0], CardObservation.FromMethod(1));
        focus.Remove(cards[3]);
        focus.FinishChanges();
        if (focus.Current != cards[0]) throw new BuildFailedException("春卡重新识别失败");
        focus.Remove(cards[0]);
        focus.FinishChanges();
        if (focus.Current.HasValue) throw new BuildFailedException("移除后仍存在当前目标");
        Debug.Log("Season handoff regression passed");
    }

    static void CheckTrackingPolicy()
    {
        TrackingVisibility state = default;
        state.Update(TrackingState.Limited, 0, .35f);
        bool initial = state.IsVisible(0);
        state.Update(TrackingState.Tracking, 1, .35f);
        bool acquired = state.IsVisible(1);
        state.Update(TrackingState.Limited, 2, .35f);
        state.Update(TrackingState.Limited, 2.2f, .35f);
        bool buffered = state.IsVisible(2.3f);
        bool expired = !state.IsVisible(2.36f);
        state.Update(TrackingState.Tracking, 3, .35f);
        bool recovered = state.IsVisible(3);
        state.Update(TrackingState.None, 3.1f, .35f);
        if (initial || !acquired || !buffered || !expired || !recovered || state.IsVisible(3.1f))
            throw new BuildFailedException("追踪可见性测试失败");
        Debug.Log("Spring tracking policy checks passed");
    }

    static void CreateScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SpringCamera.unity");
        var origin = Object.FindAnyObjectByType<XROrigin>();
        var session = Object.FindAnyObjectByType<ARSession>();
        var manager = origin.gameObject.AddComponent<ARTrackedImageManager>();
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>("Assets/SpringCards.asset");
        if (!library)
        {
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, "Assets/SpringCards.asset");
            library.Add();
            library.SetName(0, SpringVisit.ReferenceName);
            library.SetTexture(0, AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "spring_track_v2.png"), false);
            library.SetSpecifySize(0, true);
            library.SetSize(0, new Vector2(.08f, .08f));
            EditorUtility.SetDirty(library);
        }
        manager.referenceLibrary = library;
        manager.requestedMaxNumberOfMovingImages = 1;
        var main = new GameObject("四时花旅");
        var visit = main.AddComponent<SpringVisit>();
        visit.page = main.AddComponent<SpringPage>();
        visit.page.visit = visit;
        visit.page.cover = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "spring_cover_portrait.png");
        visit.page.brushFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Typography/FlowerJourneyBrush.ttf");
        visit.page.readingFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Typography/FlowerJourneyText.ttf");
        visit.images = manager;
        visit.arCamera = origin.Camera;
        visit.session = session;
        visit.characters = new[] { AssetDatabase.LoadAssetAtPath<SpringSpirit>("Assets/Prefabs/SpringSpirit.prefab") };
        origin.gameObject.SetActive(false);
        session.gameObject.SetActive(false);
        if (!EditorSceneManager.SaveScene(main.scene, Scene)) throw new BuildFailedException("保存场景失败");
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureSeasons()
    {
        EditorSceneManager.OpenScene(Scene);
        var visit = Object.FindAnyObjectByType<SpringVisit>();
        var library = (XRReferenceImageLibrary)visit.images.referenceLibrary;
        var characters = new System.Collections.Generic.List<SpringSpirit> {
            AssetDatabase.LoadAssetAtPath<SpringSpirit>("Assets/Prefabs/SpringSpirit.prefab")
        };
        void Add(string key, string id, SpringSpirit.Season season, string title, string text)
        {
            int index = 0;
            while (index < library.count && library[index].name != id) index++;
            if (index == library.count) library.Add();
            library.SetName(index, id);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + key + "_track_v1.png");
            if (!texture) throw new BuildFailedException("缺少识别图 " + key);
            library.SetTexture(index, texture, false);
            library.SetSpecifySize(index, true);
            library.SetSize(index, Vector2.one * .08f);
            characters.Add(MakeCharacter(key, id, season, title, text));
        }
        Add("summer", "summer_huajian", SpringSpirit.Season.Summer, "夏日花涧", "灯芯里盛着夏夜晚风与蛙鸣。");
        Add("autumn", "autumn_manwu", SpringSpirit.Season.Autumn, "秋叶漫舞", "能接住一捧从容又温柔的秋光。");
        Add("winter", "winter_xueling", SpringSpirit.Season.Winter, "冬雪灵", "她守着一方雪境，等晚归的人回房间。");
        visit.characters = characters.ToArray();
        EditorUtility.SetDirty(library);
        EditorUtility.SetDirty(visit);
        if (!EditorSceneManager.SaveScene(visit.gameObject.scene)) throw new BuildFailedException("保存四季场景失败");
        AssetDatabase.SaveAssets();
    }

    static SpringSpirit MakeCharacter(string key, string id, SpringSpirit.Season season, string title, string text)
    {
        var obj = new GameObject(title);
        var spirit = obj.AddComponent<SpringSpirit>();
        spirit.season = season;
        spirit.referenceName = id;
        spirit.characterName = title;
        spirit.excerpt = text;
        var portrait = new GameObject("正面立绘");
        portrait.transform.SetParent(obj.transform, false);
        spirit.portrait = portrait.AddComponent<SpriteRenderer>();
        spirit.portrait.sprite = LoadSprite(key + "_front.png", 15360, new Vector2(.5f, 0));
        var bounds = spirit.portrait.sprite.bounds;
        var hit = portrait.AddComponent<BoxCollider>();
        hit.center = bounds.center;
        hit.size = new Vector3(bounds.size.x, bounds.size.y, .004f);
        spirit.petal = LoadSprite(key + "_effect.png", 7600, new Vector2(.5f, .5f));
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/" + key + ".prefab");
        Object.DestroyImmediate(obj);
        return prefab.GetComponent<SpringSpirit>();
    }

    static Sprite LoadSprite(string file, float pixelsPerMeter, Vector2 pivot)
    {
        var path = Art + file;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spritePixelsPerUnit = pixelsPerMeter;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
