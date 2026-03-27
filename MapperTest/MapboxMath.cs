using System;
using System.Collections.Generic;
using Xunit;
using Mapper;

namespace MapperTest {
    public class MapboxMath {
        [Fact]
        public void LatitudeTest() {
            const int zoom = 13;
            double[] values = new double[] { -80, -60, -25.21, -1.317, 0, 1.317, 23.21, 60, 80 };
            int[] tiles = new int[] { 7272, 5813, 4689, 4125, 4096, 4066, 3552, 2378, 919 };

            for (int i = 0; i < values.Length; i++) {
                var lat = values[i];
                var tile = tiles[i];

                int newTile = TileHelper.LatitudeToTile(lat, zoom);
                double newLat = TileHelper.TileToLatitude(newTile, zoom);

                Assert.Equal(tile, newTile);
                Assert.Equal(lat, newLat, 1);
            }
        }

        [Fact]
        public void LatitudeTest2() {
            const int zoomMax = 13;
            double[] rawValues = new double[] { -80, -60, -25.21, -1.317, 0, 1.317, 23.21, 60, 80 };

            for (int i = 0; i < zoomMax; i++) {
                for (int j = 0; j < rawValues.Length; j++) {
                    var zoom = i;
                    var rawLat = rawValues[j];

                    // raw values will be rounded to nearest tile edge
                    int tile1 = TileHelper.LatitudeToTile(rawLat, zoom);
                    double lat1 = TileHelper.TileToLatitude(tile1, zoom);
                    int tile2 = TileHelper.LatitudeToTile(lat1, zoom);
                    double lat2 = TileHelper.TileToLatitude(tile2, zoom);

                    Assert.Equal(tile1, tile2);
                    Assert.Equal(lat1, lat2, 2);
                }
            }
        }

        [Fact]
        public void LongitudeTest() {
            const int zoom = 13;
            double[] values = new double[] { -100, -60, -25.21, -1.317, 0, 1.317, 23.21, 60, 100 };
            int[] tiles = new int[] { 1820, 2730, 3522, 4066, 4096, 4125, 4624, 5461, 6371 };

            for (int i = 0; i < values.Length; i++) {
                var lng = values[i];
                var tile = tiles[i];
                int newTile = TileHelper.LongitudeToTile(lng, zoom);
                double newLng = TileHelper.TileToLongitude(newTile, zoom);

                Assert.Equal(tile, newTile);
                Assert.Equal(lng, newLng, 1);
            }
        }

        [Fact]
        public void LongitudeTest2() {
            const int zoomMax = 13;
            double[] rawValues = new double[] { -100, -60, -25.21, -1.317, 0, 1.317, 23.21, 60, 100 };

            for (int i = 0; i < zoomMax; i++) {
                for (int j = 0; j < rawValues.Length; j++) {
                    var zoom = i;
                    var rawLng = rawValues[j];

                    // raw values will be rounded to nearest tile edge
                    int tile1 = TileHelper.LongitudeToTile(rawLng, zoom);
                    double lng1 = TileHelper.TileToLongitude(tile1, zoom);
                    int tile2 = TileHelper.LongitudeToTile(lng1, zoom);
                    double lng2 = TileHelper.TileToLongitude(tile2, zoom);

                    Assert.Equal(tile1, tile2);
                    Assert.Equal(lng1, lng2, 1);
                }
            }
        }
    }
}
