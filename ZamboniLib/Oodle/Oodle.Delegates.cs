using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Zamboni.Oodle
{
    // Oodle_LzCompress and Oodle_Compress is one? Their signature seems matched...

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    unsafe delegate int Oodle_Compress(int compressorId, IntPtr src_in, IntPtr dst_in, int src_size, int compressorLevel, CompressOptions* compressorOptions, IntPtr src_window_base, IntPtr c_void);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    delegate int Kraken_Decompress(IntPtr buffer, uint bufferSize, IntPtr result, uint outputBufferSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    delegate long Oodle_LzGetCompressedBufferSizeNeeded(long bufferSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    unsafe delegate ulong Oodle_LzCompress(CompressorType codec, IntPtr buffer, long bufferSize, IntPtr result, CompressorLevel level, IntPtr opts, long offs, ulong unk, IntPtr scr, long scrSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    unsafe delegate ulong Oodle_LzDecompress(IntPtr buffer, long bufferSize, IntPtr result, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int ThreadModule);

}
