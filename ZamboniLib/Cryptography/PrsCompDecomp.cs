// Decompiled with JetBrains decompiler
// Type: zamboni.PrsCompDecomp
// Assembly: zamboni, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 73B487C9-8F41-4586-BEF5-F7D7BFBD4C55
// Assembly location: D:\Downloads\zamboni_ngs (3)\zamboni.exe

using System;

namespace Zamboni.Cryptography
{
    public static class PrsCompDecomp
    {
        // private byte ctrlByte;
        // private int ctrlByteCounter;
        // private int currDecompPos;
        // private byte[] decompBuffer;

        private static bool getCtrlBit(Span<byte> buffer, ref int ctrlByteCounter, ref int currDecompPos, ref byte ctrlByte)
        {
            --ctrlByteCounter;
            if (ctrlByteCounter == 0)
            {
                ctrlByte = buffer[currDecompPos++];
                ctrlByteCounter = 8;
            }

            bool flag = (ctrlByte & 1U) > 0U;
            ctrlByte >>= 1;
            return flag;
        }

        public static byte[] Decompress(Span<byte> input, long expectedOutputLength)
        {
            byte[] outData = new byte[expectedOutputLength];
            // decompBuffer = input;
            byte ctrlByte = 0;
            int ctrlByteCounter = 1, currDecompPos = 0;
            int outIndex = 0;
            try
            {
                while (outIndex < expectedOutputLength && currDecompPos < input.Length)
                {
                    while (getCtrlBit(input, ref ctrlByteCounter, ref currDecompPos, ref ctrlByte))
                    {
                        outData[outIndex++] = input[currDecompPos++];
                    }

                    int controlOffset;
                    int controlSize;
                    if (getCtrlBit(input, ref ctrlByteCounter, ref currDecompPos, ref ctrlByte))
                    {
                        if (currDecompPos < input.Length)
                        {
                            int data0 = input[currDecompPos++];
                            int data1 = input[currDecompPos++];
                            if (data0 != 0 || data1 != 0)
                            {
                                controlOffset = (data1 << 5) + (data0 >> 3) - 8192;
                                int sizeTemp = data0 & 7;
                                controlSize = sizeTemp != 0 ? sizeTemp + 2 : input[currDecompPos++] + 10;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        controlSize = 2;
                        if (getCtrlBit(input, ref ctrlByteCounter, ref currDecompPos, ref ctrlByte))
                        {
                            controlSize += 2;
                        }

                        if (getCtrlBit(input, ref ctrlByteCounter, ref currDecompPos, ref ctrlByte))
                        {
                            ++controlSize;
                        }

                        controlOffset = input[currDecompPos++] - 256;
                    }

                    int loadIndex = controlOffset + outIndex;
                    for (int index = 0; index < controlSize && outIndex < outData.Length; ++index)
                    {
                        outData[outIndex++] = outData[loadIndex++];
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ZamboniException(ex);
            }

            return outData;
        }

        public static Memory<byte> Compress(Span<byte> toCompress)
        {
            return PrsCompressor.compress(toCompress);
        }
    }
}
