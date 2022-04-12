using Xunit;
using Mapper;

namespace MapperTest {
    public class TileZoomHelperTest {
        [Fact]
        public void Test() {
            Assert.True(TileHelper.GetZoomLevel(16, 0) == 13);
        }
    }
}