$ErrorActionPreference = "Stop"

Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public enum AudioDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2
}

public enum AudioRole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2
}

[Flags]
public enum AudioDeviceState : uint
{
    ACTIVE = 0x00000001,
    DISABLED = 0x00000002,
    NOTPRESENT = 0x00000004,
    UNPLUGGED = 0x00000008,
    ALL = 0x0000000F
}

public enum AudioSTGM
{
    READ = 0x00000000
}

[StructLayout(LayoutKind.Sequential)]
public struct AudioPROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
}

[StructLayout(LayoutKind.Explicit)]
public struct AudioPROPVARIANT
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] public IntPtr pwszVal;
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
public class AudioMMDeviceEnumerator
{
}

[ComImport]
[Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
public class AudioPolicyConfigClient
{
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(
        AudioDataFlow dataFlow,
        AudioDeviceState stateMask,
        out IAudioMMDeviceCollection devices
    );

    [PreserveSig]
    int GetDefaultAudioEndpoint(
        AudioDataFlow dataFlow,
        AudioRole role,
        out IAudioMMDevice endpoint
    );

    [PreserveSig]
    int GetDevice(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        out IAudioMMDevice device
    );

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-C0D7C09CCB11")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IAudioMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioMMDevice
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
        AudioSTGM accessMode,
        out IAudioPropertyStore properties
    );

    [PreserveSig]
    int GetId(out IntPtr id);

    [PreserveSig]
    int GetState(out AudioDeviceState state);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioPropertyStore
{
    [PreserveSig]
    int GetCount(out uint propertyCount);

    [PreserveSig]
    int GetAt(uint propertyIndex, out AudioPROPERTYKEY key);

    [PreserveSig]
    int GetValue(ref AudioPROPERTYKEY key, out AudioPROPVARIANT value);

    [PreserveSig]
    int SetValue(ref AudioPROPERTYKEY key, ref AudioPROPVARIANT value);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioPolicyConfig
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
        ref AudioPROPERTYKEY key,
        out AudioPROPVARIANT value
    );

    [PreserveSig]
    int SetPropertyValue(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        ref AudioPROPERTYKEY key,
        ref AudioPROPVARIANT value
    );

    [PreserveSig]
    int SetDefaultEndpoint(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        AudioRole role
    );

    [PreserveSig]
    int SetEndpointVisibility(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
        bool visible
    );
}

public class AudioOutputDeviceData
{
    public string Id;
    public string Name;
}

public static class AudioOutputSwitcher
{
    private const ushort VT_LPWSTR = 31;

    private static readonly AudioPROPERTYKEY PKEY_Device_FriendlyName =
        new AudioPROPERTYKEY
        {
            fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            pid = 14
        };

    [DllImport("Ole32.dll")]
    private static extern int PropVariantClear(ref AudioPROPVARIANT pvar);

