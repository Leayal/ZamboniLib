using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Zamboni.Cryptography;
using Zamboni.IceFileFormats;
using Zamboni.Oodle;

namespace Zamboni.IceFormats
{
    public abstract class IceData
    {
        internal static readonly byte[] FilenameNull = { Convert.ToByte('N'), Convert.ToByte('I'), Convert.ToByte('F'), Convert.ToByte('L') };
        protected static readonly int decryptShift = 16;

        /// <summary>Headers of groups</summary>
        public IReadOnlyList<GroupHeader> GroupHeaders { get; protected set; }

        /// <summary>Groups of files</summary>
        public IReadOnlyList<ReadOnlyMemory<byte>> FileGroups { get; protected set; }

        /// <summary>Header of Ice file</summary>
        public ReadOnlyMemory<byte> header { get; protected set; }

        protected abstract int SecondPassThreshold { get; }

        /// <summary>Loads Ice File</summary>
        /// <param name="inStream"></param>
        /// <returns></returns>
        /// <exception cref="ZamboniException"></exception>
        public static IceFile LoadIceFile(Stream inStream)
        {
            if (!inStream.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(inStream));

            inStream.Seek(8L, SeekOrigin.Begin);
            int num = inStream.ReadByte();
            if (num == -1) throw new ZamboniException("Invalid Ice file: Stream ended prematurely.");

            inStream.Seek(0L, SeekOrigin.Begin);
            IceFile iceFile;
            switch (num)
            {
                case 3:
                    iceFile = new IceV3File(inStream);
                    break;
                case 4:
                    iceFile = new IceV4File(inStream);
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    iceFile = new IceV5File(inStream);
                    break;
                default:
                    throw new ZamboniException("Invalid version: " + num);
            }

            inStream.Dispose();
            return iceFile;
        }

        /// <summary>When overriden, implement to parse ICE header from the stream</summary>
        /// <param name="dataStream"></param>
        protected abstract void ParseHeader(Stream dataStream);

        /// <summary>Gets File name from bytes</summary>
        /// <param name="fileToWrite"></param>
        /// <returns></returns>
        public static string getFileName(ReadOnlySpan<byte> fileToWrite, int index = -1)
        {
            //Bounds check for file
            if (fileToWrite == null || fileToWrite.Length == 0)
            {
                return "nullFile";
            }

            if (fileToWrite.Slice(0, 4).SequenceEqual(FilenameNull))
            {
                if (index == -1)
                {
                    Debug.WriteLine("No index provided, files may overwrite each other.");
                }

                return $"namelessNIFLFile_{index}.bin";
            }

            //Handle headerless files. ICE Files, as a rule, do not seem to allow upper case characters. Here, we'll assume that normal non caps ascii is allowed. Outside that range probably isn't a normal file.
            bool isNotLowerCaseOrSpecialChar = fileToWrite[0] > 126 || fileToWrite[0] < 91;
            bool isNotANumberOrSpecialChar = fileToWrite[0] > 64 || fileToWrite[0] < 32;
            if (isNotLowerCaseOrSpecialChar && isNotANumberOrSpecialChar)
            {
                if (index == -1)
                {
                    Debug.WriteLine("No index provided, files may overwrite each other.");
                }

                return $"namelessFile_{index}.bin";
            }
            int int32 = BitConverter.ToInt32(fileToWrite.Slice(0x10));
            return Encoding.ASCII.GetString(fileToWrite.Slice(0x40, int32)).TrimEnd(char.MinValue);
        }

