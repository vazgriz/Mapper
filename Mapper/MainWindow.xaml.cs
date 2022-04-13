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

namespace Mapper {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window {
        string settingsPath;

        public AppSettings AppSettings { get; private set; }

        public MainWindow() {
            InitializeComponent();
            settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mapper/settings.json");
            GridControl.Init(this, Map);

            LoadSettings();
            ApplySettings();
        }
        
        void OnClose(object sender, CancelEventArgs e) {
            AppSettings.Coordinates = Map.Center;
            AppSettings.Zoom = Map.Zoom;
            SaveSettings();
        }

        void LoadSettings() {
            try {
                using (var textReader = new StreamReader(settingsPath))
                using (JsonReader reader = new JsonTextReader(textReader)) {
                    JsonSerializer serializer = new JsonSerializer();
                    AppSettings = serializer.Deserialize<AppSettings>(reader);
                    Console.WriteLine("Settings file read from {0}", settingsPath);
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
            ApplySettings();
        }

        void SaveSettings() {
            using (var textWriter = new StreamWriter(settingsPath))
            using (var writer = new JsonTextWriter(textWriter)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.DefaultValueHandling = DefaultValueHandling.Populate;
                serializer.Serialize(writer, AppSettings);
            }

            Console.WriteLine("Settings file written to {0}", settingsPath);
        }

        void ApplySettings() {
            Map.AccessToken = AppSettings.APIKey;
            Map.Center = AppSettings.Coordinates;
            Map.Zoom = AppSettings.Zoom;
            Map.AllowRotation = AppSettings.AllowRotation;
        }

        public void DebugTiles(List<PngBitmapDecoder> tiles, int tileCount) {
            if (!AppSettings.DebugMode) return;

            CanvasWindow window = new CanvasWindow();
            window.Owner = this;

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
    }
}
