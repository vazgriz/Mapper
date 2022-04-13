using Xunit;
using Mapper;

namespace MapperTest {
    public class TileZoomHelperTest {
        [Fact]
        public void Test() {
            Assert.True(TileHelper.GetZoomLevel(16, 0) == 13);
        }
    }

    public class ImageTest {
        [Fact]
        public void IndexTest() {
            Image image = new Image(123, 456);

            for (int i = 0; i < image.Data.Length; i++) {
                image.Data[i] = i;
            }

            int count = 0;
            foreach (var point in image) {
                Assert.True(image[point] == count);
                count++;
            }

            count = 0;
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Height; x++) {
                    Assert.True(image[new PointInt(x, y)] == count);
                    count++;
                }
            }
        }
    }
}