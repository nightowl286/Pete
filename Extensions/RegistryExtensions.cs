using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Pete
{
    public static class RegistryExtensions
    {
        private enum _KEY_SET_INFORMATION_CLASS
        {
            KeyWriteTimeInformation,
            KeyWow64FlagsInformation,
            KeyControlFlagsInformation,
            KeySetVirtualizationInformation,
            KeySetDebugInformation,
            KeySetHandleTagsInformation,
            KeySetLayerInformation,
            MaxKeySetInfoClass
        }

        [DllImport("advapi32.dll")]
        private static extern int RegQueryInfoKeyW(
            SafeRegistryHandle hKey,
            StringBuilder lpClass,
            ref uint lpcbClass,
            IntPtr lpReserved, IntPtr lpcSubKeys, IntPtr lpcbMaxSubKeyLen,
            IntPtr lpcbMaxClassLen, IntPtr lpcValues, IntPtr lpcbMaxValueNameLen,
            IntPtr lpcbMaxValueLen, IntPtr lpcbSecurityDescriptor,
            out long lpftLastWriteTime);

        [DllImport("Ntdll.dll")]
        private static extern int NtSetInformationKey(IntPtr handle, _KEY_SET_INFORMATION_CLASS weird, ref long somePointer, ulong someNum);

        public static DateTime GetLastWrite(this RegistryKey key)
        {
            uint cls = 0;
            RegQueryInfoKeyW(key.Handle, null, ref cls, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out long lastWrite);

            DateTime date = DateTime.FromFileTimeUtc(lastWrite);

            return date;
        }
        public static void SetLastWrite(this RegistryKey key, DateTime date)
        {
            long time = date.ToFileTimeUtc();
            NtSetInformationKey(key.Handle.DangerousGetHandle(), _KEY_SET_INFORMATION_CLASS.KeyWriteTimeInformation, 
                ref time, sizeof(ulong));
        }
    }
}
