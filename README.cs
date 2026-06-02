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
    [SerializeField] private float unityAudioResetDelay = 0.3f;

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
            StartCoroutine(ResetUnityAudioAfterDelay());
        }
    }

    public void RefreshDeviceList()
    {
        devices.Clear();

        CoreAudioIMMDeviceEnumerator enumerator = null;
        CoreAudioIMMDeviceCollection collection = null;
        CoreAudioIMMDevice defaultDevice = null;

        try
        {
            enumerator = CreateDeviceEnumerator();

            int defaultResult = enumerator.GetDefaultAudioEndpoint(
                CoreAudioEDataFlow.eRender,
                CoreAudioERole.eMultimedia,
                out defaultDevice
            );

            if (defaultResult == 0 && defaultDevice != null)
            {
                currentDefaultDeviceId = GetDeviceId(defaultDevice);
            }
            else
            {
                currentDefaultDeviceId = "";
                Debug.LogWarning("[AudioOutput] Failed to get default endpoint. HRESULT: " + ToHex(defaultResult));
            }

            int enumResult = enumerator.EnumAudioEndpoints(
                CoreAudioEDataFlow.eRender,
                CoreAudioDeviceState.ACTIVE,
                out collection
            );

            if (enumResult != 0 || collection == null)
            {
                Debug.LogWarning("[AudioOutput] Failed to enumerate playback devices. HRESULT: " + ToHex(enumResult));
                return;
            }

            collection.GetCount(out uint count);

            for (uint i = 0; i < count; i++)
            {
                CoreAudioIMMDevice device = null;

                try
                {
                    int itemResult = collection.Item(i, out device);

                    if (itemResult != 0 || device == null)
                    {
                        Debug.LogWarning("[AudioOutput] Failed to get device item. Index: " + i + " HRESULT: " + ToHex(itemResult));
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
            Debug.LogError("[AudioOutput] Refresh failed: " + BuildExceptionMessage(exception));
        }
        finally
        {
            ReleaseComObject(defaultDevice);
            ReleaseComObject(collection);
            ReleaseComObject(enumerator);
        }
    }

    private CoreAudioIMMDeviceEnumerator CreateDeviceEnumerator()
    {
        object instance = null;

        try
        {
            instance = new CoreAudioMMDeviceEnumerator();

            CoreAudioIMMDeviceEnumerator enumerator = instance as CoreAudioIMMDeviceEnumerator;

            if (enumerator == null)
            {
                ReleaseComObject(instance);
                throw new InvalidCastException("[AudioOutput] Failed to cast MMDeviceEnumerator to CoreAudioIMMDeviceEnumerator.");
            }

            return enumerator;
        }
        catch (Exception exception)
        {
            ReleaseComObject(instance);
            throw new InvalidOperationException("[AudioOutput] Failed to create MMDeviceEnumerator.", exception);
        }
    }

    private CoreAudioIPolicyConfig CreatePolicyConfig()
    {
        object instance = null;

        try
        {
            instance = new CoreAudioPolicyConfigClient();

            CoreAudioIPolicyConfig policyConfig = instance as CoreAudioIPolicyConfig;

            if (policyConfig == null)
            {
                ReleaseComObject(instance);
                throw new InvalidCastException("[AudioOutput] Failed to cast PolicyConfigClient to CoreAudioIPolicyConfig.");
            }

            return policyConfig;
        }
        catch (Exception exception)
        {
            ReleaseComObject(instance);
            throw new InvalidOperationException("[AudioOutput] Failed to create PolicyConfigClient.", exception);
        }
    }

    private bool SetDefaultPlaybackDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return false;
        }

        CoreAudioIPolicyConfig policyConfig = null;

        try
        {
            policyConfig = CreatePolicyConfig();

            int consoleResult = policyConfig.SetDefaultEndpoint(deviceId, CoreAudioERole.eConsole);
            int multimediaResult = policyConfig.SetDefaultEndpoint(deviceId, CoreAudioERole.eMultimedia);
            int communicationsResult = policyConfig.SetDefaultEndpoint(deviceId, CoreAudioERole.eCommunications);

            if (consoleResult != 0)
            {
                Debug.LogWarning("[AudioOutput] Set Console endpoint failed. HRESULT: " + ToHex(consoleResult));
            }

            if (multimediaResult != 0)
            {
                Debug.LogWarning("[AudioOutput] Set Multimedia endpoint failed. HRESULT: " + ToHex(multimediaResult));
            }

            if (communicationsResult != 0)
            {
                Debug.LogWarning("[AudioOutput] Set Communications endpoint failed. HRESULT: " + ToHex(communicationsResult));
            }

            return consoleResult == 0
                && multimediaResult == 0
                && communicationsResult == 0;
        }
        catch (Exception exception)
        {
            Debug.LogError("[AudioOutput] Set default device failed: " + BuildExceptionMessage(exception));
            return false;
        }
        finally
        {
            ReleaseComObject(policyConfig);
        }
    }

    private IEnumerator ResetUnityAudioAfterDelay()
    {
        yield return new WaitForSeconds(unityAudioResetDelay);

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        bool result = AudioSettings.Reset(configuration);

        Debug.Log("[AudioOutput] Unity audio reset: " + result);
    }

    private string GetDeviceId(CoreAudioIMMDevice device)
    {
        IntPtr idPointer = IntPtr.Zero;

        try
        {
            int result = device.GetId(out idPointer);

            if (result != 0 || idPointer == IntPtr.Zero)
            {
                Debug.LogWarning("[AudioOutput] Failed to get device id. HRESULT: " + ToHex(result));
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

    private string GetDeviceFriendlyName(CoreAudioIMMDevice device)
    {
        CoreAudioIPropertyStore propertyStore = null;

        try
        {
            int openResult = device.OpenPropertyStore(CoreAudioSTGM.READ, out propertyStore);

            if (openResult != 0 || propertyStore == null)
            {
                Debug.LogWarning("[AudioOutput] Failed to open property store. HRESULT: " + ToHex(openResult));
                return "(Unknown Device)";
            }

            CoreAudioPROPERTYKEY key = CoreAudioPropertyKeys.PKEY_Device_FriendlyName;
            int getValueResult = propertyStore.GetValue(ref key, out CoreAudioPROPVARIANT value);

            if (getValueResult != 0)
            {
                Debug.LogWarning("[AudioOutput] Failed to get device friendly name. HRESULT: " + ToHex(getValueResult));
                return "(Unknown Device)";
            }

            try
            {
                if (value.vt == CoreAudioConstants.VT_LPWSTR && value.pwszVal != IntPtr.Zero)
                {
                    return Marshal.PtrToStringUni(value.pwszVal);
                }

                return "(Unknown Device)";
            }
            finally
            {
                CoreAudioNativeMethods.PropVariantClear(ref value);
            }
        }
        finally
        {
            ReleaseComObject(propertyStore);
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

    private string BuildExceptionMessage(Exception exception)
    {
        if (exception == null)
        {
            return "";
        }

        string message = exception.GetType().Name + ": " + exception.Message;

        if (exception.InnerException != null)
        {
            message += " | Inner: "
                + exception.InnerException.GetType().Name
                + ": "
                + exception.InnerException.Message;
        }

        if (exception is COMException comException)
        {
            message += " | HRESULT: " + ToHex(comException.ErrorCode);
        }

        if (exception.InnerException is COMException innerComException)
        {
            message += " | Inner HRESULT: " + ToHex(innerComException.ErrorCode);
        }

        return message;
    }

    private static string ToHex(int value)
    {
        return "0x" + unchecked((uint)value).ToString("X8");
    }
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class CoreAudioMMDeviceEnumerator
{
}

[ComImport]
[Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
internal class CoreAudioPolicyConfigClient
{
}

internal enum CoreAudioEDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2
}

internal enum CoreAudioERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2
}

[Flags]
internal enum CoreAudioDeviceState : uint
{
    ACTIVE = 0x00000001,
    DISABLED = 0x00000002,
    NOTPRESENT = 0x00000004,
    UNPLUGGED = 0x00000008,
    ALL = 0x0000000F
}

internal enum CoreAudioSTGM
{
    READ = 0x00000000
}

internal static class CoreAudioConstants
{
    public const ushort VT_LPWSTR = 31;
}

internal static class CoreAudioNativeMethods
{
    [DllImport("Ole32.dll")]
    public static extern int PropVariantClear(ref CoreAudioPROPVARIANT pvar);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CoreAudioPROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
}

[StructLayout(LayoutKind.Explicit)]
internal struct CoreAudioPROPVARIANT
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] public IntPtr pwszVal;
}

internal static class CoreAudioPropertyKeys
{
    public static CoreAudioPROPERTYKEY PKEY_Device_FriendlyName = new CoreAudioPROPERTYKEY
    {
        fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        pid = 14
    };
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface CoreAudioIMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(
        CoreAudioEDataFlow dataFlow,
        CoreAudioDeviceState stateMask,
        out CoreAudioIMMDeviceCollection devices
    );

    [PreserveSig]
    int GetDefaultAudioEndpoint(
        CoreAudioEDataFlow dataFlow,
        CoreAudioERole role,
        out CoreAudioIMMDevice endpoint
    );

    [PreserveSig]
    int GetDevice(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        out CoreAudioIMMDevice device
    );

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-C0D7C09CCB11")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface CoreAudioIMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out CoreAudioIMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface CoreAudioIMMDevice
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
        CoreAudioSTGM accessMode,
        out CoreAudioIPropertyStore properties
    );

    [PreserveSig]
    int GetId(out IntPtr id);

    [PreserveSig]
    int GetState(out CoreAudioDeviceState state);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface CoreAudioIPropertyStore
{
    [PreserveSig]
    int GetCount(out uint propertyCount);

    [PreserveSig]
    int GetAt(uint propertyIndex, out CoreAudioPROPERTYKEY key);

    [PreserveSig]
    int GetValue(ref CoreAudioPROPERTYKEY key, out CoreAudioPROPVARIANT value);

    [PreserveSig]
    int SetValue(ref CoreAudioPROPERTYKEY key, ref CoreAudioPROPVARIANT value);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface CoreAudioIPolicyConfig
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
        ref CoreAudioPROPERTYKEY key,
        out CoreAudioPROPVARIANT value
    );

    [PreserveSig]
    int SetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        ref CoreAudioPROPERTYKEY key,
        ref CoreAudioPROPVARIANT value
    );

    [PreserveSig]
    int SetDefaultEndpoint(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        CoreAudioERole role
    );

    [PreserveSig]
    int SetEndpointVisibility(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        bool visible
    );
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
