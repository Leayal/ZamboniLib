using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Zamboni.Cryptography
{
    public readonly struct BlewFishBlock : IReadOnlyList<uint>, IEquatable<BlewFishBlock>
    {
        public readonly uint Left, Right;

        public readonly int Count => 2;

        public readonly uint this[int index] => index switch
        {
            0 => this.Left,
            1 => this.Right,
            _ => throw new IndexOutOfRangeException()
        };

        public BlewFishBlock(uint left, uint right)
        {
            this.Left = left;
            this.Right = right;
        }

        struct BlewFishBlockWalker : IEnumerator<uint>
        {
            private int index;
            private readonly BlewFishBlock block;

            public uint Current => this.block[this.index];

            object IEnumerator.Current => this.block[this.index];

            public BlewFishBlockWalker(in BlewFishBlock block)
            {
                this.block = block;
                this.index = -1;
            }

            public bool MoveNext() => (++this.index < 2);

            public void Reset()
            {
                this.index = -1;
            }

            public void Dispose() { }
        }

        public readonly IEnumerator<uint> GetEnumerator() => new BlewFishBlockWalker(in this);

        readonly IEnumerator IEnumerable.GetEnumerator() => new BlewFishBlockWalker(in this);

        public override readonly bool Equals(object? other)
        {
            if (other is BlewFishBlock block)
            {
                return this.Equals(block);
            }
            return false;
        }

        public override readonly int GetHashCode() => HashCode.Combine(this.Left, this.Right);

        public override readonly string ToString() => $"{{ Left: {this.Left}, Right: {this.Right} }}";

        public readonly bool Equals(BlewFishBlock other) => this.Left.Equals(other.Left) && this.Right.Equals(other.Right);

        public readonly (uint, uint) ToTuple() => new ValueTuple<uint, uint>(this.Left, this.Right);
    }
}
