﻿using MapboxNetCore;
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

        public class MapCache {
            public ImageGroup<float> HeightData { get; set; }
            public ImageGroup<float> WaterData { get; set; }
            public ImageGroup<Color> SatelliteData { get; set; }
            public float HeightMin { get; set; }
            public float HeightMax { get; set; }
        }

        public MapCache Cache { get; private set; }

        public Generator(MainWindow mainWindow, GridControl gridControl) {
            if (mainWindow == null) throw new ArgumentNullException(nameof(mainWindow));
            if (gridControl == null) throw new ArgumentNullException(nameof(gridControl));

            this.mainWindow = mainWindow;
            appSettings = mainWindow.AppSettings;
            this.gridControl = gridControl;
            gridSettings = gridControl.GridSettings;

            ServicePointManager.DefaultConnectionLimit = concurrentDownloads;
        }

        public void InvalidateCache() {
            Cache = null;
        }

        public async void Inspect(GeoExtent extent, ProgressWindow progressWindow) {
            this.progressWindow = progressWindow;

            try {
                if (Cache == null) {
                    await GetMapData(extent);
                }

                gridControl.FinishInspection();
            }
            catch (OperationCanceledException) {
                gridControl.CancelInspection();
            }
        }

        public async void Generate(GeoExtent extent, ProgressWindow progressWindow) {
            this.progressWindow = progressWindow;

            try {
                if (Cache == null) {
                    await GetMapData(extent);
                }

                Pipeline pipeline = new Pipeline();
                pipeline.NormalizeMin = Cache.HeightMin;
                pipeline.NormalizeMax = Cache.HeightMax;
                pipeline.ApplyWaterOffset = gridSettings.ApplyWaterOffset;
                pipeline.WaterOffset = gridSettings.WaterOffset;

                if (gridSettings.HeightMin != 0 || gridSettings.HeightMax != 0) {
                    pipeline.NormalizeMin = gridSettings.HeightMin;
                    pipeline.NormalizeMax = gridSettings.HeightMax;
                }

                progressWindow.SetText("Processing tiles");
                progressWindow.SetMaximum(pipeline.GetProcessSteps(gridSettings.TileCount));
                progressWindow.Reset();

                OutputMapData output = new OutputMapData();
                output.HeightData = new ImageGroup<ushort>(Cache.HeightData.TileCount, Cache.HeightData.TileSize);

                await pipeline.Process(progressWindow, Cache, output);
                await Task.Delay(20);

                gridControl.FinishGenerating(output);
            }
            catch (OperationCanceledException) {
                gridControl.CancelGenerating();
            }
        }

        int GetDownloadSteps(int rawTileCount, int tileCount) {
            int steps = rawTileCount * rawTileCount;
            steps += tileCount * tileCount;

            if (gridSettings.ApplyWaterOffset) {
                steps += rawTileCount * rawTileCount;
            }

            if (gridSettings.DownloadSatelliteImages) {
                steps += rawTileCount * rawTileCount;
            }

            return steps;
        }

        async Task GetMapData(GeoExtent extent) {
            int outputSize = gridSettings.OutputSize;
            int terrainOutputSize = gridSettings.OutputSize + 1;
            double gridSizeMeters = gridSettings.GridSize * 1000;
            double pixelDensity = gridSizeMeters / terrainOutputSize;    //calculate meters/pixel
            int zoom = TileHelper.GetZoomLevel(pixelDensity, gridSettings.CoordinateY);

            zoom = Math.Min(zoom, maxZoom);

            var x1 = TileHelper.LongitudeToTile(extent.TopLeft.Longitude, zoom);
            var y1 = TileHelper.LatitudeToTile(extent.TopLeft.Latitude, zoom);
            var x2 = TileHelper.LongitudeToTile(extent.BottomRight.Longitude, zoom);
            var y2 = TileHelper.LatitudeToTile(extent.BottomRight.Latitude, zoom);

            var rawTileCount = Math.Max(x2 - x1 + 1, y2 - y1 + 1);

            double tilePixelDensity = TileHelper.GetPixelDensity(zoom, gridSettings.CoordinateY);
            double tileSizeMeters = rawTileCount * tileSize * tilePixelDensity;

            if (tileSizeMeters < gridSizeMeters) {
                rawTileCount++;
                tileSizeMeters = rawTileCount * tileSize * tilePixelDensity;
            }

            //only try enlarging once
            if (tileSizeMeters < gridSizeMeters) {
                throw new Exception("Could not find suitable raw tile count");
            }

            double sizeRatio = gridSizeMeters / tileSizeMeters;

            var tileLng1 = TileHelper.TileToLongitude(x1, zoom);
            var tileLat1 = TileHelper.TileToLatitude(y1, zoom);

            var tileLng2 = TileHelper.TileToLongitude(x1 + rawTileCount + 1, zoom);
            var tileLat2 = TileHelper.TileToLatitude(y1 + rawTileCount + 1, zoom);

            double xOffset = Utility.InverseLerp(tileLng1, tileLng2, extent.TopLeft.Longitude);
            double yOffset = Utility.InverseLerp(tileLat1, tileLat2, extent.TopLeft.Latitude);

            progressWindow.SetText("Downloading tiles");
            progressWindow.SetMaximum(GetDownloadSteps(rawTileCount, gridSettings.TileCount));
            progressWindow.Reset();

            RawMapData rawMapData = await GetMapImageData(rawTileCount, zoom, x1, y1);
            await Task.Delay(20);

            var heightData = await CropData(rawMapData.HeightData, gridSettings.TileCount, terrainOutputSize, sizeRatio, xOffset, yOffset, 1);
            ImageGroup<float> waterData = null;
            ImageGroup<Color> satelliteData = null;

            if (rawMapData.WaterData != null) {
                waterData = await CropData(rawMapData.WaterData, gridSettings.TileCount, terrainOutputSize, sizeRatio, xOffset, yOffset, 1);
            }

            if (rawMapData.SatelliteData != null) {
                satelliteData = await CropData(rawMapData.SatelliteData, gridSettings.TileCount, outputSize, sizeRatio, xOffset, yOffset, 0);
            }

            mainWindow.DebugHeightmap(heightData);

            var (heightMin, heightMax) = await GetHeightLimits(heightData);

            Cache = new MapCache();
            Cache.HeightData = heightData;
            Cache.WaterData = waterData;
            Cache.SatelliteData = satelliteData;
            Cache.HeightMin = (float)Math.Floor(heightMin);
            Cache.HeightMax = (float)Math.Floor(heightMax);
        }

        async Task<RawMapData> GetMapImageData(int tileCount, int zoom, int x1, int y1) {
            var tiles = new List<byte[]>(tileCount * tileCount);
            var vectorTiles = new List<VectorTile>(tileCount * tileCount);
            var satelliteTiles = new List<byte[]>(tileCount * tileCount);

            for (int i = 0; i < tileCount * tileCount; i++) {
                tiles.Add(null);
                vectorTiles.Add(null);
                satelliteTiles.Add(null);
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

                        if (gridSettings.DownloadSatelliteImages) {
                            var task3 = Task.Run(async () => {
                                await GetSatelliteTile(semaphore, client, satelliteTiles, zoom, tileCount, mapTileX, mapTileY, x, y);
                                progressWindow.Increment();
                            });

                            tasks.Add(task3);
                        }
                    }
                }

                await Task.WhenAll(tasks);
            }

            RawMapData result = new RawMapData();
            result.HeightData = await CombineHeightTiles(tiles, tileCount);

            if (gridSettings.DownloadSatelliteImages) {
                result.SatelliteData = await CombineSatelliteTiles(satelliteTiles, tileCount);
            }

            mainWindow.DebugTiles(tiles, tileCount);

            if (!gridSettings.ApplyWaterOffset) {
                return result;
            }

            var canvasWindow = mainWindow.DrawVectorTiles(vectorTiles, tileCount);

            try {
                var canvas = canvasWindow.Canvas;
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)canvas.Width,
                    (int)canvas.Height, 96, 96, System.Windows.Media.PixelFormats.Default);
                rtb.Render(canvas);

                result.WaterData = await GetWaterData(rtb);

                return result;
            }
            finally {
                mainWindow.CloseWatermap(canvasWindow);
            }
        }

        async Task GetTile(SemaphoreSlim semaphore, HttpClient client, List<byte[]> tiles, int zoom, int tileCount, int mapTileX, int mapTileY, int x, int y) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.terrain-rgb/{0}/{1}/{2}@2x.pngraw", zoom, mapTileX, mapTileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name, progressWindow.CancellationToken);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);

                if (data != null) {
                    TileHelper.WriteCache(mainWindow.CachePath, name, data);
                }
            }

            if (data != null) {
                using (MemoryStream stream = new MemoryStream(data)) {
                    var png = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                    var frame = png.Frames[0];
                    var bitmap = new byte[frame.PixelWidth * frame.PixelHeight * 4];
                    frame.CopyPixels(bitmap, frame.PixelWidth * 4, 0);
                    var index = x + (y * tileCount);
                    tiles[index] = bitmap;
                }
            }
        }

        async Task GetVectorTile(SemaphoreSlim semaphore, HttpClient client, List<VectorTile> tiles, int zoom, int tileCount, int mapTileX, int mapTileY, int x, int y) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.mapbox-streets-v8/{0}/{1}/{2}.vector.pbf", zoom, mapTileX, mapTileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name, progressWindow.CancellationToken);

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

        async Task GetSatelliteTile(SemaphoreSlim semaphore, HttpClient client, List<byte[]> tiles, int zoom, int tileCount, int mapTileX, int mapTileY, int x, int y) {
            string name = string.Format("https://api.mapbox.com/v4/mapbox.satellite/{0}/{1}/{2}@2x.jpg", zoom, mapTileX, mapTileY);
            var data = await TileHelper.TryLoadFromCache(mainWindow.CachePath, name, progressWindow.CancellationToken);

            if (data == null) {
                data = await DownloadTile(semaphore, client, name);

                if (data != null) {
                    TileHelper.WriteCache(mainWindow.CachePath, name, data);
                }
            }

            if (data != null) {
                using (MemoryStream stream = new MemoryStream(data)) {
                    // jpeg decodes to 24 bit bgr
                    var jpeg = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                    var frame = jpeg.Frames[0];
                    var bitmap = new byte[frame.PixelWidth * frame.PixelHeight * 3];
                    frame.CopyPixels(bitmap, frame.PixelWidth * 3, 0);
                    var index = x + (y * tileCount);
                    tiles[index] = bitmap;
                }
            }
        }

        async Task<byte[]> DownloadTile(SemaphoreSlim semaphore, HttpClient client, string name) {
            try {
                await semaphore.WaitAsync().ConfigureAwait(false);
                string url = string.Format("{0}?access_token={1}", name, appSettings.APIKey);
                try {
                    byte[] response = await client.GetByteArrayAsync(url);
                    progressWindow.CancellationToken.ThrowIfCancellationRequested();
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

        float GetHeightPixel(List<byte[]> tiles, int tileCount, int x, int y) {
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

        Color GetSatellitePixel(List<byte[]> tiles, int tileCount, int x, int y) {
            int tileX = x / tileSize;
            int tileY = y / tileSize;
            int tileLocalX = x % tileSize;
            int tileLocalY = y % tileSize;

            int tileIndex = tileX + tileY * tileCount;
            var tile = tiles[tileIndex];

            if (tile == null) return new Color();

            int tileLocalIndex = (tileLocalX + tileLocalY * tileSize) * 3;
            byte red = tile[tileLocalIndex + 2];
            byte green = tile[tileLocalIndex + 1];
            byte blue = tile[tileLocalIndex + 0];
            return new Color(red, green, blue, 255);
        }

        async Task<ImageGroup<float>> CombineHeightTiles(List<byte[]> tiles, int tileCount) {
            ImageGroup<float> image = new ImageGroup<float>(tileCount, tileSize);

            await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                for (int i = start; i < end; i++) {
                    var point = image.GetPoint(i);
                    image.SetData(point, GetHeightPixel(tiles, tileCount, point.x, point.y));

                    if (i % TileHelper.cancellationCheckInterval == 0) {
                        progressWindow.CancellationToken.ThrowIfCancellationRequested();
                    }
                }
            });

            return image;
        }

        async Task<ImageGroup<Color>> CombineSatelliteTiles(List<byte[]> tiles, int tileCount) {
            ImageGroup<Color> image = new ImageGroup<Color>(tileCount, tileSize);

            await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                for (int i = start; i < end; i++) {
                    var point = image.GetPoint(i);
                    image.SetData(point, GetSatellitePixel(tiles, tileCount, point.x, point.y));

                    if (i % TileHelper.cancellationCheckInterval == 0) {
                        progressWindow.CancellationToken.ThrowIfCancellationRequested();
                    }
                }
            });

            return image;
        }

        async Task<ImageGroup<T>> CropData<T>(ImageGroup<T> rawImage, int tileCount, int outputSize, double sizeRatio, double xOffset, double yOffset, int extraTileSize)
            where T : struct
        {
            int outputTileSize = outputSize;
            if (tileCount > 1) {
                outputTileSize = (outputSize / tileCount) + extraTileSize;
            }

            ImageGroup<T> imageGroup = new ImageGroup<T>(tileCount, outputTileSize);
            Sampler<T> sampler = new Sampler<T>(rawImage);

            sampler.FlipVertically = gridSettings.FlipOutput;
            sampler.Filtering = FilteringType.Linear;

            foreach (var tilePoint in imageGroup) {
                var image = imageGroup[tilePoint];

                await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                    int posXOffset = -tilePoint.x;
                    int posYOffset = -tilePoint.y;

                    for (int i = start; i < end; i++) {
                        var point = image.GetPoint(i);
                        Point pos = new Point(
                            ((point.x + (outputTileSize * tilePoint.x) + posXOffset) / (double)outputSize * sizeRatio) + xOffset,
                            ((point.y + (outputTileSize * tilePoint.y) + posYOffset) / (double)outputSize * sizeRatio) + yOffset
                        );
                        T sample = sampler.Sample(pos);
                        image[point] = sample;

                        if (i % TileHelper.cancellationCheckInterval == 0) {
                            progressWindow.CancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                });

                progressWindow.Increment();
            }

            return imageGroup;
        }

        float GetWaterPixel(byte[] bitmap, int size, int channels, int x, int y) {
            int index = (x + y * size) * channels;

            //only access R component
            int data = bitmap[index + 0];

            return data / 255f;
        }

        async Task<ImageGroup<float>> GetWaterData(RenderTargetBitmap bitmap) {
            int pixelWidth = bitmap.PixelWidth;
            int pixelHeight = bitmap.PixelHeight;
            ImageGroup<float> image = new ImageGroup<float>(pixelWidth, pixelHeight);

            int channels = bitmap.Format.BitsPerPixel / 8;
            byte[] bitmapBytes = new byte[pixelWidth * pixelHeight * channels];

            bitmap.CopyPixels(bitmapBytes, pixelWidth * channels, 0);

            await TileHelper.ProcessImageParallel(image, (int batchID, int start, int end) => {
                for (int i = start; i < end; i++) {
                    var point = image.GetTilePoint(i);
                    image.SetData(point, GetWaterPixel(bitmapBytes, pixelWidth, channels, point.x, point.y));
                }
            });

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

                        if (i % TileHelper.cancellationCheckInterval == 0) {
                            progressWindow.CancellationToken.ThrowIfCancellationRequested();
                        }
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
