using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class SpringPage : MonoBehaviour
{
    public SpringVisit visit;
    public Texture2D cover, title, scanButton, storyCard;

    // Clockwise contours of the artwork in normalized, bottom-left coordinates.
    static readonly Vector2[] StoryOutline = {
        new(.005f,.79f), new(.035f,.82f), new(.052f,.90f), new(.69f,.90f),
        new(.70f,.94f), new(.75f,.93f), new(.81f,.90f), new(.82f,.97f),
        new(.85f,.99f), new(.88f,.97f), new(.92f,.99f), new(.94f,.96f),
        new(.95f,.89f), new(.97f,.88f), new(.972f,.82f), new(.99f,.80f),
        new(.99f,.13f), new(.96f,.095f), new(.942f,.016f), new(.053f,.016f),
        new(.036f,.084f), new(.005f,.13f)
    };
    static readonly Color Ink = new Color32(97,45,59,255);
    static readonly Color Glass = new Color32(53,40,38,192);
    Font font;
    GameObject coverLayer, home, scanner, story;
    RectTransform safe, back;
    CanvasGroup guides;
    Text homeStatus, hint;
    RawImage coverImage;
    Vector2Int lastScreen;
    Rect lastSafe;
    bool tracked;

    void Awake()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] {"Noto Serif CJK SC","Noto Sans CJK SC","MiSans","Droid Sans Fallback","Arial"},32);
        var canvas = new GameObject("四时花旅",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform,false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(480,1067);
        scaler.matchWidthOrHeight = 0;
        new GameObject("触摸输入", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform,false);

        var frame = (RectTransform)canvas.transform;
        coverLayer = Bounds(frame,"花房",0,0,1,1).gameObject;
        coverImage = coverLayer.AddComponent<RawImage>();
        coverImage.texture = cover;
        coverImage.raycastTarget = false;
        safe = Bounds(frame,"安全区域",0,0,1,1);
        var heading = Bounds(safe,"封面",0,0,1,1);
        home = heading.gameObject;
        var mark = Fit(heading,"四时花旅",.14f,.811f,.86f,.919f,title);
        mark.gameObject.AddComponent<RawImage>().texture = title;
        var divider = Bounds(heading,"题签",.24f,.798f,.76f,.801f);
        Line(divider,0,.40f,.44f,.65f,Ink);
        Line(divider,.56f,.40f,1,.65f,Ink);
        var flower = Box(divider,"花印",.5f,.5f,5,5);
        Paint(flower,Ink);
        flower.localRotation = Quaternion.Euler(0,0,45);
        Label(Bounds(heading,"副题",.06f,.760f,.94f,.796f),"春知意 · 2.5D AR 体验",20,Ink);
        var start = Fit(heading,"开始扫描",.22f,.078f,.78f,.158f,scanButton);
        var startSurface = Surface(start,Color.white,scanButton);
        MakeButton(start,startSurface,visit.Begin);
        homeStatus = Label(Bounds(heading,"启动提示",.05f,.002f,.95f,.075f),"",17,Ink);

        var scan = Bounds(safe,"扫描",0,0,1,1);
        scanner = scan.gameObject;
        back = Box(scan,"返回封面",0,1,52,52);
        back.anchoredPosition = new Vector2(52,-51);
        MakeButton(back,Surface(back,Glass),visit.Home);
        foreach (int direction in new[]{-1,1})
        {
            var stroke = Box(back,"箭头",.48f,.5f,16,2.6f);
            stroke.anchoredPosition = new Vector2(1,direction*5);
            stroke.localRotation = Quaternion.Euler(0,0,direction*45);
            Paint(stroke,Color.white);
        }
        var overlay = Bounds(scan,"取景提示",0,0,1,1);
        guides = overlay.gameObject.AddComponent<CanvasGroup>();
        guides.blocksRaycasts = false;
        var slot = Bounds(overlay,"取景区域",.12f,.22f,.88f,.78f);
        var frameBox = Bounds(slot,"取景框",0,0,1,1);
        Aspect(frameBox,1);
        foreach (int x in new[]{0,1})
            foreach (int y in new[]{0,1})
            {
                var h = Box(frameBox,"横角",x,y,30,3);
                h.pivot = new Vector2(x,y);
                Paint(h,Color.white);
                var v = Box(frameBox,"竖角",x,y,3,30);
                v.pivot = new Vector2(x,y);
                Paint(v,Color.white);
            }
        var chip = Bounds(overlay,"找卡提示",.17f,.135f,.83f,.185f);
        Surface(chip,Glass);
        hint = Label(Bounds(chip,"提示文字",.04f,0,.96f,1),"把整张春卡放入框内",19,Color.white);
        var slip = Fit(scan,"春知意 · 把第一缕春风捎给赶路的人",.075f,.069f,.925f,.235f,storyCard);
        Surface(slip,Color.white,storyCard,StoryOutline);
        story = slip.gameObject;
        ShowScanner(false);
        UpdateLayout();
    }

    public void Message(string message) => homeStatus.text = message;

    public void ShowScanner(bool scanning)
    {
        tracked = false;
        guides.alpha = 1;
        story.SetActive(false);
        homeStatus.text = "";
        hint.text = "把整张春卡放入框内";
        coverLayer.SetActive(!scanning);
        home.SetActive(!scanning);
        scanner.SetActive(scanning);
    }

    public void SetTracking(bool visible, bool showStory, bool sessionReady)
    {
        tracked = visible;
        bool open = visible && showStory;
        if (story.activeSelf != open) story.SetActive(open);
        string tip = sessionReady ? "把整张春卡放入框内" : "正在定位，请缓慢移动手机";
        if (!visible && hint.text != tip) hint.text = tip;
    }

    public bool IsBackButton(Vector2 point) => RectTransformUtility.RectangleContainsScreenPoint(back,point);

    void Update()
    {
        UpdateLayout();
        if (scanner.activeSelf)
            guides.alpha = Mathf.MoveTowards(guides.alpha, tracked ? 0 : 1, Time.unscaledDeltaTime * 6);
    }

    void UpdateLayout()
    {
        var screen = new Vector2Int(Screen.width,Screen.height);
        Rect area = Screen.safeArea;
        if (screen.x <= 0 || screen.y <= 0 || (lastScreen == screen && lastSafe == area)) return;
        lastScreen = screen;
        lastSafe = area;
        safe.anchorMin = new Vector2(area.xMin/screen.x,area.yMin/screen.y);
        safe.anchorMax = new Vector2(area.xMax/screen.x,area.yMax/screen.y);
        coverImage.uvRect = CoverUV((float)screen.x / screen.y, (float)cover.width / cover.height);
    }

    public static Rect CoverUV(float displayAspect, float imageAspect)
    {
        float w = Mathf.Min(1,displayAspect/imageAspect);
        float h = Mathf.Min(1,imageAspect/displayAspect);
        return new Rect((1-w)/2,(1-h)/2,w,h);
    }

    static RectTransform Bounds(RectTransform parent,string name,float left,float bottom,float right,float top)
    {
        var rt = (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
        rt.SetParent(parent,false);
        rt.anchorMin = new Vector2(left,bottom);
        rt.anchorMax = new Vector2(right,top);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static RectTransform Box(RectTransform parent,string name,float x,float y,float width,float height)
    {
        var rt = Bounds(parent,name,x,y,x,y);
        rt.sizeDelta = new Vector2(width,height);
        return rt;
    }

    static void Aspect(RectTransform rect,float ratio)
    {
        var fit = rect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = ratio;
    }

    static RectTransform Fit(RectTransform parent,string name,float x,float y,float r,float t,Texture texture)
    {
        var slot = Bounds(parent,name+"位置",x,y,r,t);
        var art = Bounds(slot,name,0,0,1,1);
        Aspect(art,(float)texture.width/texture.height);
        return art;
    }

    static void Paint(RectTransform rect,Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static void Line(RectTransform parent,float x,float y,float r,float t,Color color) =>
        Paint(Bounds(parent,"细线",x,y,r,t),color);

    static SpringSurface Surface(RectTransform rt,Color color,Texture texture=null,Vector2[] outline=null)
    {
        var graphic = rt.gameObject.AddComponent<SpringSurface>();
        graphic.color = color;
        graphic.texture = texture;
        graphic.outline = outline;
        graphic.SetAllDirty();
        graphic.raycastTarget = false;
        return graphic;
    }

    static void MakeButton(RectTransform rt,Graphic graphic,UnityEngine.Events.UnityAction click)
    {
        graphic.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        button.onClick.AddListener(click);
    }

    Text Label(RectTransform parent,string value,int size,Color color)
    {
        var label = parent.gameObject.AddComponent<Text>();
        label.font = font;
        label.color = color;
        label.fontSize = size;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 13;
        label.resizeTextMaxSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }
}
