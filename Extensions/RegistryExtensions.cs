using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Pete
{
    public static class RegistryExtensions
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RegQueryInfoKeyA(
            SafeRegistryHandle hKey, 
            StringBuilder lpClass, 
            [In, Out] ref uint lpcbClass, 
            IntPtr lpReserved, IntPtr lpcSubKeys, IntPtr lpcbMaxSubKeyLen, 
            IntPtr lpcbMaxClassLen, IntPtr lpcValues, IntPtr lpcbMaxValueNameLen, 
            IntPtr lpcbMaxValueLen, IntPtr lpcbSecurityDescriptor,
            out FILETIME lpftLastWriteTime);

        public static DateTime GetLastWrite(this RegistryKey key)
        {
            uint cls = 0;
            RegQueryInfoKeyA(key.Handle, null, ref cls, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out FILETIME lastWrite);

            long time = (((long)lastWrite.dwHighDateTime) << 32) + lastWrite.dwLowDateTime;

            return DateTime.FromFileTimeUtc(time);
        }
    }
}
