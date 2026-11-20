using UnityEngine.XR.ARSubsystems;

// 状态由 AR 提供者更新；时钟由调用方传入，可离线验证连续丢失和重识别。
public struct TrackingVisibility
{
    bool acquired;
    float deadline;

    public void Update(TrackingState state, float now, float grace)
    {
        if (state == TrackingState.Tracking)
        {
            acquired = true;
            deadline = float.PositiveInfinity;
        }
        else if (state == TrackingState.None) acquired = false;
        else if (float.IsPositiveInfinity(deadline)) deadline = now + grace;
    }

    public bool IsVisible(float now) => acquired && now < deadline;
}
