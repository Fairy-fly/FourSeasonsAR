using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.ARCore;
#endif

public static class CardObservation
{
    // ARCore may still provide an estimated pose after the image leaves the camera.
    public static TrackingState FromMethod(int method) => method switch
    {
        1 => TrackingState.Tracking,
        2 => TrackingState.Limited,
        _ => TrackingState.None
    };

    public static TrackingState Read(ARSession session, ARTrackedImage image)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (image.trackingState == TrackingState.Tracking && session.subsystem is ARCoreSessionSubsystem core)
        {
            IntPtr handle = core.session.AsIntPtr();
            IntPtr wrapper = image.nativePtr;
            if (handle == IntPtr.Zero || wrapper == IntPtr.Zero || Marshal.ReadInt32(wrapper) != 1)
                return TrackingState.Limited;
            // UnityXRNativeImage_1 from the pinned plugin's UnityXRNativePtrs.h.
            var native = Marshal.PtrToStructure<NativeImage>(wrapper);
            if (native.imageTrackable == IntPtr.Zero) return TrackingState.Limited;
            ArAugmentedImage_getTrackingMethod(handle, native.imageTrackable, out int method);
            return FromMethod(method);
        }
#endif
        return image.trackingState;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    [StructLayout(LayoutKind.Sequential)]
    struct NativeImage
    {
        public int version;
        public IntPtr imageTrackable;
    }

    [DllImport("arcore_sdk_c", CallingConvention = CallingConvention.Cdecl)]
    static extern void ArAugmentedImage_getTrackingMethod(IntPtr session, IntPtr image, out int method);
#endif
}
