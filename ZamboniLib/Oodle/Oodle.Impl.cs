using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Zamboni.Oodle
{
    /// <summary>Interface implementation which allows <seealso cref="Oodle"/> to interact with unmanaged code.</summary>
    public class OodleImpl : IDisposable
    {
        /// <summary>ooz.dll x86 binary filename</summary>
        internal const string OOZ_X86 = "ooz.x86.dll";
        /// <summary>ooz.dll x64 binary filename</summary>
        internal const string OOZ_X64 = "ooz.x64.dll";
        /// <summary>oo2core_8_win64_ binary filename from any sources</summary>
        internal const string OOZ_Ex_X64 = "oo2core_8_win64_.dll";

        /// <summary>The default implementation of <seealso cref="Oodle"/> interface.</summary>
        public static readonly OodleImpl DefaultImplementation = new OodleImpl(Environment.Is64BitProcess ? /* OOZ_X64 */ OOZ_Ex_X64  : OOZ_X86);

        private readonly SafeLibraryHandle libHandle;

        /// <summary>Loads or imports oddle library programmatically and allow <seealso cref="Oodle"/> to interact via the instance.</summary>
        /// <param name="filenameOrLibraryPath">The filename of the library, or a path to the library file.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="filenameOrLibraryPath"/> is <see langword="null"/>.</exception>
        /// <exception cref="DllNotFoundException ">If <paramref name="filenameOrLibraryPath"/> can't be found.</exception>
        /// <exception cref="BadImageFormatException">If the library is not valid.</exception>
        public OodleImpl(string filenameOrLibraryPath)
        {
            var ptr = NativeLibrary.Load(filenameOrLibraryPath); // Let the standard API handling the throw errors procedure if any errors occur.
            if (ptr == IntPtr.Zero) throw new BadImageFormatException();
            this.libHandle = new SafeLibraryHandle(ptr);

            // Gets the exported functions.
            IntPtr func_ptr;

            if (NativeLibrary.TryGetExport(ptr, "Kraken_Decompress", out func_ptr)) // Don't throw EntryPointNotFoundException if not found. Simply let it null.
                this.Kraken_Decompress = Marshal.GetDelegateForFunctionPointer<Kraken_Decompress>(func_ptr);

            if (NativeLibrary.TryGetExport(ptr, "Compress", out func_ptr)) // Don't throw EntryPointNotFoundException if not found. Simply let it null.
                this.Oodle_Compress = Marshal.GetDelegateForFunctionPointer<Oodle_Compress>(func_ptr);

            if (NativeLibrary.TryGetExport(ptr, "OodleLZ_GetCompressedBufferSizeNeeded", out func_ptr)) // Don't throw EntryPointNotFoundException if not found. Simply let it null.
                this.Oodle_LzGetCompressedBufferSizeNeeded = Marshal.GetDelegateForFunctionPointer<Oodle_LzGetCompressedBufferSizeNeeded>(func_ptr);

            if (NativeLibrary.TryGetExport(ptr, "OodleLZ_Compress", out func_ptr)) // Don't throw EntryPointNotFoundException if not found. Simply let it null.
                this.Oodle_LzCompress = Marshal.GetDelegateForFunctionPointer<Oodle_LzCompress>(func_ptr);

            if (NativeLibrary.TryGetExport(ptr, "OodleLZ_Decompress", out func_ptr)) // Don't throw EntryPointNotFoundException if not found. Simply let it null.
                this.Oodle_LzDecompress = Marshal.GetDelegateForFunctionPointer<Oodle_LzDecompress>(func_ptr);
        }

        internal readonly Oodle_LzGetCompressedBufferSizeNeeded? Oodle_LzGetCompressedBufferSizeNeeded;

        internal readonly Oodle_Compress? Oodle_Compress;
        internal readonly Kraken_Decompress? Kraken_Decompress;
        
        internal readonly Oodle_LzCompress? Oodle_LzCompress;
        internal readonly Oodle_LzDecompress? Oodle_LzDecompress;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.libHandle.Dispose();
        }

        ~OodleImpl()
        {
            this.Dispose(false);
        }
    }
}
