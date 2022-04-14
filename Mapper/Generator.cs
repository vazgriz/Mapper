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
using System.Net.Http;
using System.Threading;

namespace Mapper {
    class Generator {
        MainWindow mainWindow;
        GridControl gridControl;
        AppSettings appSettings;
        GridSettings gridSettings;

        ProgressWindow progressWindow;

        public const int tileSize = 512;
        const int concurrentDownloads = 8;
        const int maxZoom = 14; //mapbox does not provide zoom levels above 14 (about 5 m / px)

        public Generator(MainWindow mainWindow, GridControl gridControl) {
            if (mainWindow == null) throw new ArgumentNullException(nameof(mainWindow));
            if (gridControl == null) throw new ArgumentNullException(nameof(gridControl));

            this.mainWindow = mainWindow;
            appSettings = mainWindow.AppSettings;
            this.gridControl = gridControl;
            gridSettings = gridControl.GridSettings;

            ServicePointManager.DefaultConnectionLimit = concurrentDownloads;
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

        (int, int, int, int, int) GetTileCount(GeoExtent extent, int zoom) {
            var x1 = TileHelper.LongitudeToTile(extent.TopLeft.Longitude, zoom);
            var y1 = TileHelper.LatitudeToTile(extent.TopLeft.Latitude, zoom);
            var x2 = TileHelper.LongitudeToTile(extent.BottomRight.Longitude, zoom);
            var y2 = TileHelper.LatitudeToTile(extent.BottomRight.Latitude, zoom);

            return (Math.Max(x2 - x1 + 1, y2 - y1 + 1), x1, y1, x2, y2);
        }

        public async void Run(GeoExtent extent) {
            int outputSize = gridSettings.OutputSize + 1;
            double pixelDensity = gridSettings.GridSize * 1000 / outputSize;    //calculate meters/pixel
            int zoomNominal = TileHelper.GetZoomLevel(pixelDensity, gridSettings.CoordinateY);

            int zoomReal = Math.Min(zoomNominal, maxZoom);

            (var tileCountNominal, _, _, _, _) = GetTileCount(extent, zoomNominal);
            (var tileCountReal, var x1, var y1, var x2, var y2) = GetTileCount(extent, zoomReal);

            var tileLng1 = TileHelper.TileToLongitude(x1, zoomReal);
            var tileLat1 = TileHelper.TileToLatitude(y1, zoomReal);

            var tileLng2 = TileHelper.TileToLongitude(x1 + tileCountReal, zoomReal);
            var tileLat2 = TileHelper.TileToLatitude(y1 + tileCountReal, zoomReal);

            double xOffset = InverseLerp(tileLng1, tileLng2, extent.TopLeft.Longitude);
            double yOffset = InverseLerp(tileLat1, tileLat2, extent.TopLeft.Latitude);

            progressWindow = gridControl.BeginGenerating(GetSteps(tileCountReal));

            (var heightData, var waterData) = await GetMapImageData(tileCountReal, zoomReal, x1, y1);

            progressWindow.SetText("Processing tiles");
            var normalizedHeightData = await Task.Run(() => {
                var crop = CropHeightData(heightData, tileCountNominal, outputSize, xOffset, yOffset);
                return GetNormalizedHeightData(heightData);
            });
            mainWindow.DebugHeightmap(normalizedHeightData);
            progressWindow.Increment();

            gridControl.FinishGenerating();
        }

        int GetSteps(int tileCount) {
            int totalTileCount = 2 * tileCount * tileCount;
            int processing = 3;
            return totalTileCount + processing;
        }

        async Task<(Image, Image)> GetMapImageData(int tileCountReal, int zoom, int x1, int y1) {
            var tiles = new List<PngBitmapDecoder>();
            var vectorTiles = new List<VectorTile>();

            progressWindow.SetText("Downloading tiles");

            var semaphore = new SemaphoreSlim(concurrentDownloads);

            using (var client = new HttpClient()) {
                for (int i = 0; i < tileCountReal; i++) {
                    for (int j = 0; j < tileCountReal; j++) {
                        await GetTile(semaphore, client, tiles, zoom, x1 + j, y1 + i);
                        progressWindow.Increment();
                        await GetVectorTile(semaphore, client, vectorTiles, zoom, x1 + j, y1 + i);
                        progressWindow.Increment();
                    }
                }
            }

            var heightData = CombineTiles(tiles, tileCountReal);
            progressWindow.Increment();

            mainWindow.DebugTiles(tiles, tileCountReal);
            var canvasWindow = mainWindow.DrawVectorTiles(vectorTiles, tileCountReal);

            try {
                var canvas = canvasWindow.Canvas;
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)canvas.ActualWidth,
                    (int)canvas.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(canvas);

                var waterData = GetWaterData(rtb);

                progressWindow.Increment();

                return (heightData, waterData);
            }
            finally {
                canvasWindow.Close();
            }
        }

