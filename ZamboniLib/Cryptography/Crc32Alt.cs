using System;
using System.Collections.Generic;

namespace Zamboni.Cryptography
{
    public class Crc32Alt
    {
        private readonly uint[] _table = new uint[0x100];
        private readonly uint DefaultSeed = 0xFFFFFFFF;

        public Crc32Alt()
        {
            unchecked
            {
                for (uint i = 0; i < 0x100; i++)
                {
                    uint crc = i;
                    for (int j = 0; j < 8; j++)
                    {
                        crc = crc >> 1 ^ ((crc & 1) > 0 ? 0xEDB88320 : 0);
                    }

                    _table[i] = crc;
                }
            }
        }

        public uint GetCrc32(ReadOnlySpan<byte> data, uint crc = 0)
        {
            int start = 0;
            int length = data.Length;
            crc ^= DefaultSeed;

            unchecked
            {
                if (data != null && data.Length > 0)
                {
                    for (; length > 0; length--)
                    {
                        crc = _table[(data[start++] ^ crc) & 0xFF] ^ crc >> 8;
                    }
                }
            }

            return crc ^ DefaultSeed;
        }

        public uint GetCrc32(IEnumerable<ReadOnlyMemory<byte>> datas, uint crc = 0)
        {
            int start = 0;
            crc ^= DefaultSeed;

            unchecked
            {
                foreach(var dataMem in datas)
                {
                    var data = dataMem.Span;
                    if (!data.IsEmpty && data.Length > 0)
                    {
                        int length = data.Length;
                        for (; length > 0; length--)
                        {
                            crc = _table[(data[start++] ^ crc) & 0xFF] ^ crc >> 8;
                        }
                    }
                }
            }

            return crc ^ DefaultSeed;
        }
    }
}
