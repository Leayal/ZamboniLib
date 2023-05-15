// Decompiled with JetBrains decompiler
// Type: PhilLibX.Compression.Oodle
// Assembly: zamboni, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 73B487C9-8F41-4586-BEF5-F7D7BFBD4C55
// Assembly location: D:\Downloads\zamboni_ngs (3)\zamboni.exe

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;

namespace Zamboni.Oodle
{
    /// <remarks>ooz.dll wrapper</remarks>
    public static class Oodle
    {
        private static OodleImpl _main_implementation = OodleImpl.DefaultImplementation;
        private static OodleImpl? _extended_implemetation = TryInitExImpl();

        private static OodleImpl? TryInitExImpl()
        {
            OodleImpl? result;
            try
            {
                result = new OodleImpl(OodleImpl.OOZ_Ex_X64);
            }
            catch
            {
                result = null;
            }
            return result;
        }

        /// <summary>Sets implementation for <seealso cref="Oodle"/> to consume without changing the extended implementation.</summary>
        /// <param name="implementation">The main implementation to change to. Usually is the "ooz.x64.dll" file. Default is <seealso cref="OodleImpl.DefaultImplementation"/>.</param>
        public static void SetImpl(OodleImpl implementation)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));

            _main_implementation = implementation;
        }

        /// <summary>Sets extended implementation for <seealso cref="Oodle"/> to consume.</summary>
        /// <param name="implementation"><para>The extended implementation. Usually is any ooz libraries you found from other games. Can be <see langword="null"/> and must <b>NOT</b> be <seealso cref="OodleImpl.DefaultImplementation"/>.</para></param>
        /// <remarks>
        /// <para>If <paramref name="implementation"/> is:</para>
        /// <para>- <see langword="null"/>: the existing main implementation will be used for all compression and decompression types.</para>
        /// <para>- Non-<see langword="null"/>: the <paramref name="implementation"/> will be used for all non-kraken compression and decompression types.</para>
        /// </remarks>
        public static void SetExImpl(OodleImpl? implementation)
        {
            _extended_implemetation = implementation;
        }

        /// <summary>Sets implementations for <seealso cref="Oodle"/> to consume.</summary>
        /// <param name="main_implementation">The main implementation. Usually is the "ooz.x64.dll" file. Default is <seealso cref="OodleImpl.DefaultImplementation"/>.</param>
        /// <param name="extended_implemetation"><para>The extended implementation. Usually is any ooz libraries you found from other games. Can be <see langword="null"/> and must <b>NOT</b> be <seealso cref="OodleImpl.DefaultImplementation"/>.</para></param>
        /// <remarks>
        /// <para>If <paramref name="extended_implemetation"/> is:</para>
        /// <para>- <see langword="null"/>: the <paramref name="main_implementation"/> will be used for all compression and decompression types.</para>
        /// <para>- Non-<see langword="null"/>: the <paramref name="extended_implemetation"/> will be used for all non-kraken compression and decompression types.</para>
        /// </remarks>
        public static void SetImpl(OodleImpl main_implementation, OodleImpl? extended_implemetation)
        {
            if (main_implementation == null) throw new ArgumentNullException(nameof(main_implementation));

            _main_implementation = main_implementation;
            _extended_implemetation = extended_implemetation;
        }

        public static CompressOptions GetDefaultCompressOpts(int level)
        {
            return level switch
            {
                4 => CompressOptions.GetCompressOptions(0, 0, 0, 0x40000, 0, 0, 0x100, 2, 0, 0x400000, 1, 0),
                _ => level > 4 ? CompressOptions.GetCompressOptions(0, 0, 0, 0x40000, 0, 0, 0x100, 4, 0, 0x400000, 1, 0) : CompressOptions.GetCompressOptions(0, 0, 0, 0x40000, 0, 0, 0x100, 1, 0, 0x400000, 0, 0)
            };
        }

        public static int GetCompressedBufferSizeNeeded(int size)
        {
            return size + 274 * ((size + 0x3FFFF) / 0x40000);
        }

        /// <summary>Decompress via Kraken</summary>
        /// <param name="input">Input binary</param>
        /// <param name="decompressedLength">output binary size</param>
        /// <returns></returns>
        /// <exception cref="ZamboniException">DLL not found or invalid DLL.</exception>
        public static byte[]? Decompress(ReadOnlyMemory<byte> input, long decompressedLength)
        {
            if (_main_implementation.Kraken_Decompress == null) throw new ZamboniException("Main implementation missing \"Kraken_Decompress\" function.");

            byte[] result = new byte[decompressedLength];
            var pinnedIn = input.Pin();
            try
            {
                unsafe
                {
                    fixed (byte* ptrOut = result)
                    {
                        if (_main_implementation.Kraken_Decompress.Invoke(new IntPtr(pinnedIn.Pointer), (uint)input.Length, new IntPtr(ptrOut), (uint)decompressedLength) == 0L)
                            return null;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new ZamboniException(ex);
            }
            finally
            {
                pinnedIn.Dispose();
            }
        }

        /// <summary>Compress via Kraken</summary>
        /// <param name="input">Input binary</param>
        /// <param name="level">Comporessor Level</param>
        /// <returns><seealso cref="Memory{T}"/> contains the compressed data.</returns>
        /// <exception cref="ZamboniException">Dll not found or invalid DLL.</exception>
        public static Memory<byte> Compress(ReadOnlyMemory<byte> input, CompressorLevel level = CompressorLevel.Fast)
        {
            if (_main_implementation.Oodle_Compress == null) throw new ZamboniException("Main implementation missing \"Compress\" function.");

            CompressOptions compressOptions = GetDefaultCompressOpts((int)level);
            //IntPtr compressOptionsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<CompressOptions>());
            //Marshal.StructureToPtr(compressOptions, compressOptionsPtr, false);

            Memory<byte> result = new byte[GetCompressedBufferSizeNeeded(input.Length)];
            int compSize;

            var memSrcHandle = input.Pin();
            var memDstHandle = result.Pin();
            try
            {
                unsafe
                {
                    compSize = _main_implementation.Oodle_Compress.Invoke((int)CompressorType.Kraken, new IntPtr(memSrcHandle.Pointer), new IntPtr(memDstHandle.Pointer), input.Length, (int)level, (CompressOptions*)Unsafe.AsPointer(ref compressOptions), IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                throw new ZamboniException(ex);
            }
            finally
            {
                memSrcHandle.Dispose();
                memDstHandle.Dispose();
            }

            // Marshal.FreeHGlobal(compressOptionsPtr);
            // Array.Resize(ref result, compSize);

            return result.Slice(0, compSize);
        }

        public static byte[]? OodleDecompress(ReadOnlyMemory<byte> input, long decompressedLength)
        {
            Oodle_LzDecompress? func = _extended_implemetation?.Oodle_LzDecompress ?? _main_implementation.Oodle_LzDecompress;
            if (func == null) throw new ZamboniException("Neither Extended implementation and Main implementation have  \"OodleLZ_Decompress\" function.");

            byte[] result = new byte[decompressedLength];

            var memSrcHandle = input.Pin();
            try
            {
                unsafe
                {
                    fixed (byte* ptrOut = result)
                    {
                        if (func.Invoke(new IntPtr(memSrcHandle.Pointer), input.Length, new IntPtr(ptrOut), decompressedLength, 0, 0, 0, 0L, 0L, 0L, 0L, 0L, 0L, 3) == 0L)
                            return null;
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new ZamboniException(ex);
            }
            finally
            {
                memSrcHandle.Dispose();
            }
        }

        public static Memory<byte> OodleCompress(ReadOnlyMemory<byte> source, CompressorLevel level)
        {
            return OodleCompress(source, CompressorType.Kraken, level);
        }

        public static Memory<byte> OodleCompress(ReadOnlyMemory<byte> source, CompressorType codec, CompressorLevel level)
        {
            Oodle_LzCompress? func = _extended_implemetation?.Oodle_LzCompress ?? _main_implementation.Oodle_LzCompress;
            if (func == null) throw new ZamboniException("Neither Extended implementation and Main implementation have  \"OodleLZ_Compress\" function.");

            Memory<byte> result = new byte[(int)(source.Length * 1.1)];
            var memSrcHandle = source.Pin();
            var memDstHandle = result.Pin();
            try
            {
                ulong resultSize;
                unsafe
                {
                    resultSize = func(codec, new IntPtr(memSrcHandle.Pointer), source.Length, new IntPtr(memDstHandle.Pointer), level, IntPtr.Zero, 0, 0, IntPtr.Zero, 0);
                }
                var convertedSize = Convert.ToInt32(resultSize);
                return result.Slice(0, convertedSize);
            }
            catch (Exception ex)
            {
                throw new ZamboniException(ex);
            }
            finally
            {
                memSrcHandle.Dispose();
                memDstHandle.Dispose();
            }
        }
    }
}
