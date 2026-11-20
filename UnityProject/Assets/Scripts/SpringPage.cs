using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class SpringPage : MonoBehaviour
{
    public SpringVisit visit;
    public Texture2D cover;
    Font font;
    GameObject backdrop;
    RectTransform safe;
    Text caption;
    Button action;
    Text actionText;
    bool scanning;
    Rect lastSafeArea;

    void Awake()
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK SC", "MiSans", "Droid Sans Fallback", "Arial" }, 32);
        var canvas = new GameObject("四时花旅", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = .5f;
        new GameObject("触摸输入", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform, false);
        var frame = (RectTransform)canvas.transform;
        backdrop = Panel(frame, "封面", Vector2.zero, Vector2.one).gameObject;
        var background = backdrop.AddComponent<Image>();
        background.color = new Color(.97f, .91f, .90f);
        var art = Panel((RectTransform)backdrop.transform, "原作", Vector2.zero, Vector2.one);
        art.gameObject.AddComponent<RawImage>().texture = cover;
        var fit = art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = (float)cover.width / cover.height;
        safe = Panel(frame, "安全区域", Vector2.zero, Vector2.one);
        var panel = Panel(safe, "说明", new Vector2(.045f, .045f), new Vector2(.955f, .21f));
        panel.gameObject.AddComponent<Image>().color = new Color(.18f, .09f, .14f, .91f);
        caption = Label(Panel(panel, "提示", new Vector2(.04f,.45f), new Vector2(.96f,.96f)), 28);
        var button = Panel(panel, "入口", new Vector2(.28f,.07f), new Vector2(.72f,.43f));
        var image = button.gameObject.AddComponent<Image>();
        image.color = new Color(.96f,.77f,.81f);
        action = button.gameObject.AddComponent<Button>();
        action.targetGraphic = image;
        actionText = Label(Panel(button, "文字", Vector2.zero, Vector2.one), 28);
        actionText.color = new Color(.23f,.12f,.18f);
        action.onClick.AddListener(() => { if (scanning) visit.Home(); else visit.Begin(); });
        ShowScanner(false);
    }

    static RectTransform Panel(RectTransform parent, string name, Vector2 min, Vector2 max)
    {
        var t = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        t.SetParent(parent, false);
        t.anchorMin = min; t.anchorMax = max;
        t.offsetMin = t.offsetMax = Vector2.zero;
        return t;
    }

    Text Label(RectTransform target, int size)
    {
        var text = target.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    void Update()
    {
        var area = Screen.safeArea;
        if (area == lastSafeArea || Screen.width == 0 || Screen.height == 0) return;
        lastSafeArea = area;
        var size = new Vector2(Screen.width, Screen.height);
        safe.anchorMin = area.min / size;
        safe.anchorMax = area.max / size;
    }

    public void Message(string text) { if (caption.text != text) caption.text = text; }
    public void ShowScanner(bool enabled)
    {
        scanning = enabled;
        backdrop.SetActive(!enabled);
        actionText.text = enabled ? "返回封面" : "开始扫描";
        Message(enabled ? "请对准春季测试卡" : "春知意 · 春季角色卡　2.5D AR 体验");
    }
}
