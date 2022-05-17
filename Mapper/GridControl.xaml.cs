using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MapboxNetCore;
using MapboxNetWPF;
using Newtonsoft.Json.Linq;

namespace Mapper {
    /// <summary>
    /// Interaction logic for GridControl.xaml
    /// </summary>
    public partial class GridControl : UserControl, INotifyPropertyChanged {
        public GridSettings GridSettings { get; private set; }
        public bool Valid { get; private set; }

        public float HeightMin {
            get {
                if (generator == null) {
                    return 0;
                }

                var cache = generator.Cache;
                if (cache == null) {
                    return 0;
                }

                return cache.HeightMin;
            }
        }

        public float HeightMax {
            get {
                if (generator == null) {
                    return 0;
                }

                var cache = generator.Cache;
                if (cache == null) {
                    return 0;
                }

                return cache.HeightMax;
            }
        }

        public float HeightDifference {
            get {
                return HeightMax - HeightMin;
            }
        }

        public string LoadedFile { get; private set; } = "Untitled.json";
        public bool LoadedFileChanged { get; set; }

        MainWindow mainWindow;
        Map map;
        bool ignoreCoordChanges;

        ProgressWindow progressWindow;
        Generator generator;

        public bool IsWorking { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GridControl() {
            GridSettings = new GridSettings();
            InitializeComponent();
        }

        public void Init(MainWindow mainWindow, Map map) {
            this.mainWindow = mainWindow;
            this.map = map;
            map.GridCenterChanged += OnMapGridChanged;
            GridSettings.PropertyChanged += OnUIChanged;
            generator = new Generator(mainWindow, this);
        }

        public void LoadSettings(string path, JObject settings) {
            GridSettings.CopyFrom(settings);
            ValidateAll();

            if (Valid) {
                map.Ready += OnMapReady;
            }

            LoadedFile = path;
            LoadedFileChanged = false;

            mainWindow.UpdateTitle();
        }

        void OnMapReady(object sender, EventArgs e) {
            map.SetGridSize(GridSettings.GridSize, GridSettings.TileCount);
        }

        void InspectHeightMap(object sender, RoutedEventArgs e) {
            if (IsWorking) return;

            Valid = GridSettings.Validate();
            if (!Valid) return;

            var extent = mainWindow.Map.GetGridExtent(GridSettings.CoordinateX, GridSettings.CoordinateY, GridSettings.GridSize, GridSettings.OutputSize);

            IsWorking = true;
            progressWindow = new ProgressWindow();
            progressWindow.Owner = mainWindow;
            progressWindow.Show();
            generator.Inspect(extent, progressWindow);
        }

        public void FinishInspection() {
            if (!IsWorking) return;
            IsWorking = false;
            progressWindow.Close();
            progressWindow = null;

            OnPropertyChanged(nameof(HeightMin));
            OnPropertyChanged(nameof(HeightMax));
            OnPropertyChanged(nameof(HeightDifference));
        }

        public void CancelInspection() {
            if (!IsWorking) return;
            IsWorking = false;
            progressWindow.Close();
            progressWindow = null;
        }

        void GenerateHeightMap(object sender, RoutedEventArgs e) {
            if (IsWorking) return;

            Valid = GridSettings.Validate();
            if (!Valid) return;

            var extent = mainWindow.Map.GetGridExtent(GridSettings.CoordinateX, GridSettings.CoordinateY, GridSettings.GridSize, GridSettings.OutputSize);

            IsWorking = true;
            progressWindow = new ProgressWindow();
            progressWindow.Owner = mainWindow;
            progressWindow.Show();
            generator.Generate(extent, progressWindow);
        }

        public void FinishGenerating(ImageGroup<ushort> output) {
            if (!IsWorking) return;
            mainWindow.ExportImage(GridSettings.ForceZipExport, output);
            IsWorking = false;
            progressWindow.Close();
            progressWindow = null;
        }

        public void CancelGenerating() {
            if (!IsWorking) return;
            IsWorking = false;
            progressWindow.Close();
            progressWindow = null;
        }

        void InvalidateCache() {
            generator.InvalidateCache();

            OnPropertyChanged(nameof(HeightMin));
            OnPropertyChanged(nameof(HeightMax));
            OnPropertyChanged(nameof(HeightDifference));
        }

        void OnMapGridChanged(object sender, EventArgs e) {
            if (ignoreCoordChanges) return;
            ignoreCoordChanges = true;

            GeoLocation location = map.GridCenter;
            GridSettings.CoordinateX = location.Longitude;
            GridSettings.CoordinateY = location.Latitude;
            ignoreCoordChanges = false;
            generator.InvalidateCache();
        }

        void OnUIChanged(object sender, PropertyChangedEventArgs e) {
            Valid = GridSettings.Validate();

            if (e.PropertyName == nameof(GridSettings.CoordinateX) || e.PropertyName == nameof(GridSettings.CoordinateY)) {
                OnGridCoordsChanged();
                InvalidateCache();
            } else if (e.PropertyName == nameof(GridSettings.GridSize) || e.PropertyName == nameof(GridSettings.TileCount) || e.PropertyName == nameof(GridSettings.OutputSize)) {
                OnGridSizeChanged();
                OnOutputSizeChanged();
                InvalidateCache();
            } else if (e.PropertyName == nameof(GridSettings.HeightMin) || e.PropertyName == nameof(GridSettings.HeightMax)) {
                OnCustomHeightChanged();
            }

            LoadedFileChanged = true;
            mainWindow.UpdateTitle();
        }

        void ValidateAll() {
            Valid = GridSettings.Validate();
            OnGridCoordsChanged();
            OnGridSizeChanged();
            OnOutputSizeChanged();
            OnCustomHeightChanged();
        }

        void OnGridSizeChanged() {
            if (GridSettings.GridSize <= 0) {
                GridSizeRow.Background = Brushes.Red;
                return;
            }

            GridSizeRow.Background = null;
        }

        void OnGridCoordsChanged() {
            if (ignoreCoordChanges) return;
            ignoreCoordChanges = true;

            GeoLocation location = new GeoLocation(GridSettings.CoordinateY, GridSettings.CoordinateX);
            map.GridCenter = location;
            ignoreCoordChanges = false;
        }

        void OnOutputSizeChanged() {
            if (GridSettings.TileCount < 1) {
                TileCountRow.Background = Brushes.Red;
                TileSizeRow.Background = null;
                return;
            }

            TileCountRow.Background = null;

            map.SetGridSize(GridSettings.GridSize, GridSettings.TileCount);

            int tileSize = GridSettings.OutputSize / GridSettings.TileCount;
            int remainder = GridSettings.OutputSize % GridSettings.TileCount;

            GridSettings.TileSize = tileSize;

            if (remainder != 0) {
                TileSizeRow.Background = Brushes.Red;
            } else {
                TileSizeRow.Background = null;
            }
        }

        void OnCustomHeightChanged() {
            if (GridSettings.HeightMin == 0 && GridSettings.HeightMax == 0) {
                CustomHeightMinRow.Background = null;
                CustomHeightMaxRow.Background = null;
                GridSettings.HeightDifference = 0;
                return;
            }

            GridSettings.HeightDifference = GridSettings.HeightMax - GridSettings.HeightMin;

            if (GridSettings.HeightDifference < 0) {
                CustomHeightMinRow.Background = Brushes.Red;
                CustomHeightMaxRow.Background = Brushes.Red;
            } else {
                CustomHeightMinRow.Background = null;
                CustomHeightMaxRow.Background = null;
            }
        }

        void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }
    }
}