        protected ReadOnlyMemory<byte>[] splitGroup(byte[] groupToSplit, int fileCount)
        {
            //Bounds check for group
            if (groupToSplit == null || groupToSplit.Length == 0)
            {
                return Array.Empty<ReadOnlyMemory<byte>>();
            }

            ReadOnlyMemory<byte>[] fileArray = new ReadOnlyMemory<byte>[fileCount];
            int sourceIndex = 0;

            //Handle headerless files. ICE Files, as a rule, do not seem to allow upper case characters. Here, we'll assume that normal non caps ascii is allowed. Outside that range probably isn't a normal file.
            bool isNotLowerCaseOrSpecialChar = groupToSplit[0] > 126 || groupToSplit[0] < 91;
            bool isNotANumberOrSpecialChar = groupToSplit[0] > 64 || groupToSplit[0] < 32;
            if (groupToSplit.AsSpan(0, 4).SequenceEqual(FilenameNull))
            {
                for (int index = 0; index < fileCount && sourceIndex < groupToSplit.Length; ++index)
                {
                    // Since the index 0 already has been checked by the first IF above, don't need to re-check again.
                    if (index == 0 || groupToSplit.AsSpan(sourceIndex, 4).SequenceEqual(FilenameNull))
                    {
                        int size = BitConverter.ToInt32(groupToSplit, sourceIndex + 0x14); //Main NIFL Size
                        int nof0Size = BitConverter.ToInt32(groupToSplit, sourceIndex + size + 0x4) + 0x8; //NOF0 size
                        nof0Size += 0x10 - (nof0Size % 0x10); //Add padding bytes
                        size += nof0Size + 0x10; //Add in NOF0 size and NEND bytes

                        // fileArray[index] = new byte[size];
                        fileArray[index] = new ReadOnlyMemory<byte>(groupToSplit, sourceIndex, size);
                        // Array.Copy(groupToSplit, sourceIndex, fileArray[index], 0, size);
                        sourceIndex += size;
                    }
                    else
                    {
                        int namelessSize = groupToSplit.Length - sourceIndex;
                        // fileArray[index] = new byte[namelessSize];

                        if (fileCount > index + 1)
                        {
                            Debug.WriteLine(
                                $"Unhandled file count {fileCount}, outputting nameless file for index {index} and remaining file data.");
                        }

                        fileArray[index] = new ReadOnlyMemory<byte>(groupToSplit, sourceIndex, namelessSize);
                        // Array.Copy(groupToSplit, sourceIndex, fileArray[index], 0, namelessSize);
                        return fileArray;
                    }
                }

                return fileArray;
            }

            if (isNotLowerCaseOrSpecialChar && isNotANumberOrSpecialChar)
            {
                fileArray[0] = groupToSplit;
                if (fileCount > 1)
                {
                    Debug.WriteLine($"Unhandled file count {fileCount}, outputting only one nameless file.");
                }

                return fileArray;
            }

            for (int index = 0; index < fileCount && sourceIndex < groupToSplit.Length; ++index)
            {
                int size = BitConverter.ToInt32(groupToSplit, sourceIndex + 4);
                // fileArray[index] = new byte[size];
                fileArray[index] = new ReadOnlyMemory<byte>(groupToSplit, sourceIndex, size);
                // Array.Copy(groupToSplit, sourceIndex, fileArray[index], 0, size);
                sourceIndex += size;
            }

            return fileArray;
        }

        protected byte[] combineGroup(ReadOnlyMemory<byte>[] filesToJoin, bool headerLess = true)
        {
            // List<byte> outBytes = new List<byte>();
            // Pre-calc the size
            int size = 0;
            for (int i = 0; i < filesToJoin.Length; i++)
            {
                //Apply file padding as we need it.
                int iceFileSize = BitConverter.ToInt32(filesToJoin[i].Span.Slice(0x4));
                int potentialPadding = 0x10 - (iceFileSize % 0x10);

                if (potentialPadding > 0 && potentialPadding != 0x10)
                {
                    size += (filesToJoin[i].Length + potentialPadding);
                }
                else
                {
                    size += filesToJoin[i].Length;
                }
            }

            var arr = new byte[size];
            Span<byte> output = arr;
            size = 0;

            // Start to fill out the alloc buffer
            for (int i = 0; i < filesToJoin.Length; i++)
            {
                //Apply file padding as we need it.
                int iceFileSize = BitConverter.ToInt32(filesToJoin[i].Span.Slice(0x4));
                int potentialPadding = 0x10 - (iceFileSize % 0x10);

                if (potentialPadding > 0 && potentialPadding != 0x10)
                {
                    filesToJoin[i].Span.CopyTo(output.Slice(size));
                    BitConverter.TryWriteBytes(output.Slice(size + 0x4), iceFileSize + potentialPadding);
                    size += (filesToJoin[i].Length + potentialPadding);
                }
                else
                {
                    // outBytes.AddRange(filesToJoin[i]);
                    filesToJoin[i].Span.CopyTo(output.Slice(size));
                    size += filesToJoin[i].Length;
                }
            }

            return arr;
        }

        protected Memory<byte> decryptGroup(byte[] buffer, uint key1, uint key2, bool v3Decrypt)
        {
            Span<byte> block1;
            // byte[] block1 = new byte[buffer.Length];
            if (v3Decrypt == false)
            {
                block1 = FloatageFish.decrypt_block(new Span<byte>(buffer), key1, decryptShift);
            }
            else
            {
                block1 = new Span<byte>(buffer);
                // Array.Copy(buffer, 0, block1, 0, buffer.Length);
            }

            var block2 = new BlewFish(ReverseBytes(key1)).decryptBlock(block1);
            if (block2.Length <= SecondPassThreshold && v3Decrypt == false)
            {
                return new BlewFish(ReverseBytes(key2)).decryptBlock(block2);
            }

            return block2;
        }

