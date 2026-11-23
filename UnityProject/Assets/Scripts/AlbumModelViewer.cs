using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class AlbumModelViewer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IDragHandler, IInitializePotentialDragHandler, IScrollHandler
{
    const int GalleryLayer = 30;
    const int NoPointer = int.MinValue;
    Camera galleryCamera;
    GameObject stage;
    Transform figure;
    Material paint;
    RenderTexture frame;
    RawImage surface;
    Bounds bounds;
    float yaw, pitch, zoom;
    int first = NoPointer, second = NoPointer;
    Vector2 firstPoint, secondPoint;

    public const string TimeKeeper = "hidden_shijian", StarDreamer = "hidden_xingmian";
    public static bool HasModel(string id) => id == SpringVisit.ReferenceName || id == "summer_huajian" ||
        id == "autumn_manwu" || id == "winter_xueling" || id == TimeKeeper || id == StarDreamer;

    public void Open(string id)
    {
        string path = "Gallery/" + id + "/";
        var prefab = Resources.Load<GameObject>(path + "Figure");
        var shader = Resources.Load<Shader>("Gallery/FigureStudio");
        var color = Resources.Load<Texture2D>(path + "Color");
        var normal = Resources.Load<Texture2D>(path + "Normal");
        var surfaceMap = Resources.Load<Texture2D>(path + "Surface");
        if (!prefab || !shader || !color || !normal || !surfaceMap)
            throw new System.InvalidOperationException("花册模型资源不完整 " + id);

        ReleaseStage();
        surface = GetComponent<RawImage>();
        gameObject.SetActive(true);
        stage = new GameObject("花册模型摄影棚");
        stage.hideFlags = HideFlags.DontSave;
        figure = new GameObject(id).transform;
        figure.SetParent(stage.transform,false);
        var model = Instantiate(prefab,figure,false);
        var renderer = model.GetComponentInChildren<MeshRenderer>();
        renderer.gameObject.layer = GalleryLayer;
        paint = new Material(shader) { mainTexture = color };
        paint.SetTexture("_NormalMap",normal);
        paint.SetTexture("_Surface",surfaceMap);
        renderer.sharedMaterial = paint;
        bounds = renderer.bounds;
        stage.transform.position = new Vector3(7500,0,0);
        galleryCamera = new GameObject("模型相机",typeof(Camera)).GetComponent<Camera>();
        galleryCamera.transform.SetParent(stage.transform,false);
        galleryCamera.enabled = false;
        galleryCamera.orthographic = true;
        galleryCamera.clearFlags = CameraClearFlags.SolidColor;
        galleryCamera.backgroundColor = Color.clear;
        galleryCamera.cullingMask = 1 << GalleryLayer;
        galleryCamera.nearClipPlane = .1f;
        galleryCamera.farClipPlane = 12;
        galleryCamera.allowHDR = false;
        SetView(0,0,1);
    }

    public void SetView(float horizontal,float vertical,float magnification)
    {
        yaw = Mathf.Repeat(horizontal,360);
        pitch = Mathf.Clamp(vertical,-18,18);
        zoom = Mathf.Clamp(magnification,.85f,1.75f);
        Draw();
    }

    void OnRectTransformDimensionsChange()
    {
        if (galleryCamera && isActiveAndEnabled) Draw();
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused && galleryCamera && isActiveAndEnabled) Draw();
    }

    void Draw()
    {
        Rect rect = surface.rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        float aspect = rect.width / rect.height;
        int h = Mathf.Min(900, Mathf.RoundToInt(900/aspect));
        int w = Mathf.Max(1,Mathf.RoundToInt(h*aspect));
        if (!frame || frame.width!=w || frame.height!=h)
        {
            ReleaseFrame();
            frame = new RenderTexture(w,h,24,RenderTextureFormat.ARGB32) { name="花册模型展窗",filterMode=FilterMode.Bilinear };
            frame.Create();
            galleryCamera.targetTexture = frame;
            surface.texture = frame;
        }
        if (!frame.IsCreated()) frame.Create();
        figure.localRotation = Quaternion.Euler(pitch,yaw+180f,0);
        figure.localPosition = -(figure.localRotation*bounds.center);
        galleryCamera.transform.localPosition = new Vector3(0,(zoom-1)*bounds.extents.y*.4f,-5);
        galleryCamera.aspect = aspect;
        galleryCamera.orthographicSize = Mathf.Max(bounds.extents.y,bounds.extents.x/aspect)*1.07f/zoom;
        
        galleryCamera.Render();
    }

    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold=false;
    public void OnPointerDown(PointerEventData e)
    {
        if (first == NoPointer) { first=e.pointerId; firstPoint=e.position; }
        else if (second==NoPointer && e.pointerId!=first) { second=e.pointerId; secondPoint=e.position; }
    }
    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId==first) { first=second; firstPoint=secondPoint; second=NoPointer; }
        else if (e.pointerId==second) second=NoPointer;
    }
    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId!=first && e.pointerId!=second) return;
        float previous=Vector2.Distance(firstPoint,secondPoint);
        Vector2 delta = e.position-(e.pointerId==first ? firstPoint : secondPoint);
        if (e.pointerId==first) firstPoint=e.position;
        else secondPoint=e.position;
        if (second!=NoPointer)
        {
            if (previous>=2) SetView(yaw,pitch,zoom*Vector2.Distance(firstPoint,secondPoint)/previous);
        }
        else
        {
            Rect pixels=RectTransformUtility.PixelAdjustRect(surface.rectTransform,surface.canvas);
            float span=Mathf.Max(1,pixels.width*surface.canvas.scaleFactor);
            SetView(yaw-260*delta.x/span,pitch+110*delta.y/span,zoom);
        }
    }
    public void OnScroll(PointerEventData e) => SetView(yaw,pitch,zoom*Mathf.Pow(1.10f,e.scrollDelta.y));

    static void DisposeObject(Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
    void ReleaseFrame()
    {
        if (!frame) return;
        galleryCamera.targetTexture = null;
        surface.texture = null;
        frame.Release();
        DisposeObject(frame);
        frame=null;
    }
    void OnDisable()
    {
        ReleaseStage();
    }
    public void Close()
    {
        gameObject.SetActive(false);
        ReleaseStage();
    }
    void ReleaseStage()
    {
        first=second=NoPointer;
        if (!stage) return;
        ReleaseFrame();
        stage.SetActive(false);
        DisposeObject(stage);
        DisposeObject(paint);
        stage=null;
        galleryCamera=null;
    }
}
