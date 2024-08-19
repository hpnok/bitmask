using System.Collections;

namespace Bitmask
{
    public class Mask
    {
        private uint W { get; }
        private uint H { get; }
        public int Width => (int)W;
        public int Height => (int)H;
        private ulong[] Bits { get; }
        private const uint BITS_PER_LONG = sizeof(ulong) * 8;
        private const ulong BLOCK_MODULO = BITS_PER_LONG - 1;  // given N a power of 2, x % N == x & (N-1)
        public Mask(int width, int height)
        {
            W = (uint)width;
            H = (uint)height;
            var long_per_row = (width - 1) / BITS_PER_LONG + 1;
            Bits = new ulong[height * long_per_row];
        }

        public Mask Fill()
        {
            const ulong full = ~(ulong)0;
            uint numberOfFullStrips = H * ((W - 1) / BITS_PER_LONG);
            for (uint i = 0; i < numberOfFullStrips; i++)
            {
                Bits[i] = full;
            }
            int numberOfStrips = Bits.Length;
            ulong partialStripMask = (~0uL) >> (int)(W & BLOCK_MODULO);
            for (uint i = numberOfFullStrips; i < numberOfStrips; i++)
            {
                Bits[i] = partialStripMask;
            }
            return this;
        }

        public void SetAt(int x, int y)
        {
            Bits[x / BITS_PER_LONG * H + y] |= 1uL << (int)((ulong)x & BLOCK_MODULO);
        }

        public unsafe bool OverlapsRect(int x, int y, int width, int height)
        {
            if ((x >= this.W) ||
                (y >= this.H) ||
                (y + height <= 0) ||
                (x + width <= 0) ||
                (this.H == 0) || (this.W == 0) ||
                (width == 0) || (height == 0))
            {
                return false;
            }
            fixed (ulong* bits = Bits)
            {
                ulong stripStart = (ulong)Math.Max(0, x);
                ulong end = Math.Min((ulong)(x + width), W);
                while (stripStart < end)
                {
                    ulong nextStrip = (1 + stripStart / BITS_PER_LONG) * BITS_PER_LONG;
                    ulong stripEnd = Math.Min(nextStrip, end);
                    ulong stripWidth = stripEnd - stripStart;
                    ulong shift = stripStart & BLOCK_MODULO;
                    ulong stripMask = ((2uL << (int)(stripWidth - 1)) - 1uL) << (int)shift;
                    ulong* stripBits = bits + H * (stripStart / BITS_PER_LONG) + y;
                    ulong* stripBitsEnd = stripBits + Math.Min(H - y, height);
                    for (ulong* sp = stripBits; sp < stripBitsEnd; sp++)
                    {
                        if ((*sp & stripMask) != 0)
                        {
                            return true;
                        }
                    }
                    stripStart = nextStrip;
                }
            }
            return false;
        }

        internal bool GetAt(int x, int y)
        {
            return (Bits[x / BITS_PER_LONG * H + y] & (1uL << (int)((ulong)x & BLOCK_MODULO))) != 0;
        }
    }
}
