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
    public SpringSpirit[] characters;
    readonly Dictionary<string, SpringSpirit> byReference = new();
    SpringSpirit storyOwner;
    public SpringPage page;
    public Camera arCamera;
    [Min(0)] public float limitedGrace = .35f;

    readonly Dictionary<TrackableId, SpringSpirit> instances = new();
    bool starting;
    bool scanning;
    float storyUntil;

    void Awake()
    {
        foreach (var character in characters) byReference.Add(character.referenceName, character);
    }

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
        storyOwner = null;
        page.ShowScanner(false);
    }

    void Changed(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var item in changes.removed)
            if (instances.Remove(item.Key, out var spirit) && spirit)
            {
                spirit.gameObject.SetActive(false);
                Destroy(spirit.gameObject);
            }
        foreach (var image in changes.added) Apply(image);
        foreach (var image in changes.updated) Apply(image);
    }

    void Apply(ARTrackedImage image)
    {
        if (!scanning || !byReference.TryGetValue(image.referenceImage.name, out var prefab)) return;
        instances.TryGetValue(image.trackableId, out var spirit);
        if (spirit && spirit.referenceName != prefab.referenceName)
        {
            spirit.gameObject.SetActive(false);
            Destroy(spirit.gameObject);
            instances.Remove(image.trackableId);
            spirit = null;
        }
        if (!spirit)
        {
            if (image.trackingState != TrackingState.Tracking) return;
            spirit = Instantiate(prefab, image.transform);
            instances[image.trackableId] = spirit;
            Debug.Log("Season detected " + prefab.referenceName + " " + image.trackableId);
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
            !page.IsBackButton(pointer.position.ReadValue()) &&
            Physics.Raycast(arCamera.ScreenPointToRay(pointer.position.ReadValue()), out var hit) &&
            hit.collider.GetComponentInParent<SpringSpirit>() is SpringSpirit selected)
        {
            selected.Blossom();
            storyOwner = selected;
            page.SetStory(selected);
            storyUntil = Time.unscaledTime + 5;
            Debug.Log("Season tapped " + selected.referenceName);
        }
        if (!storyOwner || !storyOwner.gameObject.activeInHierarchy) storyUntil = 0;
        page.SetTracking(visible, Time.unscaledTime < storyUntil, ARSession.state == ARSessionState.SessionTracking);
    }
}
