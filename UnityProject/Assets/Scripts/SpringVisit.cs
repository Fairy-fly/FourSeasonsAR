using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class SpringVisit : MonoBehaviour
{
    public const string ReferenceName = "spring_zhiyi";
    public ARSession session;
    public ARTrackedImageManager images;
    public SpringSpirit prefab;
    public SpringPage page;
    public Camera arCamera;
    [Min(0)] public float limitedGrace = .35f;

    readonly Dictionary<TrackableId, SpringSpirit> instances = new();
    bool starting;
    bool scanning;
    string description = "";
    float storyUntil;

    void OnEnable() => images.trackablesChanged.AddListener(Changed);
    void OnDisable() => images.trackablesChanged.RemoveListener(Changed);

    public void Begin()
    {
        if (!starting && !scanning) StartCoroutine(Enter());
    }

    IEnumerator Enter()
    {
        starting = true;
        page.Message("正在检查相机与 AR 服务…");
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            bool answered = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => answered = true;
            callbacks.PermissionDenied += _ => answered = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => answered = true;
            Permission.RequestUserPermission(Permission.Camera, callbacks);
            yield return new WaitUntil(() => answered);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            page.Message("请在手机的应用权限中允许使用相机，再点开始扫描。");
            starting = false;
            yield break;
        }
        yield return ARSession.CheckAvailability();
        if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();
        if (ARSession.state != ARSessionState.Ready && ARSession.state != ARSessionState.SessionTracking &&
            ARSession.state != ARSessionState.SessionInitializing)
        {
            page.Message("AR 服务暂不可用，请检查 Google Play Services for AR 后重试。");
            starting = false;
            yield break;
        }
        scanning = true;
        starting = false;
        images.gameObject.SetActive(true);
        session.gameObject.SetActive(true);
        page.ShowScanner(true);
    }

    public void Home()
    {
        scanning = false;
        images.gameObject.SetActive(false);
        session.gameObject.SetActive(false);
        foreach (var spirit in instances.Values) if (spirit) Destroy(spirit.gameObject);
        instances.Clear();
        storyUntil = 0;
        page.ShowScanner(false);
    }

    void Changed(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var item in changes.removed)
            if (instances.Remove(item.Key, out var spirit) && spirit) Destroy(spirit.gameObject);
        foreach (var image in changes.added) Apply(image);
        foreach (var image in changes.updated) Apply(image);
    }

    void Apply(ARTrackedImage image)
    {
        if (!scanning || image.referenceImage.name != ReferenceName) return;
        if (!instances.TryGetValue(image.trackableId, out var spirit) || !spirit)
        {
            if (image.trackingState != TrackingState.Tracking) return;
            spirit = Instantiate(prefab, image.transform);
            instances[image.trackableId] = spirit;
            Debug.Log("SpringVisit detected " + image.trackableId);
        }
        spirit.Track(image.trackingState, limitedGrace);
    }

    void Update()
    {
        if (!scanning) return;
        bool visible = false;
        foreach (var spirit in instances.Values)
            if (spirit && spirit.gameObject.activeSelf) visible = true;
        var pointer = Pointer.current;
        if (visible && pointer != null && pointer.press.wasPressedThisFrame &&
            Physics.Raycast(arCamera.ScreenPointToRay(pointer.position.ReadValue()), out var hit) &&
            hit.collider.GetComponentInParent<SpringSpirit>() is SpringSpirit selected)
        {
            selected.Blossom();
            description = "她性子软，却总悄悄攒着勇气，把第一缕春风捎给赶路的人。";
            storyUntil = Time.unscaledTime + 5;
            Debug.Log("SpringVisit tapped spring_zhiyi");
        }
        page.Message(visible ? (Time.unscaledTime < storyUntil ? description : "春知意 · 2.5D　轻触精灵，看樱花绽开")
            : ARSession.state == ARSessionState.SessionTracking ? "将整张春季测试卡放入画面，保持光线均匀" : "正在定位，请缓慢移动手机…");
    }
}
