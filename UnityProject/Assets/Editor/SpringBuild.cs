using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public static class SpringBuild
{
    const string ScenePath = "Assets/Scenes/SpringCamera.unity";

    // 使用 AR Foundation 自带的创建菜单，保留移动 AR 相机的位姿输入绑定。
    public static void CameraApk()
    {
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
            Selection.activeGameObject = null;
            if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (Mobile AR)") ||
                Camera.main == null || Camera.main.GetComponent<ARCameraBackground>() == null)
                throw new BuildFailedException("移动 AR 相机创建失败");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new BuildFailedException("无法保存春季相机场景");
        }
        Build(ScenePath, "SpringCamera");
    }

    public static void Build(string scene, string filename)
    {
        const BuildTargetGroup group = BuildTargetGroup.Android;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget xr))
        {
            xr = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(xr, "Assets/SeasonsXRSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, xr, true);
        }
        if (!xr.HasManagerSettingsForBuildTarget(group)) xr.CreateDefaultManagerSettingsForBuildTarget(group);
        if (!XRPackageMetadataStore.AssignLoader(xr.ManagerSettingsForBuildTarget(group), "UnityEngine.XR.ARCore.ARCoreLoader", group))
            throw new BuildFailedException("无法启用 Android ARCore loader");
        PlayerSettings.productName = "四时花旅";
        PlayerSettings.companyName = "Seasons Flower Journey";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.seasonsflowerjourney.spring");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene, true) };
        EditorUserBuildSettings.buildAppBundle = false;
        AssetDatabase.SaveAssets();
        var output = Path.GetFullPath(Path.Combine("..", "builds", filename + ".apk"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new[] { scene }, output, BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Android 构建结果：" + report.summary.result);
        Debug.Log("Android APK: " + output);
    }
}
