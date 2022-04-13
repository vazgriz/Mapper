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
using System.Windows;

namespace Mapper {
    class Generator {
        MainWindow mainWindow;
        GridControl gridControl;
        AppSettings appSettings;
        GridSettings gridSettings;

        ProgressWindow progressWindow;

        public Generator(MainWindow mainWindow, GridControl gridControl) {
            if (mainWindow == null) throw new ArgumentNullException(nameof(mainWindow));
            if (gridControl == null) throw new ArgumentNullException(nameof(gridControl));

            this.mainWindow = mainWindow;
            appSettings = mainWindow.AppSettings;
            this.gridControl = gridControl;
            gridSettings = gridControl.GridSettings;
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

            progressWindow = gridControl.BeginGenerating(GetSteps(tileCount));

            (var heightData, var waterData) = await GetMapImageData(outputSize, tileCount, zoom, xOffset, yOffset, x1, y1);

            progressWindow.SetText("Processing tiles");
            var normalizedHeightData = await Task.Run(() => {
                return GetNormalizedHeightData(heightData);
            });
            mainWindow.DebugHeightmap(normalizedHeightData);
            progressWindow.Increment();

            gridControl.FinishGenerating();
        }

        int GetSteps(int tileCount) {
            int totalTileCount = 2 * tileCount * tileCount;
            int processing = 1;
            return totalTileCount + processing;
        }

        async Task<(Image, Image)> GetMapImageData(int outputSize, int tileCount, int zoom, int xOffset, int yOffset, int x1, int y1) {
            var tiles = new List<PngBitmapDecoder>();
            var vectorTiles = new List<VectorTile>();

            progressWindow.SetText("Downloading tiles");

            using (var client = new WebClient()) {
                for (int i = 0; i < tileCount; i++) {
                    for (int j = 0; j < tileCount; j++) {
                        await DownloadTile(client, tiles, zoom, x1 + j, y1 + i);
                        progressWindow.Increment();
                        await DownloadVectorTile(client, vectorTiles, zoom, x1 + j, y1 + i);
                        progressWindow.Increment();
                    }
                }
            }

            var heightData = CropHeightData(tiles, tileCount, outputSize, xOffset, yOffset);

            mainWindow.DebugTiles(tiles, tileCount);
            var canvasWindow = mainWindow.DrawVectorTiles(vectorTiles, tileCount);

            try {
                var canvas = canvasWindow.Canvas;
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)canvas.ActualWidth,
                    (int)canvas.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(canvas);

                var crop = new CroppedBitmap(rtb, new Int32Rect(xOffset, yOffset, outputSize, outputSize));

                var waterData = GetWaterData(crop, outputSize);

                return (heightData, waterData);
            }
            finally {
                canvasWindow.Close();
            }
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

        float GetHeightData(int r, int g, int b) {
            return -10000 + ((r * 256 * 256 + g * 256 + b) * 0.1f);
        }

        float GetPixel(List<byte[]> tiles, int tileCount, int x, int y) {
            int tileX = x / 512;
            int tileY = y / 512;
            int tileLocalX = x % 512;
            int tileLocalY = y % 512;

            int tileIndex = tileX + tileY * tileCount;
            var tile = tiles[tileIndex];

            if (tile == null) return 0;

            int tileLocalIndex = (tileLocalX + tileLocalY * 512) * 4;
            return GetHeightData(tile[tileLocalIndex + 2], tile[tileLocalIndex + 1], tile[tileLocalIndex + 0]);
        }

        Image CropHeightData(List<PngBitmapDecoder> tiles, int tileCount, int outputSize, int xOffset, int yOffset) {
            Image image = new Image(outputSize, outputSize);

            var bitmapTiles = new List<byte[]>();
            foreach (var tile in tiles) {
                if (tile == null) {
                    bitmapTiles.Add(null);
                    continue;
                }

                var frame = tile.Frames[0];
                var bitmap = new byte[frame.PixelWidth * frame.PixelHeight * 4];
                frame.CopyPixels(bitmap, frame.PixelWidth * 4, 0);
                bitmapTiles.Add(bitmap);
            }

            foreach (var point in image) {
                var data = GetPixel(bitmapTiles, tileCount, point.x + xOffset, point.y + yOffset);
                image[point] = data;
            }

            return image;
        }

        float GetWaterPixel(byte[] bitmap, int outputSize, int channels, int x, int y) {
            int index = (x + y * outputSize) * channels;

            int data = 0;
            data += bitmap[index + 0];
            data += bitmap[index + 1];
            data += bitmap[index + 2];

            return data / 768f; //average of 3 pixels, max 256 each
        }

        Image GetWaterData(CroppedBitmap bitmap, int outputSize) {
            Image image = new Image(outputSize, outputSize);

            int channels = bitmap.Format.BitsPerPixel / 8;
            byte[] bitmapBytes = new byte[outputSize * outputSize * channels];

            bitmap.CopyPixels(bitmapBytes, outputSize * channels, 0);

            foreach (var point in image) {
                image[point] = GetWaterPixel(bitmapBytes, outputSize, channels, point.x, point.y);
            }

            return image;
        }

        Image GetNormalizedHeightData(Image heightData) {
            float max = float.NegativeInfinity;
            float min = float.PositiveInfinity;

            foreach (var point in heightData) {
                var height = heightData[point];
                if (height < min) {
                    min = height;
                }

                if (height > max) {
                    max = height;
                }
            }

            Image newHeightData = new Image(heightData.Width, heightData.Height);

            if (min == max) {
                foreach (var point in newHeightData) {
                    newHeightData[point] = 0.5f;
                }
            } else {
                foreach (var point in heightData) {
                    var height = heightData[point];
                    var t = InverseLerp(min, max, height);
                    newHeightData[point] = (float)t;
                }
            }

            return newHeightData;
        }
    }
}
