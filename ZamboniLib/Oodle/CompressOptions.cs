using System;
using System.Runtime.InteropServices;

namespace Zamboni.Oodle
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CompressOptions
    {
        public int unknown_0;
        public int min_match_length;
        public int seek_chunk_reset;
        public int seek_chunk_len;

        public int unknown_1;
        public int dictionary_size;
        public int space_speed_tradeoff_bytes;
        public int unknown_2;

        public int make_qhcrc;
        public int max_local_dictionary_size;
        public int make_long_range_matcher;
        public int hash_bits;

        public static CompressOptions GetCompressOptions(int Unknown_0, int Min_match_length, int Seek_chunk_reset,
            int Seek_chunk_len, int Unknown_1, int Dictionary_size, int Space_speed_tradeoff_bytes, int Unknown_2,
            int Make_qhcrc, int Max_local_dictionary_size, int Make_long_range_matcher, int Hash_bits)
        {
            CompressOptions options = new CompressOptions();
            options.unknown_0 = Unknown_0;
            options.min_match_length = Min_match_length;
            options.seek_chunk_reset = Seek_chunk_reset;
            options.seek_chunk_len = Seek_chunk_len;

            options.unknown_1 = Unknown_1;
            options.dictionary_size = Dictionary_size;
            options.space_speed_tradeoff_bytes = Space_speed_tradeoff_bytes;
            options.unknown_2 = Unknown_2;

            options.make_qhcrc = Make_qhcrc;
            options.max_local_dictionary_size = Max_local_dictionary_size;
            options.make_long_range_matcher = Make_long_range_matcher;
            options.hash_bits = Hash_bits;

            return options;
        }
    }
}
