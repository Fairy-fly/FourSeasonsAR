using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class FlowerPageEntrance : MonoBehaviour
{
    const float Duration = .46f;
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
        float t = Mathf.Clamp01(fraction);
        float remaining = 1 - t;
        float ease = remaining * remaining * remaining;
        group.alpha = 1 - ease;
        float rebound = Mathf.Sin(Mathf.PI*t) * remaining;
        transform.localScale = Vector3.one * (1 - .093f*ease + .036f*rebound);
        transform.localRotation = Quaternion.Euler(0,0,-1.2f*ease);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        SetProgress(1);
    }
}
