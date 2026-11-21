using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class FlowerButton : Button
{
    Coroutine motion;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (!isActiveAndEnabled) return;
        float target = state == SelectionState.Pressed ? .955f : 1;
        if (motion != null) StopCoroutine(motion);
        if (instant || !Application.isPlaying)
        {
            transform.localScale = Vector3.one * target;
            return;
        }
        if (state == SelectionState.Pressed)
            transform.localScale = Vector3.one * Mathf.Min(transform.localScale.x,.979f);
        motion = StartCoroutine(ScaleTo(target));
    }

    IEnumerator ScaleTo(float target)
    {
        float start = transform.localScale.x;
        float duration = target < 1 ? .065f : .13f;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(start,target,1 - (1-t)*(1-t));
            yield return null;
        }
        transform.localScale = Vector3.one * target;
        motion = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllCoroutines();
        motion = null;
        transform.localScale = Vector3.one;
    }
}
