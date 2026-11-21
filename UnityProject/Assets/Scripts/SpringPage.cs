using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class SpringPage : MonoBehaviour
{
    public SpringVisit visit;
    public Texture2D cover;
    public Font brushFont, readingFont;
    static readonly Color Plum = new Color32(103,47,63,255);
    static readonly Color Honey = new Color32(202,165,121,255);
    static readonly Color Parchment = new Color32(255,247,232,255);
    static readonly Color Ink = new Color32(97,45,59,255);
    static readonly Color Glass = new Color32(53,40,38,192);
    GameObject coverLayer, home, scanner, story;
    RectTransform safe, back;
    CanvasGroup guides;
    Text homeStatus, hint, storyTitle, storyText;
    RawImage coverImage;
    Vector2Int lastScreen;
    Rect lastSafe;
    bool tracked;

    void Awake()
    {
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
        Label(Bounds(heading,"书名",.08f,.80f,.92f,.929f),"四时花旅",82,Ink,brushFont);
        var divider = Bounds(heading,"题签",.26f,.788f,.74f,.808f);
        Ornament(divider);
        Label(Bounds(heading,"副题",.06f,.746f,.94f,.786f),"四季角色卡 · 2.5D AR 体验",20,Ink);

        var start = Fit(heading,"开始扫描",.22f,.078f,.78f,.158f,4.05f);
        var startSurface = Surface(start,Plum);
        Surface(Inset(start,"描金",2.6f), Honey);
        Surface(Inset(start,"漆面",3.6f), Plum);
        Label(Bounds(start,"按钮文字",.08f,.08f,.81f,.92f),"开始扫描",29,Parchment);
        Blossom(Box(start,"樱印",.855f,.5f,25,25));
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
        hint = Label(Bounds(chip,"提示文字",.04f,0,.96f,1),"把整张角色卡放入框内",19,Color.white);
        var slip = Fit(scan,"花笺",.065f,.065f,.935f,.236f,2.98f);
        Surface(slip,Honey,true);
        Surface(Inset(slip,"笺纸",1.5f),Parchment,true);
        Surface(Inset(slip,"内框",5),new Color32(224,197,164,255),true);
        Surface(Inset(slip,"内页",6),Parchment,true);
        Blossom(Bounds(slip,"折枝樱花",.71f,.59f,1.015f,1.04f),true);
        storyTitle = Label(Bounds(slip,"人名",.18f,.51f,.82f,.90f),"春知意",38,Ink,brushFont);
        Ornament(Bounds(slip,"花钿",.32f,.42f,.68f,.51f));
        storyText = Label(Bounds(slip,"角色介绍",.06f,.115f,.94f,.415f),"把第一缕春风捎给赶路的人。",22,Ink);
        story = slip.gameObject;
        ShowScanner(false);
        UpdateLayout();
    }

    public void SetStory(SpringSpirit character)
    {
        storyTitle.text = character.characterName;
        storyText.text = character.excerpt;
    }

    public void Message(string message) => homeStatus.text = message;

    public void ShowScanner(bool scanning)
    {
        tracked = false;
        guides.alpha = 1;
        story.SetActive(false);
        homeStatus.text = "";
        hint.text = "把整张角色卡放入框内";
        coverLayer.SetActive(!scanning);
        home.SetActive(!scanning);
        scanner.SetActive(scanning);
    }

    public void SetTracking(bool visible, bool showStory, bool sessionReady)
    {
        tracked = visible;
        bool open = visible && showStory;
        if (story.activeSelf != open) story.SetActive(open);
        string tip = sessionReady ? "把整张角色卡放入框内" : "正在定位，请缓慢移动手机";
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

    static RectTransform Fit(RectTransform parent,string name,float x,float y,float r,float t,float ratio)
    {
        var slot = Bounds(parent,name+"位置",x,y,r,t);
        var art = Bounds(slot,name,0,0,1,1);
        Aspect(art,ratio);
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

    static RectTransform Inset(RectTransform parent,string name,float margin)
    {
        var rect=Bounds(parent,name,0,0,1,1);
        rect.offsetMin = Vector2.one*margin;
        rect.offsetMax = -Vector2.one*margin;
        return rect;
    }

    static void Blossom(RectTransform rect,bool sprig=false)
    {
        var blossom=rect.gameObject.AddComponent<SpringBlossoms>();
        blossom.sprig=sprig;
        blossom.raycastTarget=false;
        blossom.SetVerticesDirty();
    }

    static void Ornament(RectTransform rect)
    {
        Line(rect,0,.47f,.43f,.55f,Honey);
        Line(rect,.57f,.47f,1,.55f,Honey);
        Blossom(Box(rect,"花钿",.5f,.5f,12,12));
    }

    static SpringSurface Surface(RectTransform rt,Color color,bool paper=false)
    {
        var graphic = rt.gameObject.AddComponent<SpringSurface>();
        graphic.color = color;
        graphic.paper = paper;
        graphic.raycastTarget = false;
        graphic.SetAllDirty();
        return graphic;
    }

    static void MakeButton(RectTransform rt,Graphic graphic,UnityEngine.Events.UnityAction click)
    {
        graphic.raycastTarget = true;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        button.onClick.AddListener(click);
    }

    Text Label(RectTransform parent,string value,int size,Color color,Font face=null)
    {
        var label = parent.gameObject.AddComponent<Text>();
        label.font = face ? face : readingFont;
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
