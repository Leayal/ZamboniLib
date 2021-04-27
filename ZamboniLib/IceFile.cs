﻿// Decompiled with JetBrains decompiler
// Type: zamboni.IceFile
// Assembly: zamboni, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 73B487C9-8F41-4586-BEF5-F7D7BFBD4C55
// Assembly location: D:\Downloads\zamboni_ngs (3)\zamboni.exe

using PhilLibX.Compression;
using System;
using System.IO;

namespace zamboni
{
    public abstract class IceFile
    {
        protected int decryptShift = 16;

        public byte[][] groupOneFiles { get; set; }

        public byte[][] groupTwoFiles { get; set; }

        public byte[] header { get; set; }

        protected abstract int SecondPassThreshold { get; }

        public static IceFile LoadIceFile(Stream inStream)
        {
            inStream.Seek(8L, SeekOrigin.Begin);
            int num = inStream.ReadByte();
            inStream.Seek(0L, SeekOrigin.Begin);
            IceFile iceFile;
            switch (num)
            {
                case 4:
                    iceFile = (IceFile)new IceV4File(inStream);
                    break;
                case 5:
                    iceFile = (IceFile)new IceV5File(inStream);
                    break;
                case 6:
                    iceFile = (IceFile)new IceV5File(inStream);
                    break;
                case 7:
                    iceFile = (IceFile)new IceV5File(inStream);
                    break;
                case 8:
                    iceFile = (IceFile)new IceV5File(inStream);
                    break;
                case 9:
                    iceFile = (IceFile)new IceV5File(inStream);
                    break;
                default:
                    throw new Exception("Invalid version: " + num.ToString());
            }
            inStream.Dispose();
            return iceFile;
        }

        public static string getFileName(byte[] fileToWrite) 
        { 
            int int32 = BitConverter.ToInt32(fileToWrite, 0x10); 
            return Encoding.ASCII.GetString(fileToWrite, 0x40, int32).TrimEnd(new char[1]); 
        } 
 
        protected byte[][] splitGroup(byte[] groupToSplit, int fileCount)
        {
            byte[][] numArray = new byte[fileCount][];
            int sourceIndex = 0;
            for (int index = 0; index < fileCount && sourceIndex < groupToSplit.Length; ++index)
            {
                int int32 = BitConverter.ToInt32(groupToSplit, sourceIndex + 4);
                numArray[index] = new byte[int32];
                Array.Copy((Array)groupToSplit, sourceIndex, (Array)numArray[index], 0, int32);
                sourceIndex += int32;
            }
            return numArray;
        }

        protected byte[] combineGroup(byte[][] filesToJoin)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter((Stream)memoryStream);
            for (int index = 0; index < filesToJoin.Length; ++index)
                binaryWriter.Write(filesToJoin[index]);
            return memoryStream.ToArray();
        }

        protected byte[] decryptGroup(byte[] buffer, uint key1, uint key2)
        {
            byte[] block1 = FloatageFish.decrypt_block(buffer, (uint)buffer.Length, key1, this.decryptShift);
            byte[] block2 = new BlewFish(this.ReverseBytes(key1)).decryptBlock(block1);
            byte[] numArray = block2;
            if (block2.Length <= this.SecondPassThreshold)
                numArray = new BlewFish(this.ReverseBytes(key2)).decryptBlock(block2);
            return numArray;
        }

        public uint ReverseBytes(uint x)
        {
            x = x >> 16 | x << 16;
            return (x & 4278255360U) >> 8 | (uint)(((int)x & 16711935) << 8);
        }

        protected IceFile.GroupHeader[] readHeaders(byte[] decryptedHeaderData)
        {
            IceFile.GroupHeader[] groupHeaderArray = new IceFile.GroupHeader[2]
            {
        new IceFile.GroupHeader(),
        null
            };
            groupHeaderArray[0].decompSize = BitConverter.ToUInt32(decryptedHeaderData, 0);
            groupHeaderArray[0].compSize = BitConverter.ToUInt32(decryptedHeaderData, 4);
            groupHeaderArray[0].count = BitConverter.ToUInt32(decryptedHeaderData, 8);
            groupHeaderArray[0].CRC = BitConverter.ToUInt32(decryptedHeaderData, 12);
            groupHeaderArray[1] = new IceFile.GroupHeader();
            groupHeaderArray[1].decompSize = BitConverter.ToUInt32(decryptedHeaderData, 16);
            groupHeaderArray[1].compSize = BitConverter.ToUInt32(decryptedHeaderData, 20);
            groupHeaderArray[1].count = BitConverter.ToUInt32(decryptedHeaderData, 24);
            groupHeaderArray[1].CRC = BitConverter.ToUInt32(decryptedHeaderData, 28);
            return groupHeaderArray;
        }

        protected byte[] extractGroup(
          IceFile.GroupHeader header,
          BinaryReader openReader,
          bool encrypt,
          uint groupOneTempKey,
          uint groupTwoTempKey,
          bool ngsMode)
        {
            byte[] buffer = openReader.ReadBytes((int)header.getStoredSize());
            byte[] inData = !encrypt ? buffer : this.decryptGroup(buffer, groupOneTempKey, groupTwoTempKey);
            return header.compSize <= 0U ? inData : (!ngsMode ? this.decompressGroup(inData, header.decompSize) : this.decompressGroupNgs(inData, header.decompSize));
        }

        protected byte[] decompressGroup(byte[] inData, uint bufferLength)
        {
            byte[] input = new byte[inData.Length];
            Array.Copy((Array)inData, (Array)input, input.Length);
            for (int index = 0; index < input.Length; ++index)
                input[index] ^= (byte)149;
            return PrsCompDecomp.Decompress(input, bufferLength);
        }

        protected byte[] decompressGroupNgs(byte[] inData, uint bufferLength) => Oodle.Decompress(inData, (long)bufferLength);

        protected byte[] getCompressedContents(byte[] buffer, bool compress)
        {
            if (!compress || (uint)buffer.Length <= 0U)
                return buffer;
            byte[] numArray = PrsCompDecomp.compress(buffer);
            for (int index = 0; index < numArray.Length; ++index)
                numArray[index] ^= (byte)149;
            return numArray;
        }

        protected byte[] packGroup(byte[] buffer, uint key1, uint key2, bool encrypt)
        {
            if (!encrypt)
                return buffer;
            byte[] block = buffer;
            if (buffer.Length <= this.SecondPassThreshold)
                block = new BlewFish(this.ReverseBytes(key2)).encryptBlock(buffer);
            byte[] data_block = new BlewFish(this.ReverseBytes(key1)).encryptBlock(block);
            return FloatageFish.decrypt_block(data_block, (uint)data_block.Length, key1);
        }

        public class GroupHeader
        {
            public uint decompSize;
            public uint compSize;
            public uint count;
            public uint CRC;

            public uint getStoredSize() => this.compSize > 0U ? this.compSize : this.decompSize;
        }
    }
}