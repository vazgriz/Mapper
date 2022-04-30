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

        public async void Run(GeoExtent extent) {
            int outputSize = gridSettings.OutputSize + 1;
            double pixelDensity = gridSettings.GridSize * 1000 / outputSize;    //calculate meters/pixel
            int zoom = TileHelper.GetZoomLevel(pixelDensity, gridSettings.CoordinateY);

            zoom = Math.Min(zoom, maxZoom);

            var x1 = TileHelper.LongitudeToTile(extent.TopLeft.Longitude, zoom);
            var y1 = TileHelper.LatitudeToTile(extent.TopLeft.Latitude, zoom);
            var x2 = TileHelper.LongitudeToTile(extent.BottomRight.Longitude, zoom);
            var y2 = TileHelper.LatitudeToTile(extent.BottomRight.Latitude, zoom);

            var rawTileCount = Math.Max(x2 - x1 + 1, y2 - y1 + 1);

            var tileLng1 = TileHelper.TileToLongitude(x1, zoom);
            var tileLat1 = TileHelper.TileToLatitude(y1, zoom);

            var tileLng2 = TileHelper.TileToLongitude(x1 + rawTileCount + 1, zoom);
            var tileLat2 = TileHelper.TileToLatitude(y1 + rawTileCount + 1, zoom);

            double xOffset = Utility.InverseLerp(tileLng1, tileLng2, extent.TopLeft.Longitude);
            double yOffset = Utility.InverseLerp(tileLat1, tileLat2, extent.TopLeft.Latitude);

            double tilePixelDensity = TileHelper.GetPixelDensity(zoom, gridSettings.CoordinateY);
            double tileSizeMeter = rawTileCount * tileSize * tilePixelDensity;
            double sizeRatio = (gridSettings.GridSize * 1000) / tileSizeMeter;

            // image has y=0 at the top, growing down; map has y=0, growing up
            // change the yOffset to map convention
            yOffset = 1 - sizeRatio - yOffset;

            progressWindow = gridControl.BeginGenerating();
            progressWindow.SetText("Downloading tiles");
            progressWindow.SetMaximum(GetDownloadSteps(rawTileCount));
            progressWindow.Reset();

            (var heightDataRaw, var waterDataRaw) = await GetMapImageData(rawTileCount, zoom, x1, y1);
            await Task.Delay(20);

            progressWindow.SetText("Processing tiles");
            progressWindow.SetMaximum(GetProcessSteps(gridSettings.TileCount));
            progressWindow.Reset();

            var heightData = await CropData(heightDataRaw, gridSettings.TileCount, outputSize, sizeRatio, xOffset, yOffset);
            ImageGroup<float> waterData = null;

            if (waterDataRaw != null) {
                waterData = await CropData(waterDataRaw, gridSettings.TileCount, outputSize, sizeRatio, xOffset, yOffset);
                mainWindow.DebugHeightmap(waterData);
            }

            var (heightMin, heightMax) = await GetHeightLimits(heightData);

            Pipeline pipeline = new Pipeline();
            pipeline.NormalizeMin = heightMin;
            pipeline.NormalizeMax = heightMax;
            pipeline.ApplyWaterOffset = gridSettings.ApplyWaterOffset;
            pipeline.WaterOffset = gridSettings.WaterOffset;

            ImageGroup<ushort> output = new ImageGroup<ushort>(heightData.TileCount, heightData.TileSize);

            await pipeline.Process(progressWindow, heightData, output);
            await Task.Delay(20);

            gridControl.FinishGenerating(output);
        }

        int GetDownloadSteps(int rawTileCount) {
            int steps = rawTileCount * rawTileCount;

            if (gridSettings.ApplyWaterOffset) {
                steps *= 2;
            }

            return steps;
        }

        int GetProcessSteps(int tileCount) {
            int totalTileCount = tileCount * tileCount;
            int steps = 1;  //crop heightmap

            if (gridSettings.ApplyWaterOffset) {
                steps++;    //crop watermap
            }

            steps++;    //get height data
            steps++;    //process

            return steps * totalTileCount;
        }

        async Task<(Image<float>, Image<float>)> GetMapImageData(int tileCount, int zoom, int x1, int y1) {
            var tiles = new List<byte[]>(tileCount * tileCount);
            var vectorTiles = new List<VectorTile>(tileCount * tileCount);

            for (int i = 0; i < tileCount * tileCount; i++) {
                tiles.Add(null);
                vectorTiles.Add(null);
            }

            var semaphore = new SemaphoreSlim(concurrentDownloads);
            List<Task> tasks = new List<Task>(tileCount * tileCount);

            using (var client = new HttpClient()) {
                for (int i = 0; i < tileCount; i++) {
                    for (int j = 0; j < tileCount; j++) {
                        int x = j;
                        int y = i;
                        int mapTileX = x1 + j;
                        int mapTileY = y1 + i;
                        var task1 = Task.Run(async () => {
                            await GetTile(semaphore, client, tiles, zoom, tileCount, mapTileX, mapTileY, x, y);
                            progressWindow.Increment();
                        });

                        tasks.Add(task1);

                        if (gridSettings.ApplyWaterOffset) {
                            var task2 = Task.Run(async () => {
                                await GetVectorTile(semaphore, client, vectorTiles, zoom, tileCount, mapTileX, mapTileY, x, y);
                                progressWindow.Increment();
                            });

                            tasks.Add(task2);
                        }
                    }
                }

                await Task.WhenAll(tasks);
            }

            var heightData = await CombineTiles(tiles, tileCount);

            mainWindow.DebugTiles(tiles, tileCount);

            if (!gridSettings.ApplyWaterOffset) {
                return (heightData, null);
            }

            var canvasWindow = mainWindow.DrawVectorTiles(vectorTiles, tileCount);

            try {
                var canvas = canvasWindow.Canvas;
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)canvas.Width,
                    (int)canvas.Height, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(canvas);

                var waterData = GetWaterData(rtb);

                return (heightData, waterData);
            }
            finally {
                canvasWindow.Close();
            }
        }

        async Task GetTile(SemaphoreSlim semaphore, HttpClient client, List<byte[]> tiles, int zoom, int tileCount, int mapTileX, int mapTileY, int x, int y) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.terrain-rgb/{0}/{1}/{2}@2x.pngraw", zoom, mapTileX, mapTileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);

                if (data != null) {
                    TileHelper.WriteCache(mainWindow.CachePath, name, data);
                }
            }

            if (data != null) {
                MemoryStream stream = new MemoryStream(data);
                var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                var frame = png.Frames[0];
                var bitmap = new byte[frame.PixelWidth * frame.PixelHeight * 4];
                frame.CopyPixels(bitmap, frame.PixelWidth * 4, 0);
                var index = x + (y * tileCount);
                tiles[index] = bitmap;
            }
        }

        async Task GetVectorTile(SemaphoreSlim semaphore, HttpClient client, List<VectorTile> tiles, int zoom, int tileCount, int mapTileX, int mapTileY, int x, int y) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/{0}/{1}/{2}.vector.pbf", zoom, mapTileX, mapTileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);

                if (data != null) {
                    TileHelper.WriteCache(mainWindow.CachePath, name, data);
                }
            }

            if (data != null) {
                using (MemoryStream raw = new MemoryStream(data))
                using (GZipStream decompressor = new GZipStream(raw, CompressionMode.Decompress))
                using (MemoryStream decompressed = new MemoryStream()) {
                    decompressor.CopyTo(decompressed);
                    var layers = new VectorTile(decompressed.ToArray());
                    var index = x + (y * tileCount);
                    tiles[index] = layers;
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

        async Task<Image<float>> CombineTiles(List<byte[]> tiles, int tileCount) {
            int size = tileCount * tileSize;
            Image<float> image = new Image<float>(size, size);

            await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                for (int i = start; i < end; i++) {
                    var point = image.GetPoint(i);
                    image[point] = GetPixel(tiles, tileCount, point.x, point.y);
                }
            });

            return image;
        }

        async Task<ImageGroup<float>> CropData(Image<float> rawImage, int tileCount, int outputSize, double sizeRatio, double xOffset, double yOffset) {
            Sampler<float> sampler = new Sampler<float>(rawImage);
            int outputTileSize = outputSize / tileCount;
            ImageGroup<float> imageGroup = new ImageGroup<float>(tileCount, outputTileSize + 1);

            sampler.FlipVertically = gridSettings.FlipOutput;
            sampler.Filtering = FilteringType.Linear;

            foreach (var tilePoint in imageGroup) {
                var image = imageGroup[tilePoint];

                await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                    for (int i = start; i < end; i++) {
                        var point = image.GetPoint(i);
                        Point pos = new Point(
                            ((point.x + (outputTileSize * tilePoint.x)) / (double)outputSize * sizeRatio) + xOffset,
                            ((point.y + (outputTileSize * tilePoint.y)) / (double)outputSize * sizeRatio) + yOffset
                        );
                        float sample = sampler.Sample(pos);
                        image[point] = sample;
                    }
                });

                progressWindow.Increment();
            }

            return imageGroup;
        }

        float GetWaterPixel(byte[] bitmap, int size, int channels, int x, int y) {
            int index = (x + y * size) * channels;

            int data = 0;
            data += bitmap[index + 0];
            data += bitmap[index + 1];
            data += bitmap[index + 2];

            return data / 765f; //average of 3 pixels, max 256 each
        }

        Image<float> GetWaterData(RenderTargetBitmap bitmap) {
            Image<float> image = new Image<float>(bitmap.PixelWidth, bitmap.PixelHeight);

            int channels = bitmap.Format.BitsPerPixel / 8;
            byte[] bitmapBytes = new byte[bitmap.PixelWidth * bitmap.PixelHeight * channels];

            bitmap.CopyPixels(bitmapBytes, bitmap.PixelWidth * channels, 0);

            foreach (var point in image) {
                image[point] = GetWaterPixel(bitmapBytes, bitmap.PixelWidth, channels, point.x, point.y);
            }

            return image;
        }

        async Task<(float, float)> GetHeightLimits(ImageGroup<float> heightDataGroup) {
            float max = float.NegativeInfinity;
            float min = float.PositiveInfinity;

            int batchCount = TileHelper.GetParallelBatchCount();

            List<float> localMins = new List<float>(batchCount);
            List<float> localMaxes = new List<float>(batchCount);

            for (int i = 0; i < batchCount; i++) {
                localMins.Add(0);
                localMaxes.Add(0);
            }

            foreach (var tilePoint in heightDataGroup) {
                var heightData = heightDataGroup[tilePoint];

                await TileHelper.ProcessImageParallel(heightData, (int batchID, int start, int end) => {
                    float localMax = float.NegativeInfinity;
                    float localMin = float.PositiveInfinity;

                    for (int i = start; i < end; i++) {
                        var point = heightData.GetPoint(i);
                        var height = heightData[point];
                        localMin = Math.Min(localMin, height);
                        localMax = Math.Max(localMax, height);
                    }

                    localMins[batchID] = localMin;
                    localMaxes[batchID] = localMax;
                });

                for (int i = 0; i < batchCount; i++) {
                    min = Math.Min(min, localMins[i]);
                    max = Math.Max(max, localMaxes[i]);
                }

                progressWindow.Increment();
            }

            return (min, max);
        }
    }
}
