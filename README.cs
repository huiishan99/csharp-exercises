using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

public class DemoWindowsAudioOutputSwitcher : MonoBehaviour
{
    [Serializable]
    private class AudioOutputDeviceInfo
    {
        public string id;
        public string name;
    }

    [SerializeField] private bool resetUnityAudioAfterSwitch = true;
    [SerializeField] private float unityAudioResetDelay = 0.2f;

    private readonly List<AudioOutputDeviceInfo> devices = new List<AudioOutputDeviceInfo>();
    private string currentDefaultDeviceId = "";

    private void Start()
    {
        RefreshDeviceList();
        LogDeviceList();
    }

    public void SwitchToNextDevice()
    {
        RefreshDeviceList();

        if (devices.Count == 0)
        {
            Debug.LogWarning("[AudioOutput] No active playback device found.");
            return;
        }

        int currentIndex = FindDeviceIndex(currentDefaultDeviceId);
        int nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + 1) % devices.Count;

        AudioOutputDeviceInfo nextDevice = devices[nextIndex];

        bool success = SetDefaultPlaybackDevice(nextDevice.id);

        if (!success)
        {
            Debug.LogWarning("[AudioOutput] Failed to switch device: " + nextDevice.name);
            return;
        }

        currentDefaultDeviceId = nextDevice.id;

        Debug.Log("[AudioOutput] Switched to: " + nextDevice.name);

