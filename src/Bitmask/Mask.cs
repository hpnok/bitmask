using System.Collections;
using System.Numerics;

namespace Bitmask
{
    public class Mask
    {
        private uint W { get; }
        private uint H { get; }
        public int Width => (int)W;
        public int Height => (int)H;
        private ulong[] Bits { get; }
        private const uint BITS_PER_STRIP = sizeof(ulong) * 8;
        private const ulong BLOCK_MODULO = BITS_PER_STRIP - 1;  // given N a power of 2, x % N == x & (N-1)
        public Mask(int width, int height)
        {
            W = (uint)width;
            H = (uint)height;
            var long_per_row = (width - 1) / BITS_PER_STRIP + 1;
            Bits = new ulong[height * long_per_row];
        }

        public Mask Fill()
        {
            const ulong full = ~(ulong)0;
            uint numberOfFullStrips = H * ((W - 1) / BITS_PER_STRIP);
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
            Bits[x / BITS_PER_STRIP * H + y] |= 1uL << (int)((ulong)x & BLOCK_MODULO);
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
                    ulong nextStrip = (1 + stripStart / BITS_PER_STRIP) * BITS_PER_STRIP;
                    ulong stripEnd = Math.Min(nextStrip, end);
                    ulong stripWidth = stripEnd - stripStart;
                    ulong shift = stripStart & BLOCK_MODULO;
                    ulong stripMask = ((2uL << (int)(stripWidth - 1)) - 1uL) << (int)shift;
                    ulong* stripBits = bits + H * (stripStart / BITS_PER_STRIP) + y;
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
            return (Bits[x / BITS_PER_STRIP * H + y] & (1uL << (int)((ulong)x & BLOCK_MODULO))) != 0;
        }

        public unsafe Tuple<int, int>? OverlapsRay(int startX, int startY, int endX, int endY)
        {
            if (
                W == 0 || H == 0 ||
                (startX < 0 && endX < 0) || (startX >= W && endX >= W) ||
                (startY < 0 && endY < 0) || (startY >= H && endY >= H))
            {
                return null;
            }
            int dx = endX - startX;
            int dy = endY - startY;
            fixed (ulong* bits = Bits)
            {
                if (dx == 0)
                {
                    if (dy == 0)
                    {
                        return GetAt(startX, startY) ? new Tuple<int, int>(startX, startY) : null;
                    }
                    ulong x = (ulong)startX;
                    long initialY = Math.Clamp(startY, 0, H - 1);
                    long targetY = Math.Clamp(endY, 0, H - 1);
                    ulong shift = x & BLOCK_MODULO;
                    ulong stripMask = 1uL << (int)shift;
                    ulong* stripBits = bits + H * (x / BITS_PER_STRIP);
                    int direction = Math.Sign(dy);
                    for (long y = initialY; y != targetY + direction; y += direction)
                    {
                        if ((*(stripBits + y) & stripMask) != 0)
                        {
                            return new Tuple<int, int>((int)x, (int)y);
                        }
                    }
                }
                else if (dy == 0)
                {
                    long y = startY;
                    long x = Math.Clamp(startX, 0, W - 1);
                    long targetX = Math.Clamp(endX, 0, W - 1);
                    if (dx > 0)
                    {
                        while (x <= targetX)
                        {
                            long nextStrip = (1 + x / BITS_PER_STRIP) * BITS_PER_STRIP;
                            ulong mask = (2uL << (int)(targetX & (long)BLOCK_MODULO)) - (1uL << (int)(x & (long)BLOCK_MODULO));  // mask [x, targetX]
                            ulong maskedStrip = bits[H * (x / BITS_PER_STRIP) + y] & mask;
                            int unsetBits = BitOperations.TrailingZeroCount(maskedStrip);
                            if (unsetBits != BITS_PER_STRIP)
                            {
                                x = BITS_PER_STRIP * (x / BITS_PER_STRIP) + unsetBits;
                                return new Tuple<int, int>((int)x, (int)y);
                            }
                            x = nextStrip;
                        }
                    }
                    else
                    {
                        while (x >= targetX)
                        {
                            long nextStrip = BITS_PER_STRIP * (x / BITS_PER_STRIP) - 1;
                            ulong mask = (2uL << (int)(x & (long)BLOCK_MODULO)) - (1uL << (int)(targetX & (long)BLOCK_MODULO));  // mask [targetX, x]
                            ulong maskedStrip = bits[H * (x / BITS_PER_STRIP) + y] & mask;
                            int emptyBits = BitOperations.LeadingZeroCount(maskedStrip);
                            if (emptyBits != BITS_PER_STRIP)
                            {
                                int firstSetBitFromRight = 1 + emptyBits;
                                x = BITS_PER_STRIP * (x / BITS_PER_STRIP) + (BITS_PER_STRIP - firstSetBitFromRight);
                                return new Tuple<int, int>((int)x, (int)y);
                            }
                            x = nextStrip;
                        }
                    }
                }
                else if (Math.Abs(dx) >= Math.Abs(dy))  // for each x value there's only one f(x)=y value
                {
                    double slope = dy / (double)dx;  // y(x) = s*(x - startX) + startY
                    var initialY = startY + (int)Math.Round(slope * (0 - startX));
                    var finalY = startY + (int)Math.Round(slope * (W - 1 - startX));
                    if ((initialY < 0 && finalY < 0) || (initialY >= H && finalY >= H))
                    {
                        return null;
                    }
                    long sourceX = Math.Clamp(startX, 0, W - 1);
                    long targetX = Math.Clamp(endX, 0, W - 1);
                    int direction = Math.Sign(dx);
                    for (int x = (int)sourceX; x != targetX; x += direction)
                    {
                        int y = (int)Math.Round(slope * (x - startX)) + startY;
                        if (y >= 0 && y < H && GetAt(x, y))
                        {
                            return new Tuple<int, int>(x, y);
                        }
                    }
                }
                else
                {
                    double slope = dx / (double)dy;  // x(y) = s*(y - startY) + startX
                    var initialX = startX + (int)Math.Round(slope * (0 - startY));
                    var finalX = startX + (int)Math.Round(slope * (H - 1 - startY));
                    if ((initialX < 0 && finalX < 0) || (initialX >= W && finalX >= W))
                    {
                        return null;
                    }
                    long sourceY = Math.Clamp(startY, 0, H - 1);
                    long targetY = Math.Clamp(endY, 0, H - 1);
                    int direction = Math.Sign(dy);
                    for (int y = (int)sourceY; y != targetY; y += direction)
                    {
                        int x = (int)Math.Round(slope * (y - startY)) + startX;
                        if (x >= 0 && x < W && GetAt(x, y))
                        {
                            return new Tuple<int, int>(x, y);
                        }
                    }
                }
                return null;
            }
        }
    }
}
