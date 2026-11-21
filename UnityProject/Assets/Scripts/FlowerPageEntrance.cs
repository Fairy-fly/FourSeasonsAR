using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class FlowerPageEntrance : MonoBehaviour
{
    const float Duration = .20f;
    CanvasGroup group;

    void Awake() => group = GetComponent<CanvasGroup>();

    void OnEnable()
    {
        if (Application.isPlaying) StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        float elapsed = 0;
        while (elapsed < Duration)
        {
            SetProgress(elapsed / Duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetProgress(1);
    }

    public void SetProgress(float fraction)
    {
        if (!group) group = GetComponent<CanvasGroup>();
        float remaining = 1 - Mathf.Clamp01(fraction);
        float ease = remaining * remaining * remaining;
        group.alpha = 1 - ease;
        transform.localScale = Vector3.one * (1 - .024f * ease);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        SetProgress(1);
    }
}
