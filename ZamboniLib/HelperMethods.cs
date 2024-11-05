using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Zamboni
{
    static class HelperMethods
    {
        /// <summary>Reads a block of bytes and returns when the <paramref name="buffer"/> is filled or when the end of the stream is reached.</summary>
        /// <param name="myself">The binary reader.</param>
        /// <param name="buffer">The buffer to fill the required number of bytes.</param>
        /// <returns>The number of bytes which have been read so far. This may be less than the buffer's size in case the stream's end is reached prematurely.</returns>
        public static int ReadRequiredBlock(this BinaryReader myself, Span<byte> buffer)
        {
            int requiredSize = buffer.Length,
                read, pos = 0;
            while (pos < requiredSize)
            {
                read = myself.Read(buffer.Slice(pos));
                if (read == 0) break;
                pos += read;
            }
            return pos;
        }
    }
}
