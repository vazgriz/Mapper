using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapper {
    public static class TileHelper {
        public const int cancellationCheckInterval = 1024;

        //find best zoom level given size of map and output resolution
        //data from https://docs.mapbox.com/help/glossary/zoom-level/

        static List<List<double>> lookUpTable = new List<List<double>> {
            //latitude 0
            new List<double> {
                78271.484,  //0
                39135.742,
                19567.871,
                9783.936,
                4891.968,
                2445.984,   //5
                1222.992,
                611.496,
                305.748,
                152.874,
                76.437,     //10
                38.218,
                19.109,
                9.555,
                4.777,
                2.389,      //15
                1.194,
                0.597,
                0.299,
                0.149,
                0.075,      //20
                0.037,
                0.019
            },

            //latitude 20
            new List<double> {
                73551.136,  //0
                36775.568,
                18387.784,
                9193.892,
                4596.946,
                2298.473,   //5
                1149.237,
                574.618,
                287.309,
                143.655,
                71.827,     //10
                35.914,
                17.957,
                8.978,
                4.489,
                2.245,      //15
                1.122,
                0.561,
                0.281,
                0.140,
                0.070,      //20
                0.035,
                0.018
            },

            //latitude 40
            new List<double> {
                59959.436,  //0
                29979.718,
                14989.859,
                7494.929,
                3747.465,
                1873.732,   //5
                936.866,
                468.433,
                234.217,
                117.108,
                58.554,     //10
                29.277,
                14.639,
                7.319,
                3.660,
                1.830,      //15
                0.915,
                0.457,
                0.229,
                0.114,
                0.057,      //20
                0.029,
                0.014
            },

            //latitude 60
            new List<double> {
                39135.742,  //0
                19567.871,
                9783.936,
                4891.968,
                2445.984,
                1222.992,   //5
                611.496,
                305.748,
                152.874,
                76.437,
                38.218,     //10
                19.109,
                9.555,
                4.777,
                2.389,
                1.194,      //15
                0.597,
                0.299,
                0.149,
                0.075,
                0.037,      //20
                0.019,
                0.009
            },

            //latitude 80
            new List<double> {
                13591.701,  //0
                6795.850,
                3397.925,
                1698.963,
                849.481,
                424.741,    //5
                212.370,
                106.185,
                53.093,
                26.546,
                13.273,     //10
                6.637,
                3.318,
                1.659,
                0.830,
                0.415,      //15
                0.207,
                0.104,
                0.052,
                0.026,
                0.013,      //20
                0.006,
                0.003
            },
        };

        public static int GetZoomLevel(double pixelDensity, double latitude) {
            latitude = Math.Abs(Math.Min(90, latitude));
            int latitudeIndex = (int)Math.Round(latitude / 20);

            var list = lookUpTable[latitudeIndex];

            for (int i = 0; i < list.Count; i++) {
                if (list[i] < pixelDensity) {
                    return i;
                }
            }

            return list.Count - 1;  //return highest zoom level
        }

        public static double GetPixelDensity(int zoom, double latitude) {
            latitude = Math.Abs(Math.Min(90, latitude));
            int latitudeIndex = (int)Math.Round(latitude / 20);
            var list = lookUpTable[latitudeIndex];
            return list[zoom];
        }

        public static int LongitudeToTile(double lng, int zoom) {
            return (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2, zoom));
        }

        public static int LatitudeToTile(double lat, int zoom) {
            return (int)Math.Floor((1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom));
        }

        public static double TileToLongitude(int tileX, int zoom) {
            return tileX / Math.Pow(2, zoom) * 360 - 180;
        }

        public static double TileToLatitude(int tileY, int zoom) {
            var n = Math.PI - 2 * Math.PI * tileY / Math.Pow(2, zoom);
            return 180 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }

        public static void DrawVectorTileToCanvas(Canvas canvas, Mapbox.VectorTile.VectorTileLayer layer, int tileSize, long extent, int x, int y) {
            for (int i = 0; i < layer.FeatureCount(); i++) {
                var feature = layer.GetFeature(i);

                if (feature.GeometryType == Mapbox.VectorTile.Geometry.GeomType.POLYGON) {
                    DrawPolygon(canvas, feature, tileSize, extent, x, y);
                }
            }
        }

        static void DrawPolygon(Canvas canvas, Mapbox.VectorTile.VectorTileFeature feature, int tileSize, double extent, int xOffset, int yOffset) {
            Polygon polygon = null;
            PointCollection points = null;
            double x = 0;
            double y = 0;

            for (int i = 0; i < feature.GeometryCommands.Count; i++) {
                var command = feature.GeometryCommands[i];
                var id = command & 0x7;
                var count = command >> 3;

                if (id == 1) {
                    for (int j = 0; j < count; j++) {
                        var param1 = DecodeParameter(feature.GeometryCommands[i + 1]);
                        var param2 = DecodeParameter(feature.GeometryCommands[i + 2]);
                        i += 2;

                        x += param1 / extent;
                        y += param2 / extent;
                    }

                    polygon = new Polygon();
                    polygon.Fill = Brushes.Black;

                    points = new PointCollection();
                    points.Add(new Point(x * tileSize, y * tileSize));
                } else if (id == 2) {
                    for (int j = 0; j < count; j++) {
                        var param1 = DecodeParameter(feature.GeometryCommands[i + 1]);
                        var param2 = DecodeParameter(feature.GeometryCommands[i + 2]);
                        i += 2;

                        x += param1 / extent;
                        y += param2 / extent;

                        points.Add(new Point(x * tileSize, y * tileSize));
                    }
                } else if (id == 7) {
                    polygon.Points = points;

                    canvas.Children.Add(polygon);
                    Canvas.SetTop(polygon, xOffset);
                    Canvas.SetLeft(polygon, yOffset);
                }
            }
        }

        static int DecodeParameter(uint value) {
            return (int)((value >> 1) ^ (-(value & 1)));
        }

        public static async Task<byte[]> TryLoadFromCache(string cachePath, string name, CancellationToken cancellationToken) {
            string filename = System.IO.Path.Combine(cachePath, Base64Encode(name));

            try {
                using (var stream = System.IO.File.OpenRead(filename)) {
                    var result = new byte[stream.Length];
                    await stream.ReadAsync(result, 0, (int)stream.Length, cancellationToken);
                    return result;
                }
            }
            catch (System.IO.FileNotFoundException) {
                return null;
            }
        }

        public static void WriteCache(string cachePath, string name, byte[] data) {
            string filename = System.IO.Path.Combine(cachePath, Base64Encode(name));
            System.IO.File.WriteAllBytes(filename, data);
        }

        static string Base64Encode(string plainText) {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static int GetParallelBatchCount() {
            return Environment.ProcessorCount;
        }

        public static async Task ProcessImageParallel<T>(Image<T> image, Action<int, int, int> action) where T : struct {
            int batchCount = GetParallelBatchCount();
            int batchSize = (image.Height / batchCount) * image.Width;

            List<Task> tasks = new List<Task>(batchCount);

            for (int i = 0; i < batchCount; i++) {
                int batchID = i;
                int batchStart = i * batchSize;
                int batchEnd = batchStart + batchSize;
                if (i == batchCount - 1) batchEnd = image.Data.Length;

                var task = Task.Run(() => {
                    action(batchID, batchStart, batchEnd);
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
    }
}
