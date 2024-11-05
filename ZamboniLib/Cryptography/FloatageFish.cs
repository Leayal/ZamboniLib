using System;

namespace Zamboni.Cryptography
{
    public static class FloatageFish
    {
        public static byte[] decrypt_block(byte[] data_block, int length, uint key)
            => decrypt_block(data_block, length, key, 16);

        public static byte[] decrypt_block(byte[] data_block, int length, uint key, int shift)
        {
            /*
            byte xor_byte = (byte)((( key >> 16 ) ^ key) & 0xFF);
            byte[] to_return = new byte[length];

	        for ( uint i = 0; i < length; ++i )
            {
                if (data_block[i] != 0 && data_block[i] != xor_byte)
                    to_return[i] = (byte)(data_block[i] ^ xor_byte);
                else
                    to_return[i] = data_block[i];
            }*/
            return decrypt_block(new ReadOnlySpan<byte>(data_block, 0, length), key, shift);
        }

        public static byte[] decrypt_block(ReadOnlySpan<byte> data_block, uint key, int shift)
        {
            byte[] to_return = new byte[data_block.Length];
            decrypt_block_to(data_block, key, shift, to_return);
            return to_return;
        }

        public static void decrypt_block_to(ReadOnlySpan<byte> data_block, uint key, Span<byte> outputBuffer)
            => decrypt_block_to(data_block, key, 16, outputBuffer);

        public static void decrypt_block_to(ReadOnlySpan<byte> data_block, uint key, int shift, Span<byte> outputBuffer)
        {
            var length = data_block.Length;
            if (outputBuffer.Length < length) throw new ArgumentException(nameof(outputBuffer));

            byte xor_byte = (byte)((key >> shift ^ key) & 0xFF);

            for (int i = 0; i < length; ++i)
            {
                if (data_block[i] != 0 && data_block[i] != xor_byte)
                {
                    outputBuffer[i] = (byte)(data_block[i] ^ xor_byte);
                }
                else
                {
                    outputBuffer[i] = data_block[i];
                }
            }
        }
    }
}
