﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using MapboxNetCore;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Mapbox.VectorTile;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Windows.Ink;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace Mapper {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window {
        public string SettingsPath { get; private set; }
        public string CachePath { get; private set; }

        public AppSettings AppSettings { get; private set; }

        public static RoutedUICommand SaveCommand = new RoutedUICommand("Save", "Save", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });
        public static RoutedUICommand SaveAsCommand = new RoutedUICommand("Save as", "Save-as", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt) });
        public static RoutedUICommand OpenCommand = new RoutedUICommand("Open", "Open", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control) });

        public MainWindow() {
            InitializeComponent();
            SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazgriz/Mapper/settings.json");
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazgriz/Mapper/tilecache");

            if (!Directory.Exists(CachePath)) {
                Directory.CreateDirectory(CachePath);
            }

            GridControl.Init(this, Map);

            LoadSettings();
            ApplySettings(true);
            OpenLastFile();
        }
        
        void OnClose(object sender, CancelEventArgs e) {
            AppSettings.Coordinates = Map.Center;
            AppSettings.Zoom = Map.Zoom;
            SaveSettings();
        }

        void LoadSettings() {
            AppSettings = new AppSettings();

            try {
                JObject settings;

                using (var textReader = new StreamReader(SettingsPath))
                using (var reader = new JsonTextReader(textReader)) {
                    settings = (JObject)JToken.ReadFrom(reader);
                    Console.WriteLine("Settings file read from {0}", SettingsPath);
                }

                AppSettings.CopyFrom(settings);
            }
            catch (DirectoryNotFoundException) {
                var settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mapper");
                Directory.CreateDirectory(settingsFolder);
                OpenSettingsDialog();
                return;
            }
            catch (FileNotFoundException) {
                OpenSettingsDialog();
                return;
            }

            AppSettings.Validate();
            SaveSettings();
        }

        public void OpenSettingsDialog(object sender = null, RoutedEventArgs e = null) {
            var settingsWindow = new SettingsWindow(AppSettings);
            settingsWindow.ShowDialog();
            AppSettings.Validate();
            SaveSettings();
            ApplySettings(false);
        }

        void SaveSettings() {
            using (var textWriter = new StreamWriter(SettingsPath))
            using (var writer = new JsonTextWriter(textWriter)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.DefaultValueHandling = DefaultValueHandling.Populate;
                serializer.Serialize(writer, AppSettings);
            }

            Console.WriteLine("Settings file written to {0}", SettingsPath);
        }

        void ApplySettings(bool moveCenter) {
            Map.AccessToken = AppSettings.APIKey;

            if (moveCenter) {
                Map.Center = AppSettings.Coordinates;
            }

            Map.Zoom = AppSettings.Zoom;
            Map.AllowRotation = AppSettings.AllowRotation;
        }

        void OpenLastFile() {
            if (string.IsNullOrEmpty(AppSettings.LastFile)) return;
            if (!File.Exists(AppSettings.LastFile)) {
                AppSettings.LastFile = "";
                return;
            }

            LoadGridSettings(AppSettings.LastFile);
        }

        public void SaveAsGridSettingsHandler(object sender = null, RoutedEventArgs e = null) {
            var savePath = AppSettings.SavePath;

            if (!Directory.Exists(savePath)) {
                //invalid save path, prompt user for a path
                savePath = "";
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON file|*.json";
            dialog.InitialDirectory = savePath;

            if (dialog.ShowDialog() == true) {
                AppSettings.SavePath = Path.GetDirectoryName(dialog.FileName);
                SaveGridSettings(dialog.FileName);
            }

            GridControl.LoadedFileChanged = false;
            UpdateTitle();
        }

        public void SaveGridSettingsHandler(object sender = null, RoutedEventArgs e = null) {
            if (AppSettings.LastFile == "") {
                SaveAsGridSettingsHandler();
                return;
            }

            SaveGridSettings(AppSettings.LastFile);

            GridControl.LoadedFileChanged = false;
            UpdateTitle();
        }

        void SaveGridSettings(string path) {
            try {
                using (var textWriter = new StreamWriter(path))
                using (var writer = new JsonTextWriter(textWriter)) {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented;
                    serializer.DefaultValueHandling = DefaultValueHandling.Populate;
                    serializer.Serialize(writer, GridControl.GridSettings);
                }

                Console.WriteLine("Settings file written to {0}", path);
                AppSettings.LastFile = path;
            }
            catch (IOException) {
                //do nothing
            }
        }

        public void LoadGridSettingsHandler(object sender = null, RoutedEventArgs e = null) {
            var loadPath = AppSettings.SavePath;

            if (!Directory.Exists(loadPath)) {
                //invalid save path, prompt user for a path
                loadPath = "";
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JSON file|*.json";
            dialog.InitialDirectory = loadPath;
            if (dialog.ShowDialog() == true) {
                loadPath = Path.GetDirectoryName(dialog.FileName);
                LoadGridSettings(dialog.FileName);
            }

            AppSettings.SavePath = loadPath;
        }

        void LoadGridSettings(string path) {
            try {
                JObject settings = null;
                using (var textReader = new StreamReader(path))
                using (var reader = new JsonTextReader(textReader)) {
                    settings = JToken.ReadFrom(reader) as JObject;
                }

                GridControl.LoadSettings(Path.GetFileName(path), settings);

                Console.WriteLine("Grid settings file read from {0}", path);
                AppSettings.LastFile = path;
            }
            catch (IOException) {
                //do nothing
            }
        }

        public void DebugTiles(List<byte[]> tiles, int tileCount) {
            if (!AppSettings.DebugMode) return;

            CanvasWindow window = new CanvasWindow();
            window.Owner = this;
            window.Title = "Height raw tile debugger";

            window.Canvas.Width = 514 * tileCount;
            window.Canvas.Height = 514 * tileCount;

            for (int i = 0; i < tileCount; i++) {
                for (int j = 0; j < tileCount; j++) {
                    var tile = tiles[i * tileCount + j];
                    if (tile == null) continue;
                    var image = new System.Windows.Controls.Image();
                    window.Canvas.Children.Add(image);
                    var bitmap = BitmapSource.Create(Generator.tileSize, Generator.tileSize, 96, 96,
                                                     System.Windows.Media.PixelFormats.Bgra32,
                                                     null, tile, Generator.tileSize * 4);
                    image.Source = bitmap;
                    System.Windows.Controls.Canvas.SetTop(image, 514 * i);
                    System.Windows.Controls.Canvas.SetLeft(image, 514 * j);
                }
            }

            window.Show();
        }

        public CanvasWindow DrawVectorTiles(List<VectorTile> tiles, int tileCount) {
            CanvasWindow window = new CanvasWindow();
            window.Owner = this;

            window.Canvas.Background = System.Windows.Media.Brushes.White;
            window.Canvas.Width = tileCount * Generator.tileSize;
            window.Canvas.Height = tileCount * Generator.tileSize;

            for (int i = 0; i < tileCount; i++) {
                for (int j = 0; j < tileCount; j++) {
                    var tile = tiles[i * tileCount + j];
                    if (tile == null) continue;

                    var water = tile.GetLayer("water");

                    if (water != null) {
                        long extent = (long)water.Extent;
                        TileHelper.DrawVectorTileToCanvas(window.Canvas, water, Generator.tileSize, extent, i * Generator.tileSize, j * Generator.tileSize);
                    }
                }
            }

            window.Show();
            window.Hide();

            return window;
        }

        public void CloseWatermap(CanvasWindow canvasWindow) {
            if (AppSettings.DebugMode) {
                canvasWindow.Show();
                canvasWindow.Title = "Watermap Debugger";
            } else {
                canvasWindow.Close();
            }
        }

        public void DebugHeightmap(ImageGroup<float> heightmapGroup) {
            if (!AppSettings.DebugMode) return;

            CanvasWindow window = new CanvasWindow();
            window.Owner = this;
            window.Title = "Heightmap Debugger";
            int canvasSize = heightmapGroup.TileCount * (heightmapGroup.TileSize + 2);
            window.Canvas.Width = canvasSize;
            window.Canvas.Height = canvasSize;

            foreach (var tilePoint in heightmapGroup) {
                var heightmap = heightmapGroup[tilePoint];

                var image = new System.Windows.Controls.Image();
                window.Canvas.Children.Add(image);
                System.Windows.Controls.Canvas.SetTop(image, tilePoint.y * (heightmapGroup.TileSize + 2));
                System.Windows.Controls.Canvas.SetLeft(image, tilePoint.x * (heightmapGroup.TileSize + 2));

                var writeableBitmap = new WriteableBitmap(
                    heightmap.Width, heightmap.Height,
                    96, 96,
                    System.Windows.Media.PixelFormats.Bgr32, null);

                image.Source = writeableBitmap;

                try {
                    writeableBitmap.Lock();
                    IntPtr ptr = writeableBitmap.BackBuffer;

                    foreach (var point in heightmap) {
                        int index = (point.y * writeableBitmap.BackBufferStride) + (point.x * 4);
                        float height = heightmap[point];
                        byte value = (byte)(height * 255f);

                        unsafe {
                            ((byte*)ptr)[index + 0] = value;
                            ((byte*)ptr)[index + 1] = value;
                            ((byte*)ptr)[index + 2] = value;
                        }
                    }

                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, heightmap.Width, heightmap.Height));
                }
                finally {
                    writeableBitmap.Unlock();
                }
            }

            window.Show();
        }

        public void ExportImage(GridSettings gridSettings, OutputMapData outputMapData) {
            bool shouldUseArchive = outputMapData.HeightData.TileCount != 1
                || gridSettings.ForceZipExport
                || gridSettings.DownloadSatelliteImages;

            if (shouldUseArchive) {
                ExportImageGroup(gridSettings, outputMapData);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "RAW file|*.raw";

            if (dialog.ShowDialog() == true) {
                var tile = outputMapData.HeightData[new PointInt(0, 0)];
                byte[] data = new byte[tile.Width * tile.Height * 2];
                Buffer.BlockCopy(tile.Data, 0, data, 0, data.Length);
                File.WriteAllBytes(dialog.FileName, data);
            }
        }

        void ExportImageGroup(GridSettings gridSettings, OutputMapData outputMapData) {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "zip archive|*.zip";

            if (dialog.ShowDialog() == true) {
                using (var fileStream = new FileStream(dialog.FileName, FileMode.Create))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create)) {
                    // write tiles
                    foreach (var tilePoint in outputMapData.HeightData) {
                        var entry = archive.CreateEntry(string.Format("tile_{0}_{1}.raw", tilePoint.x, tilePoint.y), CompressionLevel.Fastest);

                        var tile = outputMapData.HeightData[tilePoint];
                        byte[] data = new byte[tile.Width * tile.Height * 2];
                        Buffer.BlockCopy(tile.Data, 0, data, 0, data.Length);

                        using (var stream = entry.Open()) {
                            stream.Write(data, 0, data.Length);
                        }
                    }

                    // write info
                    var infoEntry = archive.CreateEntry("info.json", CompressionLevel.Fastest);
                    using (var textWriter = new StreamWriter(infoEntry.Open()))
                    using (var writer = new JsonTextWriter(textWriter)) {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Formatting = Formatting.Indented;
                        serializer.DefaultValueHandling = DefaultValueHandling.Populate;
                        serializer.Serialize(writer, GridControl.GridSettings);
                    }

                    // write satellite data
                    if (gridSettings.DownloadSatelliteImages) {
                        var imageSize = outputMapData.SatelliteData.TileSize;
                        byte[] buffer = new byte[4 * imageSize * imageSize];

                        foreach (var tilePoint in outputMapData.SatelliteData) {
                            var tile = outputMapData.SatelliteData[tilePoint];
                            var entry = archive.CreateEntry(string.Format("tile_{0}_{1}.png", tilePoint.x, tilePoint.y), CompressionLevel.Fastest);

                            for (int x = 0; x < imageSize; x++) {
                                for (int y = 0; y < imageSize; y++) {
                                    int tileLocalIndex = (x + y * imageSize) * 4;
                                    buffer[tileLocalIndex + 2] = tile[new PointInt(x, y)].r;
                                    buffer[tileLocalIndex + 1] = tile[new PointInt(x, y)].g;
                                    buffer[tileLocalIndex + 0] = tile[new PointInt(x, y)].b;
                                    buffer[tileLocalIndex + 3] = 255;
                                }
                            }

                            var pngEncoder = new PngBitmapEncoder();
                            BitmapSource image = BitmapSource.Create(
                                imageSize,
                                imageSize,
                                96,
                                96,
                                PixelFormats.Bgra32,
                                null,
                                buffer,
                                imageSize * 4);

                            pngEncoder.Frames.Add(BitmapFrame.Create(image));

                            using (var stream = entry.Open())
                            using (var mem = new MemoryStream()) {
                                pngEncoder.Save(mem);
                                mem.Position = 0;
                                mem.CopyTo(stream);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateTitle() {
            Title = string.Format("Mapper ({0}) - {1}{2}", Version.CurrentVersion, GridControl.LoadedFile, GridControl.LoadedFileChanged ? "*" : "");
        }
    }
}
