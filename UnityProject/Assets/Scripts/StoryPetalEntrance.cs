using UnityEngine;
using UnityEngine.UI;

public sealed class StoryPetalEntrance : MonoBehaviour
{
    public const float Duration = .85f;
    CanvasGroup paper, words;
    Image[] petals;
    float elapsed;
    bool playing, petalsOnly;

    public void Bind(CanvasGroup paperGroup, CanvasGroup textGroup, Image[] petalImages)
    {
        paper = paperGroup;
        words = textGroup;
        petals = petalImages;
    }

    public void Open()
    {
        petalsOnly = false;
        Sample(0);
    }

    public void OnRetap()
    {
        if (playing) return;
        petalsOnly = true;
        Sample(0);
    }

    void Update()
    {
        if (playing) Sample(elapsed + Time.unscaledDeltaTime);
    }

    public void Sample(float seconds)
    {
        elapsed = Mathf.Clamp(seconds,0,Duration);
        playing = elapsed < Duration;
        paper.alpha = petalsOnly ? 1 : EaseBetween(.10f,.61f,elapsed);
        words.alpha = petalsOnly ? 1 : EaseBetween(.28f,.83f,elapsed);
        var paperRect = (RectTransform)paper.transform;
        paperRect.localScale = Vector3.one * Mathf.Lerp(.975f,1,paper.alpha);
        var textRect = (RectTransform)words.transform;
        textRect.anchoredPosition = Vector2.down * (3 * (1 - words.alpha));

        for (int i = 0; i < petals.Length; i++)
        {
            var petal = petals[i];
            float t = Mathf.InverseLerp((i % 4) * .035f,Duration - (i % 3) * .043f,elapsed);
            float opacity = EaseBetween(0,.2f,t) * (1 - EaseBetween(.67f,1,t));
            petal.enabled = opacity > 0;
            if (!petal.enabled) continue;

            // Two loose streams settle into the divider and the printed blossom sprig.
            bool right = i % 2 != 0;
            Vector2 start = new(right ? .63f + .095f * (i % 5) : .035f + .084f * (i % 5),
                                1.04f + .13f * (i % 4));
            Vector2 end = right ? new Vector2(.84f + .023f * (i % 4), .80f + .03f * (i % 3))
                                : new Vector2(.48f + .015f * (i % 4), .46f);
            Vector2 bend = new(start.x + (right ? .14f : -.08f),end.y + .15f * (i % 3));
            float u = t * t * (3 - 2 * t), v = 1-u;
            var rect = petal.rectTransform;
            Vector2 anchor = v*v*start + 2*v*u*bend + u*u*end;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.localRotation = Quaternion.Euler(0,0,i * 41 + (right ? 83 : -94) * u);
            rect.localScale = Vector3.one * Mathf.Lerp(1,.44f,u);
            petal.color = new Color(1,1,1,.94f * opacity);
        }
    }

    static float EaseBetween(float begin, float end, float value) =>
        Mathf.SmoothStep(0,1,Mathf.InverseLerp(begin,end,value));
}
