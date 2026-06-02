[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
private interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(
        EDataFlow dataFlow,
        DEVICE_STATE stateMask,
        out IMMDeviceCollection devices
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
