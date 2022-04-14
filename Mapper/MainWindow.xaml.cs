using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using MapboxNetCore;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using Mapbox.VectorTile;
using System.Windows.Input;
using Microsoft.Win32;

namespace Mapper {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window {
        public string SettingsPath { get; private set; }
        public string CachePath { get; private set; }

        public AppSettings AppSettings { get; private set; }

        public static RoutedUICommand SaveCommand = new RoutedUICommand("Save", "Save", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });
        public static RoutedUICommand OpenCommand = new RoutedUICommand("Open", "Open", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control) });

        public MainWindow() {
            InitializeComponent();
            SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mapper/settings.json");
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mapper/tilecache");

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
            try {
                using (var textReader = new StreamReader(SettingsPath))
                using (JsonReader reader = new JsonTextReader(textReader)) {
                    JsonSerializer serializer = new JsonSerializer();
                    AppSettings = serializer.Deserialize<AppSettings>(reader);
                    Console.WriteLine("Settings file read from {0}", SettingsPath);
                }
            }
            catch (DirectoryNotFoundException) {
                var settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mapper");
                Directory.CreateDirectory(settingsFolder);
                AppSettings = new AppSettings();
                OpenSettingsDialog();
                return;
            }
            catch (FileNotFoundException) {
                AppSettings = new AppSettings();
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

        public void SaveGridSettings(object sender = null, RoutedEventArgs e = null) {
            var savePath = AppSettings.SavePath;

            if (!Directory.Exists(savePath)) {
                //invalid save path, prompt user for a path
                savePath = "";
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JSON file|*.json";
            dialog.InitialDirectory = savePath;

            if (dialog.ShowDialog() == true) {
                savePath = Path.GetDirectoryName(dialog.FileName);
                SaveGridSettings(dialog.FileName);
            }

            AppSettings.SavePath = savePath;
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

        public void LoadGridSettings(object sender = null, RoutedEventArgs e = null) {
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
            GridSettings gridSettings = null;

            try {
                using (var textReader = new StreamReader(path))
                using (JsonReader reader = new JsonTextReader(textReader)) {
                    JsonSerializer serializer = new JsonSerializer();
                    gridSettings = serializer.Deserialize<GridSettings>(reader);
                }

                GridControl.LoadSettings(gridSettings);

                Console.WriteLine("Grid settings file read from {0}", path);
                AppSettings.LastFile = path;
            }
            catch (IOException) {
                //do nothing
            }
        }

        public void DebugTiles(List<PngBitmapDecoder> tiles, int tileCount) {
            if (!AppSettings.DebugMode) return;

            CanvasWindow window = new CanvasWindow();
            window.Owner = this;
            window.Title = "Height tile debugger";

            window.Canvas.Width = 514 * tileCount;
            window.Canvas.Height = 514 * tileCount;

            for (int i = 0; i < tileCount; i++) {
                for (int j = 0; j < tileCount; j++) {
                    var tile = tiles[i * tileCount + j];
                    if (tile == null) continue;
                    var image = new System.Windows.Controls.Image();
                    window.Canvas.Children.Add(image);
                    image.Source = tile.Frames[0];
                    System.Windows.Controls.Canvas.SetTop(image, 514 * i);
                    System.Windows.Controls.Canvas.SetLeft(image, 514 * j);
                }
            }

            window.Show();
        }

        public CanvasWindow DrawVectorTiles(List<VectorTile> tiles, int tileCount) {
            CanvasWindow window = new CanvasWindow();
            window.Owner = this;

            window.Canvas.Width = tileCount * 512;
            window.Canvas.Height = tileCount * 512;

            for (int i = 0; i < tileCount; i++) {
                for (int j = 0; j < tileCount; j++) {
                    var tile = tiles[i * tileCount + j];
                    if (tile == null) continue;

                    var water = tile.GetLayer("water");

                    if (water != null) {
                        TileHelper.DrawVectorTileToCanvas(window.Canvas, water);
                    }
                }
            }

            window.Show();
            window.Hide();

            return window;
        }

        public void DebugHeightmap(Image<float> heightmap) {
            if (!AppSettings.DebugMode) return;

            CanvasWindow window = new CanvasWindow();
            window.Owner = this;
            window.Title = "Heightmap Debugger";

            var image = new System.Windows.Controls.Image();
            window.Canvas.Children.Add(image);
            window.Canvas.Width = heightmap.Width;
            window.Canvas.Height = heightmap.Height;
            System.Windows.Controls.Canvas.SetTop(image, 0);
            System.Windows.Controls.Canvas.SetLeft(image, 0);

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

            window.Show();
        }
    }
}
