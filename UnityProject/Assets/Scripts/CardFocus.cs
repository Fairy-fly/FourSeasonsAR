using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;

// A rediscovered card may take focus. Repeated updates for an older card cannot reclaim it.
public sealed class CardFocus
{
    readonly HashSet<TrackableId> directlyTracked = new();
    TrackableId? challenger;
    public TrackableId? Current { get; private set; }
    public TrackingState State { get; private set; }

    public void Observe(TrackableId id, TrackingState state)
    {
        if (Current == id) State = state;
        if (state != TrackingState.Tracking)
        {
            directlyTracked.Remove(id);
            if (challenger == id) challenger = null;
            return;
        }
        if (!directlyTracked.Add(id) || Current == id) return;
        if (!challenger.HasValue || ComesFirst(id, challenger.Value)) challenger = id;
    }

    static bool ComesFirst(TrackableId left, TrackableId right)
    {
        return left.subId1 < right.subId1 ||
            (left.subId1 == right.subId1 && left.subId2 < right.subId2);
    }

    public void Remove(TrackableId id)
    {
        Observe(id, TrackingState.None);
        if (Current == id) Current = null;
    }

    public void FinishChanges()
    {
        if (challenger.HasValue)
        {
            Current = challenger;
            State = TrackingState.Tracking;
        }
        else if (State == TrackingState.None) Current = null;
        challenger = null;
    }

    public void Clear()
    {
        Current = challenger = null;
        State = TrackingState.None;
        directlyTracked.Clear();
    }
}
