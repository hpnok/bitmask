using System.Collections;
using FluentAssertions;

namespace Bitmask.Test
{
    public class MaskTest
    {
        private const int WIDTH = 8;
        private const int HEIGHT = 16;
        
        [Theory]
        [InlineData(-WIDTH, 0)]
        [InlineData(0, -HEIGHT)]
        [InlineData(WIDTH, 0)]
        [InlineData(0, HEIGHT)]
        public void DoesNotOverlapRectOutsideOfMask(int x, int y)
        {
            var mask = new Mask(WIDTH, HEIGHT);
            mask.Fill();

            var overlaps = mask.OverlapsRect(x, y, WIDTH, HEIGHT);

            overlaps.Should().BeFalse();
        }

        [Theory]
        [InlineData(0, 0, WIDTH)]
        [InlineData(72, HEIGHT/2, 64 + 32)]
        public void PixelMaskOverlapsRect(int dotPositionX, int dotPositionY, int width)
        {
            var mask = new Mask(width, HEIGHT);
            mask.SetAt(dotPositionX, dotPositionY);

            var overlaps = mask.OverlapsRect(0, 0, width, HEIGHT);

            overlaps.Should().BeTrue();
        }

        [Fact]
        public void RectDoesNotOverlapHoleInMask()
        {
            var boxMask = BoxMask(64 + 32, 3);

            var overlaps = boxMask.OverlapsRect(1, 1, boxMask.Width - 2, boxMask.Height - 2);

            overlaps.Should().BeFalse();
        }

        [Theory]
        [InlineData(-5, -5)]
        [InlineData(64 + 32 - 5, -5)]
        [InlineData(64 + 32 - 5, 5)]
        [InlineData(-5, 5)]
        public void OverflowingRectOverlapsHoleInMask(int x, int y)
        {
            var boxMask = BoxMask(64 + 32, 10);

            var overlaps = boxMask.OverlapsRect(x, y, 10, 10);

            overlaps.Should().BeTrue();
        }

        private static Mask BoxMask(int width, int height)
        {
            var mask = new Mask(width, height);
            foreach (int x in new int[] { 0, mask.Width - 1 })
            {
                for (int y = 0; y < mask.Height; y++)
                {
                    mask.SetAt(x, y);
                }
            }
            foreach (int y in new int[] { 0, mask.Height - 1 })
            {
                for (int x = 1; x < mask.Width - 1; x++)
                {
                    mask.SetAt(x, y);
                }
            }
            return mask;
        }
    }
}