        public uint ReverseBytes(uint x)
        {
            x = (x >> 16) | (x << 16);
            return ((x & 4278255360U) >> 8) | (uint)(((int)x & 16711935) << 8);
        }

        protected ValueTuple<GroupHeader, GroupHeader> readHeaders(byte[] decryptedHeaderData)
        {
            var groupHeaderArray = new ValueTuple<GroupHeader, GroupHeader>(new GroupHeader()
            {
                decompSize = BitConverter.ToUInt32(decryptedHeaderData, 0),
                compSize = BitConverter.ToUInt32(decryptedHeaderData, 4),
                count = BitConverter.ToUInt32(decryptedHeaderData, 8),
                CRC = BitConverter.ToUInt32(decryptedHeaderData, 12)
            }, new GroupHeader()
            {
                decompSize = BitConverter.ToUInt32(decryptedHeaderData, 16),
                compSize = BitConverter.ToUInt32(decryptedHeaderData, 20),
                count = BitConverter.ToUInt32(decryptedHeaderData, 24),
                CRC = BitConverter.ToUInt32(decryptedHeaderData, 28)
            });
            return groupHeaderArray;
        }

        protected byte[] extractGroup(
            GroupHeader header,
            BinaryReader openReader,
            bool encrypt,
            uint groupOneTempKey,
            uint groupTwoTempKey,
            bool ngsMode,
            bool v3Decrypt = false)
        {
            byte[] buffer = openReader.ReadBytes((int)header.getStoredSize());
            var inData = !encrypt ? buffer : decryptGroup(buffer, groupOneTempKey, groupTwoTempKey, v3Decrypt);
            if (header.compSize <= 0U)
            {
                if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)inData, out var segment) && segment.Array != null && segment.Count == segment.Array.Length)
                {
                    return segment.Array;
                }
                else
                {
                    return inData.ToArray();
                }
            }
            else
            {
                return ngsMode ? decompressGroup(inData.Span, header.decompSize) : decompressGroupNgs(inData, header.decompSize);
            }
        }

        protected byte[] decompressGroup(Span<byte> inData, uint expectedOutputLength)
        {
            var len = inData.Length;
            byte[] xorred = new byte[inData.Length];
            // Array.Copy(inData, input, input.Length);
            const byte xorKey = 149;
            for (int index = 0; index < inData.Length; ++index)
            {
                xorred[index] = (byte)(inData[index] ^ xorKey);
                // byte xorVal = xorKey;
                // xorred[index] = xorVal ^= inData[index];
            }
            return PrsCompDecomp.Decompress(xorred, expectedOutputLength);
        }

        protected byte[]? decompressGroupNgs(ReadOnlyMemory<byte> inData, uint bufferLength)
        {
            return Oodle.Oodle.Decompress(inData, bufferLength);
        }

        protected Memory<byte> compressGroupNgs(byte[] buffer, CompressorLevel compressorLevel = CompressorLevel.Optimal1)
        {
            return Oodle.Oodle.OodleCompress(buffer, compressorLevel);
        }

        protected Memory<byte> getCompressedContents(byte[] buffer, bool compress,
            CompressorLevel compressorLevel = CompressorLevel.Fast)
        {
            if ((uint)buffer.Length <= 0U || compress == false)
            {
                return buffer;
            }

            return compressGroupNgs(buffer, compressorLevel);
            if (!compress || (uint)buffer.Length <= 0U)
            {
                return buffer;
            }

            var numArray = PrsCompDecomp.Compress(buffer);
            var spanOfnumArray = numArray.Span;
            for (int index = 0; index < numArray.Length; ++index)
            {
                spanOfnumArray[index] ^= 149;
            }

            return numArray;
        }

        protected Span<byte> packGroup(Span<byte> buffer, uint key1, uint key2, bool encrypt)
        {
            if (!encrypt)
            {
                return buffer;
            }

            var block = buffer;
            if (buffer.Length <= SecondPassThreshold)
            {
                block = new BlewFish(ReverseBytes(key2)).encryptBlock(buffer);
            }

            var data_block = new BlewFish(ReverseBytes(key1)).encryptBlock(block);
            return FloatageFish.decrypt_block(data_block, key1);
        }

        public record GroupHeader
        {
            public uint compSize { get; init; }
            public uint count { get; init; }
            public uint CRC { get; init; }
            public uint decompSize { get; init; }

            public uint getStoredSize()
            {
                return compSize > 0U ? compSize : decompSize;
            }
        }
    }
}
