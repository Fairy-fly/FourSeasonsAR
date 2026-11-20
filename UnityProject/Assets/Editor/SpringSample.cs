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
    public static void BuildApk()
    {
        CheckTrackingPolicy();
        if (!File.Exists(Scene)) CreateScene();
        var scene = EditorSceneManager.OpenScene(Scene);
        var visit = Object.FindAnyObjectByType<SpringVisit>();
        if (!visit || !visit.page || !visit.page.cover || !visit.prefab || !visit.prefab.portrait.sprite ||
            visit.images.referenceLibrary is not XRReferenceImageLibrary library ||
            library.count != 1 || library[0].name != SpringVisit.ReferenceName ||
            Mathf.Abs(library[0].width - .080f) > .0001f)
            throw new BuildFailedException("春季场景或识别图库配置不完整");
        SpringBuild.Build(scene.path, "SeasonsFlowerJourney-Spring");
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
        visit.page.cover = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "cover.jpg");
        visit.images = manager;
        visit.arCamera = origin.Camera;
        visit.session = session;
        visit.prefab = MakeCharacter();
        origin.gameObject.SetActive(false);
        session.gameObject.SetActive(false);
        if (!EditorSceneManager.SaveScene(main.scene, Scene)) throw new BuildFailedException("保存场景失败");
        AssetDatabase.SaveAssets();
    }

    static SpringSpirit MakeCharacter()
    {
        var obj = new GameObject("春知意");
        var spirit = obj.AddComponent<SpringSpirit>();
        var portrait = new GameObject("正面立绘");
        portrait.transform.SetParent(obj.transform, false);
        spirit.portrait = portrait.AddComponent<SpriteRenderer>();
        spirit.portrait.sprite = LoadSprite("spring_front.png", 5300, new Vector2(.5f, 0));
        var bounds = spirit.portrait.sprite.bounds;
        var hit = portrait.AddComponent<BoxCollider>();
        hit.center = bounds.center;
        hit.size = new Vector3(bounds.size.x, bounds.size.y, .004f);
        spirit.petal = LoadSprite("petal.png", 8000, new Vector2(.5f, .5f));
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/SpringSpirit.prefab");
        Object.DestroyImmediate(obj);
        return prefab.GetComponent<SpringSpirit>();
    }

    static Sprite LoadSprite(string file, float pixelsPerMeter, Vector2 pivot)
    {
        var path = Art + file;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
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
