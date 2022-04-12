using MapboxNetCore;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Mapbox.VectorTile;

namespace Mapper {
    class Generator {
        MainWindow mainWindow;
        AppSettings appSettings;
        GridSettings gridSettings;

        public Generator(MainWindow mainWindow, AppSettings appSettings, GridSettings gridSettings) {
            if (mainWindow == null) throw new ArgumentNullException(nameof(mainWindow));
            if (appSettings == null) throw new ArgumentNullException(nameof(appSettings));
            if (gridSettings == null) throw new ArgumentNullException(nameof(gridSettings));

            this.mainWindow = mainWindow;
            this.appSettings = appSettings;
            this.gridSettings = gridSettings;
        }

        double Clamp(double value, double min, double max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }

        double Lerp(double a, double b, double t) {
            return a + (b - a) * Clamp(t, 0, 1);
        }

        double InverseLerp(double a, double b, double value) {
            return Clamp((value - a) / (b - a), 0, 1);
        }

        public async void Run(GeoExtent extent) {
            int outputSize = gridSettings.OutputSize + 1;
            double pixelDensity = gridSettings.GridSize * 1000 / outputSize;    //calculate meters/pixel
            int zoom = TileHelper.GetZoomLevel(pixelDensity, gridSettings.CoordinateY);

            var x1 = TileHelper.LongitudeToTile(extent.TopLeft.Longitude, zoom);
            var y1 = TileHelper.LatitudeToTile(extent.TopLeft.Latitude, zoom);
            var x2 = TileHelper.LongitudeToTile(extent.BottomRight.Longitude, zoom);
            var y2 = TileHelper.LatitudeToTile(extent.BottomRight.Latitude, zoom);

            var tileCount = Math.Max(x2 - x1 + 1, y2 - y1 + 1);

            var tileLng1 = TileHelper.TileToLongitude(x1, zoom);
            var tileLat1 = TileHelper.TileToLatitude(y1, zoom);

            var tileLng2 = TileHelper.TileToLongitude(x1 + tileCount, zoom);
            var tileLat2 = TileHelper.TileToLatitude(y1 + tileCount, zoom);

            double xStart = InverseLerp(tileLng1, tileLng2, extent.TopLeft.Longitude);
            double yStart = InverseLerp(tileLat1, tileLat2, extent.TopLeft.Latitude);

            int xOffset = (int)Math.Round(xStart * tileCount * 512);
            int yOffset = (int)Math.Round(yStart * tileCount * 512);

            var tiles = new List<PngBitmapDecoder>();
            var vectorTiles = new List<VectorTile>();

            using (var client = new WebClient()) {
                for (int i = 0; i < tileCount; i++) {
                    for (int j = 0; j < tileCount; j++) {
                        await DownloadTile(client, tiles, zoom, x1 + j, y1 + i);
                        await DownloadVectorTile(client, vectorTiles, zoom, x1 + j, y1 + i);
                    }
                }
            }

            mainWindow.DebugTiles(tiles, tileCount);
        }

        async Task DownloadTile(WebClient client, List<PngBitmapDecoder> tiles, int zoom, int tileX, int tileY) {
            string url = string.Format("https://api.mapbox.com/v4/mapbox.terrain-rgb/{0}/{1}/{2}@2x.pngraw?access_token={3}", zoom, tileX, tileY, appSettings.APIKey);
            try {
                byte[] response = await client.DownloadDataTaskAsync(url);
                MemoryStream stream = new MemoryStream(response);
                var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                tiles.Add(png);
            }
            catch (WebException ex) {
                var errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse.StatusCode != HttpStatusCode.NotFound) {
                    throw;
                }
                tiles.Add(null);
            }
        }

        async Task DownloadVectorTile(WebClient client, List<VectorTile> tiles, int zoom, int tileX, int tileY) {
            string url = string.Format("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/{0}/{1}/{2}.vector.pbf?access_token={3}", zoom, tileX, tileY, appSettings.APIKey);
            try {
                byte[] response = await client.DownloadDataTaskAsync(url);
                using (MemoryStream raw = new MemoryStream(response))
                using (GZipStream decompressor = new GZipStream(raw, CompressionMode.Decompress))
                using (MemoryStream data = new MemoryStream()) {
                    decompressor.CopyTo(data);
                    var layers = new VectorTile(data.ToArray());
                    tiles.Add(layers);
                }
            }
            catch (WebException ex) {
                var errorResponse = ex.Response as HttpWebResponse;
                if (errorResponse.StatusCode != HttpStatusCode.NotFound) {
                    throw;
                }
                tiles.Add(null);
            }
        }
    }
}
