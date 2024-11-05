// Decompiled with JetBrains decompiler
// Type: psu_generic_parser.PrsCompressor
// Assembly: zamboni, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 73B487C9-8F41-4586-BEF5-F7D7BFBD4C55
// Assembly location: D:\Downloads\zamboni_ngs (3)\zamboni.exe

using System;
using System.Collections.Generic;

namespace Zamboni.Cryptography
{
    class PrsCompressor : CompressionBuffer
    {
        private readonly static Tuple<List<int>, int> EmptyTuple = new Tuple<List<int>, int>(new List<int>(), 0);
        // private byte[] compBuffer;
        // private int ctrlBitCounter;
        // private int ctrlByteCounter;
        // private int outLoc;

        private PrsCompressor(int length) : base(length) { }

        public static Memory<byte> compress(Span<byte> toCompress)
        {
            var buffer = new PrsCompressor(toCompress.Length);
            Dictionary<byte, Tuple<List<int>, int>> offsetDictionary = buildOffsetDictionary(toCompress);
            buffer.ctrlByteCounter = 0;
            int length = toCompress.Length;
            var compBuffer = buffer.compBuffer;
            buffer.outLoc = 3;
            buffer.ctrlBitCounter = 2;
            compBuffer[0] = 3;

            // Array.Copy(toCompress, 0, compBuffer, 1, 2);
            toCompress.Slice(0, 2).CopyTo(new Span<byte>(compBuffer, 1, 2));
            
            int currentOffset = 2;
            while (currentOffset < length)
            {
                Tuple<List<int>, int> offsetList =
                    getOffsetList(offsetDictionary, toCompress[currentOffset], currentOffset);
                int count = 2;
                int num1 = -1;
                int num2 = currentOffset - 256;
                for (int index = offsetList.Item2;
                     index < offsetList.Item1.Count && offsetList.Item1[index] < currentOffset;
                     ++index)
                {
                    int num3 = offsetList.Item1[index];
                    int num4 = 0;
                    int num5 = Math.Min(length - currentOffset, 256);
                    while (num4 < num5 && toCompress[num3 + num4] == toCompress[currentOffset + num4])
                    {
                        ++num4;
                    }

                    if ((num4 > 2 || num3 > num2) && (num4 > count || num4 == count && num3 > num1))
                    {
                        count = num4;
                        num1 = num3;
                    }
                }

                if (num1 == -1 || currentOffset - num1 > 256 && count < 3)
                {
                    buffer.writeRawByte(toCompress[currentOffset++]);
                }
                else
                {
                    if (count < 6 && currentOffset - num1 < 256)
                    {
                        buffer.writeShortReference(count, (byte)(num1 - (currentOffset - 256)));
                    }
                    else
                    {
                        buffer.writeLongReference(count, num1 - (currentOffset - 8192));
                    }

                    currentOffset += count;
                }
            }

            buffer.finalizeCompression();
            // Array.Resize(ref compBuffer, outLoc);
            return buffer.Memory;
        }

        private static Tuple<List<int>, int> getOffsetList(
            Dictionary<byte, Tuple<List<int>, int>> offsetDictionary,
            byte currentVal,
            int currentOffset)
        {
            // Tuple<List<int>, int> offset = offsetDictionary[currentVal];
            if (!offsetDictionary.TryGetValue(currentVal, out var offset) || offset == null)
            // if (offset == null)
            {
                return EmptyTuple;
            }

            if (offset.Item2 < currentOffset - 8176)
            {
                int index = offset.Item2;
                while (offset.Item1[index] < currentOffset - 8176 && index < offset.Item1.Count)
                {
                    ++index;
                }

                Tuple<List<int>, int> tuple = new Tuple<List<int>, int>(offset.Item1, index);
                offsetDictionary[currentVal] = tuple;
            }

            return offsetDictionary[currentVal];
        }

        private static Dictionary<byte, Tuple<List<int>, int>> buildOffsetDictionary(Span<byte> toCompress)
        {
            Dictionary<byte, Tuple<List<int>, int>> dictionary = new Dictionary<byte, Tuple<List<int>, int>>(toCompress.Length);
            for (int index = 0; index < toCompress.Length; ++index)
            {
                byte key = toCompress[index];
                Tuple<List<int>, int>? item;
                if (!dictionary.TryGetValue(key, out item))
                {
                    item = new Tuple<List<int>, int>(new List<int>(), 0);
                    dictionary.Add(key, item);
                }

                item.Item1.Add(index);
            }

            return dictionary;
        }

        private void finalizeCompression()
        {
            addCtrlBit(0);
            addCtrlBit(1);
            compBuffer[outLoc++] = 0;
            compBuffer[outLoc++] = 0;
        }

        private void writeRawByte(byte val)
        {
            addCtrlBit(1);
            compBuffer[outLoc++] = val;
        }

        private void writeShortReference(int count, byte offset)
        {
            addCtrlBit(0);
            addCtrlBit(0);
            addCtrlBit(count - 2 >> 1);
            addCtrlBit(count - 2 & 1);
            compBuffer[outLoc++] = offset;
        }

        private void writeLongReference(int count, int offset)
        {
            addCtrlBit(0);
            addCtrlBit(1);
            ushort num = (ushort)(offset << 3);
            if (count <= 9)
            {
                num |= (ushort)(count - 2);
            }

            // BitConverter.GetBytes(num).CopyTo(compBuffer, outLoc);
            BitConverter.TryWriteBytes(new Span<byte>(compBuffer, outLoc, compBuffer.Length - outLoc), num);
            outLoc += 2;
            if (count <= 9)
            {
                return;
            }

            compBuffer[outLoc++] = (byte)(count - 10);
        }

        private void addCtrlBit(int input)
        {
            if (ctrlBitCounter == 8)
            {
                ctrlBitCounter = 0;
                ctrlByteCounter = outLoc++;
            }

            compBuffer[ctrlByteCounter] |= (byte)(input << ctrlBitCounter);
            ++ctrlBitCounter;
        }

        private interface ICompressionChunk
        {
            void encode(CompressionBuffer buff);
        }
    }

    class CompressionBuffer
    {
        public readonly byte[] compBuffer;
        public int ctrlBitCounter;
        public int ctrlByteCounter;
        public int outLoc;

        public Span<byte> Span => new Span<byte>(this.compBuffer, 0, outLoc);

        public Memory<byte> Memory => new Memory<byte>(this.compBuffer, 0, outLoc);

        public CompressionBuffer(int length)
        {
            this.compBuffer = new byte[length];
        }
    }
}
