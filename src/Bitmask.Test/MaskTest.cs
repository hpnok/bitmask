using System.Collections;
using FluentAssertions;

namespace Bitmask.Test
{
    public class MaskTest
    {
        private const int WIDTH = 8;
        private const int HEIGHT = 16;

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(WIDTH, 0)]
        [InlineData(0, HEIGHT)]
        [InlineData(WIDTH + 1000, HEIGHT + 1000)]
        public void MaskIsNotSetOutsideShape(int x, int y)
        {
            var mask = new Mask(WIDTH, HEIGHT);
            mask.Fill();

            var isSet = mask.IsSet(x, y);

            isSet.Should().BeFalse();
        }

        [Fact]
        public void EmptyMaskIsNotSetAnywhere()
        {
            var mask = new Mask(WIDTH, HEIGHT);
            var isSet = false;

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    isSet |= mask.IsSet(x, y);
                }
            }

            isSet.Should().BeFalse();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(WIDTH - 1, HEIGHT - 1)]
        public void MaskIsSet(int x, int y)
        {
            var mask = new Mask(WIDTH, HEIGHT);
            mask.SetAt(x, y);

            var isSet = mask.IsSet(x, y);

            isSet.Should().BeTrue();
        }

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
        [InlineData(72, HEIGHT / 2, 64 + 32)]
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

        [Theory]
        [InlineData(-10, HEIGHT/2, -5, HEIGHT/2)]
        [InlineData(WIDTH, 8, WIDTH, 1)]
        [InlineData(0, -1, 5, -1)]
        [InlineData(0, HEIGHT, 5, HEIGHT)]
        [InlineData(-1, 0, 0, -1)]
        [InlineData(WIDTH - 1, HEIGHT, WIDTH, HEIGHT - 1)]
        [InlineData(WIDTH - 1, -1, WIDTH + 1, 1)]
        public void RayMissesRect(int startX, int startY, int endX, int endY)
        {
            var mask = new Mask(WIDTH, HEIGHT);
            mask.Fill();

            var overlaps = mask.OverlapsRay(startX, startY, endX, endY);
            var commutative = mask.OverlapsRay(endX, endY, startX, startY);

            commutative.Should().BeEquivalentTo(overlaps);
            overlaps.Should().BeNull();
        }

        [Theory]
        [InlineData(-1, 0, 0, 0, 0, 0)]
        [InlineData(0, -1, 0, 0, 0, 0)]
        [InlineData(-10000, HEIGHT - 1, 10000, HEIGHT - 1, 0, HEIGHT - 1)]
        [InlineData(WIDTH + 10000, HEIGHT/2, 0, HEIGHT/2, WIDTH - 1, HEIGHT/2)]
        [InlineData(WIDTH/2, HEIGHT + 10000, WIDTH/2, -10000, WIDTH/2, HEIGHT - 1)]
        [InlineData(2*WIDTH - 1, 2*HEIGHT - 1, 0, 0, WIDTH - 1, HEIGHT - 1)]
        [InlineData(-3, 0, 9, 4, 0, 1)]
        [InlineData(0, HEIGHT + 1, 2, HEIGHT - 3, 1, HEIGHT - 1)]
        public void RayFromOutsideTouchesRect(int startX, int startY, int endX, int endY, int expectedX, int expectedY)
        {
            var mask = new Mask(WIDTH, HEIGHT);
            mask.Fill();

            var overlaps = mask.OverlapsRay(startX, startY, endX, endY);

            overlaps.Should().BeEquivalentTo(new Tuple<int, int>(expectedX, expectedY));
        }

        [Theory]
        [InlineData(-200, 50, 200, 50)]
        [InlineData(400, 50, -200, 50)]
        [InlineData(50, 200, 50, -200)]
        [InlineData(0, 0, 200, 100)]
        [InlineData(0, 400, 100, -100)]
        public void RayDoesNotIntersectEmptyMask(int startX, int startY, int endX, int endY)
        {
            var mask = new Mask(160, 160);

            var overlaps = mask.OverlapsRay(startX, startY, endX, endY);
            var commutative = mask.OverlapsRay(endX, endY, startX, startY);

            commutative.Should().BeEquivalentTo(overlaps);
            overlaps.Should().BeNull();
        }

        [Theory]
        [InlineData(200, 0, 0, 0, 174, 0)]
        [InlineData(0, 0, 200, 0, 74, 0)]
        public void HorizonatalRayIntersectsStructureInMask(int startX, int startY, int endX, int endY, int expectedX, int expectedY)
        {
            var mask = new Mask(200, 1);
            for (int x = 0; x < 200; x++)
            {
                if ((x >=74 && x <= 138) || (x >= 152 && x <= 174)){
                    mask.SetAt(x, 0);
                }
            }

            var overlaps = mask.OverlapsRay(startX, startY, endX, endY);

            overlaps.Should().BeEquivalentTo(new Tuple<int, int>(expectedX, expectedY));
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
