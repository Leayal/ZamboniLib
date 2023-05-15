using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zamboni
{
    class SafeLibraryHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeLibraryHandle(IntPtr libraryHandle) : base(true)
        {
            this.SetHandle(libraryHandle);
        }

        protected override bool ReleaseHandle()
        {
            try
            {
                NativeLibrary.Free(handle);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
