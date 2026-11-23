using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class FlowerButton : Button
{
    public EnamelFlare flare;
    Coroutine motion;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (!isActiveAndEnabled) return;
        if (state == SelectionState.Pressed && flare) flare.Play();
        float target = state == SelectionState.Pressed ? .927f : 1;
        if (motion != null) StopCoroutine(motion);
        if (instant || !Application.isPlaying)
        {
            transform.localScale = Vector3.one * target;
            return;
        }
        if (state == SelectionState.Pressed)
            transform.localScale = Vector3.one * Mathf.Min(transform.localScale.x,.965f);
        motion = StartCoroutine(ScaleTo(target));
    }

    IEnumerator ScaleTo(float target)
    {
        float start = transform.localScale.x;
        float duration = target < 1 ? .075f : .23f;
        for (float t = 0; t < 1; t += Time.unscaledDeltaTime / duration)
        {
            float rebound = target == 1 ? (1-start)*.48f*Mathf.Sin(Mathf.PI*t) : 0;
            transform.localScale = Vector3.one * (Mathf.Lerp(start,target,1 - (1-t)*(1-t)) + rebound);
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