        if (resetUnityAudioAfterSwitch)
        {
            StartCoroutine(ResetUnityAudioNextFrame());
        }
    }

    public void RefreshDeviceList()
    {
        devices.Clear();

        IMMDeviceEnumerator enumerator = null;
        IMMDeviceCollection collection = null;
        IMMDevice defaultDevice = null;

        try
        {
            // UnityではCOM classの直接castが失敗する場合があるため、
            // CoCreateInstanceでInterfaceを直接生成する。
            enumerator = CreateComInstance<IMMDeviceEnumerator>(CLSID_MMDeviceEnumerator);

            int defaultResult = enumerator.GetDefaultAudioEndpoint(
                EDataFlow.eRender,
                ERole.eMultimedia,
                out defaultDevice
            );

            currentDefaultDeviceId = defaultResult == 0 && defaultDevice != null
                ? GetDeviceId(defaultDevice)
                : "";

            int result = enumerator.EnumAudioEndpoints(
                EDataFlow.eRender,
                DEVICE_STATE.ACTIVE,
                out collection
            );

            if (result != 0 || collection == null)
            {
                Debug.LogWarning("[AudioOutput] Failed to enumerate playback devices. HRESULT: " + ToHex(result));
                return;
            }

            collection.GetCount(out uint count);

            for (uint i = 0; i < count; i++)
            {
                IMMDevice device = null;

                try
                {
                    collection.Item(i, out device);

                    if (device == null)
                    {
                        continue;
                    }

                    string id = GetDeviceId(device);
                    string name = GetDeviceFriendlyName(device);

                    devices.Add(
                        new AudioOutputDeviceInfo
                        {
                            id = id,
                            name = name
                        }
                    );
                }
                finally
                {
                    ReleaseComObject(device);
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[AudioOutput] Refresh failed: " + exception.Message);
        }
        finally
        {
            ReleaseComObject(defaultDevice);
            ReleaseComObject(collection);
            ReleaseComObject(enumerator);
        }
    }

    private void LogDeviceList()
    {
        if (devices.Count == 0)
        {
            Debug.Log("[AudioOutput] Device list is empty.");
            return;
        }

        Debug.Log("[AudioOutput] Active playback devices:");

        for (int i = 0; i < devices.Count; i++)
        {
            string mark = devices[i].id == currentDefaultDeviceId
                ? " *Current"
                : "";

            Debug.Log("[AudioOutput] " + i + ": " + devices[i].name + mark);
        }
    }

    private int FindDeviceIndex(string deviceId)
    {
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].id == deviceId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool SetDefaultPlaybackDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return false;
        }

        IPolicyConfig policyConfig = null;

        try
        {
            // 直接newしてcastするとUnityで失敗することがあるため、
            // CoCreateInstanceでPolicyConfigを生成する。
            policyConfig = CreateComInstance<IPolicyConfig>(CLSID_PolicyConfigClient);

            int resultConsole = policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            int resultMultimedia = policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            int resultCommunications = policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);

            if (resultConsole != 0)
            {
                Debug.LogWarning("[AudioOutput] Set console endpoint failed: " + ToHex(resultConsole));
            }

            if (resultMultimedia != 0)
            {
                Debug.LogWarning("[AudioOutput] Set multimedia endpoint failed: " + ToHex(resultMultimedia));
            }

            if (resultCommunications != 0)
            {
                Debug.LogWarning("[AudioOutput] Set communications endpoint failed: " + ToHex(resultCommunications));
            }

            return resultConsole == 0
                && resultMultimedia == 0
                && resultCommunications == 0;
        }
        catch (Exception exception)
        {
            Debug.LogError("[AudioOutput] Set default device failed: " + exception.Message);
            return false;
        }
        finally
        {
            ReleaseComObject(policyConfig);
        }
    }

    private IEnumerator ResetUnityAudioNextFrame()
    {
        yield return new WaitForSeconds(unityAudioResetDelay);

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        bool result = AudioSettings.Reset(configuration);

        Debug.Log("[AudioOutput] Unity audio reset: " + result);
    }

    private T CreateComInstance<T>(Guid classId) where T : class
    {
        Guid interfaceId = typeof(T).GUID;
        IntPtr instancePointer = IntPtr.Zero;

        int result = CoCreateInstance(
            ref classId,
            IntPtr.Zero,
            CLSCTX_ALL,
            ref interfaceId,
            out instancePointer
        );

        if (result != 0 || instancePointer == IntPtr.Zero)
        {
            throw new COMException(
                "[AudioOutput] CoCreateInstance failed: " + typeof(T).Name + " HRESULT: " + ToHex(result),
                result
            );
        }

        try
        {
            object comObject = Marshal.GetTypedObjectForIUnknown(
                instancePointer,
                typeof(T)
            );

            return comObject as T;
        }
        finally
        {
            Marshal.Release(instancePointer);
        }
    }

    private string GetDeviceId(IMMDevice device)
    {
        IntPtr idPointer = IntPtr.Zero;

        try
        {
            device.GetId(out idPointer);

            if (idPointer == IntPtr.Zero)
            {
                return "";
            }

            return Marshal.PtrToStringUni(idPointer);
        }
        finally
        {
            if (idPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(idPointer);
            }
        }
    }

    private string GetDeviceFriendlyName(IMMDevice device)
    {
        IPropertyStore propertyStore = null;

        try
        {
            int openResult = device.OpenPropertyStore(STGM.READ, out propertyStore);

            if (openResult != 0 || propertyStore == null)
            {
                return "(Unknown Device)";
            }

            PROPERTYKEY key = PropertyKeys.PKEY_Device_FriendlyName;
            propertyStore.GetValue(ref key, out PROPVARIANT value);

            try
            {
                if (value.vt == VT_LPWSTR && value.pwszVal != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(value.pwszVal);
                }

                return "(Unknown Device)";
            }
            finally
            {
                PropVariantClear(ref value);
            }
        }
        finally
        {
            ReleaseComObject(propertyStore);
        }
    }

    private void ReleaseComObject(object comObject)
    {
        if (comObject == null)
        {
            return;
        }

        if (Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    private static string ToHex(int value)
    {
        return "0x" + unchecked((uint)value).ToString("X8");
    }

    private static readonly Guid CLSID_MMDeviceEnumerator =
        new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");

    private static readonly Guid CLSID_PolicyConfigClient =
        new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");

    private const uint CLSCTX_INPROC_SERVER = 0x1;
    private const uint CLSCTX_INPROC_HANDLER = 0x2;
    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint CLSCTX_REMOTE_SERVER = 0x10;

    private const uint CLSCTX_ALL =
        CLSCTX_INPROC_SERVER
        | CLSCTX_INPROC_HANDLER
        | CLSCTX_LOCAL_SERVER
        | CLSCTX_REMOTE_SERVER;

    private const ushort VT_LPWSTR = 31;

    [DllImport("Ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid clsid,
        IntPtr outer,
        uint context,
        ref Guid iid,
        out IntPtr instance
    );

    [DllImport("Ole32.dll")]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);

    private enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [Flags]
    private enum DEVICE_STATE : uint
    {
        ACTIVE = 0x00000001,
        DISABLED = 0x00000002,
        NOTPRESENT = 0x00000004,
        UNPLUGGED = 0x00000008,
        ALL = 0x0000000F
    }

    private enum STGM
    {
        READ = 0x00000000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PROPVARIANT
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr pwszVal;
    }

    private static class PropertyKeys
    {
        public static PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            pid = 14
        };
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(
            EDataFlow dataFlow,
            DEVICE_STATE stateMask,
            out IMMDevice devices
        );

        [PreserveSig]
        int GetDefaultAudioEndpoint(
            EDataFlow dataFlow,
            ERole role,
            out IMMDevice endpoint
        );

        [PreserveSig]
        int GetDevice(
            [MarshalAs(UnmanagedType.LPWStr)] string id,
            out IMMDevice device
        );

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr client);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-C0D7C09CCB11")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint count);

        [PreserveSig]
        int Item(uint index, out IMMDevice device);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(
            ref Guid interfaceId,
            uint classContext,
            IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer
        );

        [PreserveSig]
        int OpenPropertyStore(
            STGM accessMode,
            out IPropertyStore properties
        );

        [PreserveSig]
        int GetId(out IntPtr id);

        [PreserveSig]
        int GetState(out DEVICE_STATE state);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out uint propertyCount);

        [PreserveSig]
        int GetAt(uint propertyIndex, out PROPERTYKEY key);

        [PreserveSig]
        int GetValue(ref PROPERTYKEY key, out PROPVARIANT value);

        [PreserveSig]
        int SetValue(ref PROPERTYKEY key, ref PROPVARIANT value);

        [PreserveSig]
        int Commit();
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr mixFormat
        );

        [PreserveSig]
        int GetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            bool defaultFormat,
            IntPtr deviceFormat
        );

        [PreserveSig]
        int ResetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName
        );

        [PreserveSig]
        int SetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr endpointFormat,
            IntPtr mixFormat
        );

        [PreserveSig]
        int GetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            bool defaultPeriod,
            out long defaultPeriodValue,
            out long minimumPeriodValue
        );

        [PreserveSig]
        int SetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ref long period
        );

        [PreserveSig]
        int GetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr mode
        );

        [PreserveSig]
        int SetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr mode
        );

        [PreserveSig]
        int GetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ref PROPERTYKEY key,
            out PROPVARIANT value
        );

        [PreserveSig]
        int SetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ref PROPERTYKEY key,
            ref PROPVARIANT value
        );

        [PreserveSig]
        int SetDefaultEndpoint(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ERole role
        );

        [PreserveSig]
        int SetEndpointVisibility(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            bool visible
        );
    }
}

#else

public class DemoWindowsAudioOutputSwitcher : MonoBehaviour
{
    public void SwitchToNextDevice()
    {
        Debug.LogWarning("[AudioOutput] Windows audio output switching is only supported on Windows.");
    }
}

#endif
