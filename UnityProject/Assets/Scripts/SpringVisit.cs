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
    SpringSpirit activeSpirit;
    public SpringPage page;
    public Camera arCamera;
    [Min(0)] public float limitedGrace = .35f;

    readonly Dictionary<TrackableId, ARTrackedImage> targets = new();
    readonly CardFocus focus = new();
    bool starting;
    string invitation;
    public WorldWalk Travel { get; private set; }
    public void Invite(string id)
    {
        if(!CanOpenAlbum || !Album.Contains(id) || id!=ReferenceName)return;
        invitation=id;
        page.WorldDisplay(true);
        page.WorldHint("正在开启摄像头");
        StartCoroutine(Enter());
    }
    public void LeaveWorld()
    {
        if(starting)return;
        if(Travel && Travel.Active)Travel.Close();
        invitation=null;
        Home();
        page.WorldReturn();
    }
    bool scanning;
    public bool CanOpenAlbum => !starting && !scanning && !(Travel && Travel.Active);
    public SeasonAlbum Album { get; set; } = new SeasonAlbum();
    float storyUntil;

    void Awake()
    {
        foreach (var character in characters) byReference.Add(character.referenceName, character);
    }

    void OnEnable() => images.trackablesChanged.AddListener(Changed);
    void OnDisable() => images.trackablesChanged.RemoveListener(Changed);

    public void Begin()
    {
        if(CanOpenAlbum){invitation=null;StartCoroutine(Enter());}
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
            if(invitation!=null)page.WorldHint("相机或 AR 服务暂不可用，可返回花册");
            yield break;
        }
        yield return ARSession.CheckAvailability();
        if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();
        if (ARSession.state != ARSessionState.Ready && ARSession.state != ARSessionState.SessionTracking &&
            ARSession.state != ARSessionState.SessionInitializing)
        {
            page.Message("AR 服务暂不可用，请检查 Google Play Services for AR 后重试。");
            starting = false;
            if(invitation!=null)page.WorldHint("相机或 AR 服务暂不可用，可返回花册");
            yield break;
        }
        scanning = invitation==null;
        images.enabled=scanning;
        starting = false;
        images.gameObject.SetActive(true);
        session.gameObject.SetActive(true);
        if(scanning)page.ShowScanner(true);
        else
        {
            if(!Travel)Travel=gameObject.AddComponent<WorldWalk>();
            Travel.Open(this,invitation);
        }
    }

    public void Home()
    {
        scanning = false;
        images.gameObject.SetActive(false);
        session.gameObject.SetActive(false);
        targets.Clear();
        focus.Clear();
        ClearPresentation();
        page.WorldDisplay(false);
        page.ShowScanner(false);
    }

    void ClearPresentation()
    {
        page.HideAnnouncement();
        storyUntil = 0;
        page.SetTracking(false, false, false);
        if (activeSpirit)
        {
            activeSpirit.gameObject.SetActive(false);
            Destroy(activeSpirit.gameObject);
        }
        activeSpirit = null;
    }

    void Changed(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        if (!scanning) return;
        foreach (var item in changes.removed)
        {
            targets.Remove(item.Key);
            focus.Remove(item.Key);
        }
        foreach (var image in changes.added) Observe(image);
        foreach (var image in changes.updated) Observe(image);
        focus.FinishChanges();

        if (focus.Current is not TrackableId id || !targets.TryGetValue(id, out var target) || !target)
        {
            if (activeSpirit || storyUntil != 0) ClearPresentation();
            return;
        }
        if (!activeSpirit || activeSpirit.transform.parent != target.transform ||
            activeSpirit.referenceName != target.referenceImage.name)
        {
            ClearPresentation();
            if (focus.State != TrackingState.Tracking) return;
            activeSpirit = Instantiate(byReference[target.referenceImage.name], target.transform);
            if(activeSpirit.referenceName == ReferenceName) activeSpirit.ShowCompanion(arCamera);
            Debug.Log("Season encountered " + activeSpirit.referenceName);
        }
        activeSpirit.Track(focus.State, limitedGrace);
    }

    void Observe(ARTrackedImage image)
    {
        if (!byReference.ContainsKey(image.referenceImage.name))
        {
            focus.Remove(image.trackableId);
            targets.Remove(image.trackableId);
            return;
        }
        targets[image.trackableId] = image;
        focus.Observe(image.trackableId, CardObservation.Read(session, image));
    }

    void Update()
    {
        if (!scanning) return;
        bool visible = activeSpirit && activeSpirit.gameObject.activeInHierarchy;
        var pointer = Pointer.current;
        if (visible && focus.State == TrackingState.Tracking && pointer != null && pointer.press.wasPressedThisFrame &&
            !page.IsBackButton(pointer.position.ReadValue()) &&
            Physics.Raycast(arCamera.ScreenPointToRay(pointer.position.ReadValue()), out var hit) &&
            hit.collider.GetComponentInParent<SpringSpirit>() == activeSpirit)
        {
            activeSpirit.Blossom();
            if(Album.Collect(activeSpirit.referenceName))
            {
                page.AnnounceCollection(activeSpirit);
                page.FlyToAlbum(activeSpirit);
                Debug.Log("Season collected by tap " + activeSpirit.referenceName);
            }
            Debug.Log("Season tapped " + activeSpirit.referenceName);
        }
        if (!visible) storyUntil = 0;
        page.SetTracking(visible,false,ARSession.state == ARSessionState.SessionTracking);
        page.FollowSpirit(activeSpirit,visible && focus.State==TrackingState.Tracking,arCamera);
    }
}
