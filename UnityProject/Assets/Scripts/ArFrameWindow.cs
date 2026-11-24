using UnityEngine;

// Aggregates only frames with a tracked real-world character placed.
public sealed class ArFrameWindow
{
    const int WindowSeconds=30;
    const int MaximumMilliseconds=251;
    readonly int[] histogram=new int[MaximumMilliseconds];
    float seconds;
    int frames, over50ms;

    public void Sample(float delta)
    {
        if (delta<=0) return;
        seconds+=delta;
        frames++;
        int ms=Mathf.Clamp(Mathf.CeilToInt(1000*delta),0,histogram.Length-1);
        histogram[ms]++;
        if (delta>.05f) over50ms++;
        if (seconds>=WindowSeconds) Write();
    }

    public void Write()
    {
        if(frames==0) return;
        int rank=Mathf.CeilToInt(frames*.95f);
        int bucket=0, counted=histogram[0];
        while(counted<rank && bucket<histogram.Length-1) counted+=histogram[++bucket];
        Debug.Log(System.FormattableString.Invariant($"Flower AR frames seconds={seconds:F2} frames={frames} fps={frames/seconds:F2} p95_ms={bucket} over50ms={over50ms}"));
        System.Array.Clear(histogram,0,histogram.Length);
        seconds=0;
        frames=over50ms=0;
    }
}