    public static int SwitchToNextOutputDevice()
    {
        IAudioMMDeviceEnumerator enumerator = null;
        IAudioMMDeviceCollection collection = null;
        IAudioMMDevice defaultDevice = null;
        IAudioPolicyConfig policyConfig = null;

        try
        {
            enumerator = (IAudioMMDeviceEnumerator)(new AudioMMDeviceEnumerator());

            int defaultResult = enumerator.GetDefaultAudioEndpoint(
                AudioDataFlow.eRender,
                AudioRole.eMultimedia,
                out defaultDevice
            );

            string currentDeviceId = "";

            if (defaultResult == 0 && defaultDevice != null)
            {
                currentDeviceId = GetDeviceId(defaultDevice);
            }

            int enumResult = enumerator.EnumAudioEndpoints(
                AudioDataFlow.eRender,
                AudioDeviceState.ACTIVE,
                out collection
            );

            if (enumResult != 0 || collection == null)
            {
                Console.Error.WriteLine("[AudioOutputPS] Failed to enumerate devices. HRESULT: " + ToHex(enumResult));
                return 1;
            }

            collection.GetCount(out uint count);

            List<AudioOutputDeviceData> devices = new List<AudioOutputDeviceData>();

            for (uint i = 0; i < count; i++)
            {
                IAudioMMDevice device = null;

                try
                {
                    int itemResult = collection.Item(i, out device);

                    if (itemResult != 0 || device == null)
                    {
                        continue;
                    }

                    string id = GetDeviceId(device);
                    string name = GetDeviceFriendlyName(device);

                    if (!string.IsNullOrEmpty(id))
                    {
                        devices.Add(
                            new AudioOutputDeviceData
                            {
                                Id = id,
                                Name = name
                            }
                        );
                    }
                }
                finally
                {
                    ReleaseComObject(device);
                }
            }

            if (devices.Count == 0)
            {
                Console.Error.WriteLine("[AudioOutputPS] No active playback device found.");
                return 1;
            }

            Console.WriteLine("[AudioOutputPS] Active playback devices:");

            int currentIndex = -1;

            for (int i = 0; i < devices.Count; i++)
            {
                bool isCurrent = devices[i].Id == currentDeviceId;

                if (isCurrent)
                {
                    currentIndex = i;
                }

                Console.WriteLine(
                    "[AudioOutputPS] "
                    + i
                    + ": "
                    + devices[i].Name
                    + (isCurrent ? " *Current" : "")
                );
            }

            int nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + 1) % devices.Count;

            AudioOutputDeviceData nextDevice = devices[nextIndex];

            policyConfig = (IAudioPolicyConfig)(new AudioPolicyConfigClient());

            int resultConsole = policyConfig.SetDefaultEndpoint(nextDevice.Id, AudioRole.eConsole);
            int resultMultimedia = policyConfig.SetDefaultEndpoint(nextDevice.Id, AudioRole.eMultimedia);
            int resultCommunications = policyConfig.SetDefaultEndpoint(nextDevice.Id, AudioRole.eCommunications);

            if (resultConsole != 0 || resultMultimedia != 0 || resultCommunications != 0)
            {
                Console.Error.WriteLine("[AudioOutputPS] Set default endpoint failed.");
                Console.Error.WriteLine("[AudioOutputPS] Console HRESULT: " + ToHex(resultConsole));
                Console.Error.WriteLine("[AudioOutputPS] Multimedia HRESULT: " + ToHex(resultMultimedia));
                Console.Error.WriteLine("[AudioOutputPS] Communications HRESULT: " + ToHex(resultCommunications));
                return 1;
            }

            Console.WriteLine("[AudioOutputPS] Switched to: " + nextDevice.Name);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                "[AudioOutputPS] ERROR: "
                + exception.GetType().Name
                + ": "
                + exception.Message
            );

            if (exception.InnerException != null)
            {
                Console.Error.WriteLine(
                    "[AudioOutputPS] INNER: "
                    + exception.InnerException.GetType().Name
                    + ": "
                    + exception.InnerException.Message
                );
            }

            return 1;
        }
        finally
        {
            ReleaseComObject(policyConfig);
            ReleaseComObject(defaultDevice);
            ReleaseComObject(collection);
            ReleaseComObject(enumerator);
        }
    }

    private static string GetDeviceId(IAudioMMDevice device)
    {
        IntPtr idPointer = IntPtr.Zero;

        try
        {
            int result = device.GetId(out idPointer);

            if (result != 0 || idPointer == IntPtr.Zero)
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

    private static string GetDeviceFriendlyName(IAudioMMDevice device)
    {
        IAudioPropertyStore propertyStore = null;

        try
        {
            int openResult = device.OpenPropertyStore(AudioSTGM.READ, out propertyStore);

            if (openResult != 0 || propertyStore == null)
            {
                return "(Unknown Device)";
            }

            AudioPROPERTYKEY key = PKEY_Device_FriendlyName;
            int valueResult = propertyStore.GetValue(ref key, out AudioPROPVARIANT value);

            if (valueResult != 0)
            {
                return "(Unknown Device)";
            }

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

    private static void ReleaseComObject(object comObject)
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
}
"@

$result = [AudioOutputSwitcher]::SwitchToNextOutputDevice()
exit $result
