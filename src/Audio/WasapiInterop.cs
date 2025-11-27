namespace Pulsar.Audio;

using System.Runtime.InteropServices;

/// <summary>
/// Direct P/Invoke bindings for Windows Core Audio (WASAPI) to avoid NAudio COM interop issues.
/// </summary>
internal static class WasapiInterop
{
    // COM GUIDs
    public static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    public static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    public static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    public static readonly Guid IID_IAudioCaptureClient = new("C8ADBD64-E71E-48a0-A4DE-185C395CD317");

    // Enums
    public enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
    public enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

    [Flags]
    public enum AUDCLNT_STREAMFLAGS : UInt32
    {
        AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000,
        AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000,
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM = 0x80000000,
    }

    public enum AUDCLNT_SHAREMODE { AUDCLNT_SHAREMODE_SHARED = 0, AUDCLNT_SHAREMODE_EXCLUSIVE = 1 }

    // WAVEFORMATEX structure
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAVEFORMATEX
    {
        public UInt16 wFormatTag;
        public UInt16 nChannels;
        public UInt32 nSamplesPerSec;
        public UInt32 nAvgBytesPerSec;
        public UInt16 nBlockAlign;
        public UInt16 wBitsPerSample;
        public UInt16 cbSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAVEFORMATEXTENSIBLE
    {
        public WAVEFORMATEX Format;
        public UInt16 wValidBitsPerSample;
        public UInt32 dwChannelMask;
        public Guid SubFormat;
    }

    public static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00aa00389b71");
    public static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00aa00389b71");

    // COM Interfaces
    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        Int32 EnumAudioEndpoints(EDataFlow dataFlow, UInt32 dwStateMask, out IntPtr ppDevices);
        Int32 GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
        Int32 GetDevice(String pwstrId, out IMMDevice ppDevice);
        Int32 RegisterEndpointNotificationCallback(IntPtr pClient);
        Int32 UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        Int32 Activate(ref Guid iid, UInt32 dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        Int32 OpenPropertyStore(UInt32 stgmAccess, out IntPtr ppProperties);
        Int32 GetId(out IntPtr ppstrId);
        Int32 GetState(out UInt32 pdwState);
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioClient
    {
        Int32 Initialize(AUDCLNT_SHAREMODE ShareMode, UInt32 StreamFlags, Int64 hnsBufferDuration, Int64 hnsPeriodicity, IntPtr pFormat, IntPtr AudioSessionGuid);
        Int32 GetBufferSize(out UInt32 pNumBufferFrames);
        Int32 GetStreamLatency(out Int64 phnsLatency);
        Int32 GetCurrentPadding(out UInt32 pNumPaddingFrames);
        Int32 IsFormatSupported(AUDCLNT_SHAREMODE ShareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        Int32 GetMixFormat(out IntPtr ppDeviceFormat);
        Int32 GetDevicePeriod(out Int64 phnsDefaultDevicePeriod, out Int64 phnsMinimumDevicePeriod);
        Int32 Start();
        Int32 Stop();
        Int32 Reset();
        Int32 SetEventHandle(IntPtr eventHandle);
        Int32 GetService(ref Guid riid, out IntPtr ppv);
    }

    [ComImport]
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioCaptureClient
    {
        Int32 GetBuffer(out IntPtr ppData, out UInt32 pNumFramesToRead, out UInt32 pdwFlags, out UInt64 pu64DevicePosition, out UInt64 pu64QPCPosition);
        Int32 ReleaseBuffer(UInt32 NumFramesRead);
        Int32 GetNextPacketSize(out UInt32 pNumFramesInNextPacket);
    }

    // P/Invoke
    [DllImport("ole32.dll")]
    public static extern Int32 CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, UInt32 dwClsContext, ref Guid riid, out IntPtr ppv);

    [DllImport("ole32.dll")]
    public static extern Int32 CoInitializeEx(IntPtr pvReserved, UInt32 dwCoInit);

    [DllImport("ole32.dll")]
    public static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    public static extern void CoTaskMemFree(IntPtr pv);

    public const UInt32 CLSCTX_ALL = 0x17;
    public const UInt32 COINIT_MULTITHREADED = 0x0;
    public const UInt32 COINIT_APARTMENTTHREADED = 0x2;

    public const UInt32 AUDCLNT_BUFFERFLAGS_SILENT = 0x2;
}