        async Task GetTile(SemaphoreSlim semaphore, HttpClient client, List<PngBitmapDecoder> tiles, int zoom, int tileX, int tileY) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.terrain-rgb/{0}/{1}/{2}@2x.pngraw", zoom, tileX, tileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);
                TileHelper.WriteCache(mainWindow.CachePath, name, data);
            }

            if (data == null) {
                tiles.Add(null);
            } else {
                MemoryStream stream = new MemoryStream(data);
                var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                tiles.Add(png);
            }
        }

        async Task GetVectorTile(SemaphoreSlim semaphore, HttpClient client, List<VectorTile> tiles, int zoom, int tileX, int tileY) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/{0}/{1}/{2}.vector.pbf", zoom, tileX, tileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);

                if (data != null) {
                    TileHelper.WriteCache(mainWindow.CachePath, name, data);
                }
            }

            if (data == null) {
                tiles.Add(null);
            } else {
                using (MemoryStream raw = new MemoryStream(data))
                using (GZipStream decompressor = new GZipStream(raw, CompressionMode.Decompress))
                using (MemoryStream decompressed = new MemoryStream()) {
                    decompressor.CopyTo(decompressed);
                    var layers = new VectorTile(decompressed.ToArray());
                    tiles.Add(layers);
                }
            }
        }

        async Task<byte[]> DownloadTile(SemaphoreSlim semaphore, HttpClient client, string name) {
            try {
                await semaphore.WaitAsync().ConfigureAwait(false);
                string url = string.Format("{0}?access_token={1}", name, appSettings.APIKey);
                try {
                    byte[] response = await client.GetByteArrayAsync(url);
                    return response;
                }
                catch (HttpRequestException ex) {
                    if (!ex.Message.Contains("404")) {
                        throw;
                    }
                    return null;
                }
            }
            finally {
                semaphore.Release();
            }
        }

        float GetHeightData(int r, int g, int b) {
            return -10000 + ((r * 256 * 256 + g * 256 + b) * 0.1f);
        }

        float GetPixel(List<byte[]> tiles, int tileCount, int x, int y) {
            int tileX = x / tileSize;
            int tileY = y / tileSize;
            int tileLocalX = x % tileSize;
            int tileLocalY = y % tileSize;

            int tileIndex = tileX + tileY * tileCount;
            var tile = tiles[tileIndex];

            if (tile == null) return 0;

            int tileLocalIndex = (tileLocalX + tileLocalY * tileSize) * 4;
            return GetHeightData(tile[tileLocalIndex + 2], tile[tileLocalIndex + 1], tile[tileLocalIndex + 0]);
        }

        Image CombineTiles(List<PngBitmapDecoder> tiles, int tileCount) {
            int size = tileCount * tileSize;
            Image image = new Image(size, size);

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
                image[point] = GetPixel(bitmapTiles, tileCount, point.x, point.y);
            }

            return image;
        }

        Image CropHeightData(Image rawHeightImage, int tileCountNominal, int outputSize, double xOffset, double yOffset) {
            Sampler sampler = new Sampler(rawHeightImage);
            Image image = new Image(outputSize, outputSize);

            int nominalSize = tileCountNominal * tileSize;

            foreach (var point in image) {
                Point pos = new Point(
                    (point.x / (double)nominalSize) + xOffset,
                    (point.y / (double)nominalSize) + yOffset
                );
                image[point] = sampler.Sample(pos);
            }

            return image;
        }

        float GetWaterPixel(byte[] bitmap, int size, int channels, int x, int y) {
            int index = (x + y * size) * channels;

            int data = 0;
            data += bitmap[index + 0];
            data += bitmap[index + 1];
            data += bitmap[index + 2];

            return data / 768f; //average of 3 pixels, max 256 each
        }

        Image GetWaterData(RenderTargetBitmap bitmap) {
            Image image = new Image(bitmap.PixelWidth, bitmap.PixelHeight);

            int channels = bitmap.Format.BitsPerPixel / 8;
            byte[] bitmapBytes = new byte[bitmap.PixelWidth * bitmap.PixelHeight * channels];

            bitmap.CopyPixels(bitmapBytes, bitmap.PixelWidth * channels, 0);

            foreach (var point in image) {
                image[point] = GetWaterPixel(bitmapBytes, bitmap.PixelWidth, channels, point.x, point.y);
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
