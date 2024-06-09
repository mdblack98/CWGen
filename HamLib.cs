using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CWGen
{
    // rigctl TenTec is 1611
    // rigtctl 746Pro is 346

    public static class HamLibStatic
    {
        #region DLL References
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("libhamlib-2.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int rig_init(int model);
        #endregion
        public static Delegate LoadFunction<T>(string dllPath, string functionName)
        {
            var hModule = LoadLibrary(dllPath);
            var functionAddress = GetProcAddress(hModule, functionName);
            return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
        }
    }
    public class HamLib
    {
        public HamLib()
        {
            int r = HamLibStatic.rig_init(1611);
            MessageBox.Show("Got " + r);
        }
    }
}
