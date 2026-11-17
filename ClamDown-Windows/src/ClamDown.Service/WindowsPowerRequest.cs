using System.ComponentModel;
using System.Runtime.InteropServices;
using ClamDown.Core.Power;

namespace ClamDown.Service;

/// <summary>
/// Requests that Windows keep the system awake while an active session runs.
/// This is not a lid-close override and does not prevent thermal shutdown.
/// </summary>
public sealed class WindowsPowerRequest : IPowerRequest
{
    private IntPtr _handle;
    private bool _active;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ReasonContext
    {
        public uint Version;
        public uint Flags;
        public IntPtr SimpleReasonString;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr PowerCreateRequest(ref ReasonContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(IntPtr handle, int requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerClearRequest(IntPtr handle, int requestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    private const int PowerRequestSystemRequired = 0;

    public void KeepSystemAwake(string reason)
    {
        if (_active) return;

        var reasonPtr = Marshal.StringToHGlobalUni(reason);
        try
        {
            var context = new ReasonContext
            {
                Version = 0,
                Flags = 1, // POWER_REQUEST_CONTEXT_SIMPLE_STRING
                SimpleReasonString = reasonPtr
            };

            _handle = PowerCreateRequest(ref context);
            if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "PowerCreateRequest failed.");

            if (!PowerSetRequest(_handle, PowerRequestSystemRequired))
            {
                var error = Marshal.GetLastWin32Error();
                CloseHandle(_handle);
                _handle = IntPtr.Zero;
                throw new Win32Exception(error, "PowerSetRequest failed.");
            }

            _active = true;
        }
        finally
        {
            Marshal.FreeHGlobal(reasonPtr);
        }
    }

    public void Release()
    {
        if (!_active) return;
        try
        {
            if (!PowerClearRequest(_handle, PowerRequestSystemRequired))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "PowerClearRequest failed.");
        }
        finally
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
            _active = false;
        }
    }

    public void Dispose() => Release();
}
