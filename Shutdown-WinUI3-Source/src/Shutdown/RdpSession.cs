using System;
using System.Runtime.InteropServices;

namespace ShutdownApp;

internal static class RdpSession
{
    public static bool IsCurrentSessionRemote()
    {
        nint buffer = nint.Zero;
        try
        {
            if (NativeMethods.WTSQuerySessionInformation(
                    nint.Zero,
                    NativeMethods.WTS_CURRENT_SESSION,
                    NativeMethods.WTS_CLIENT_PROTOCOL_TYPE,
                    out buffer,
                    out uint bytesReturned) &&
                buffer != nint.Zero &&
                bytesReturned >= sizeof(short))
            {
                return Marshal.ReadInt16(buffer) != 0;
            }
        }
        catch
        {
            // Fall back to the user32 session flag below.
        }
        finally
        {
            if (buffer != nint.Zero)
                NativeMethods.WTSFreeMemory(buffer);
        }

        return NativeMethods.GetSystemMetrics(NativeMethods.SM_REMOTESESSION) != 0;
    }
}
