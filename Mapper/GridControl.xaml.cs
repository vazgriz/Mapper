using MapboxNetCore;
using MapboxNetWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mapper {
    /// <summary>
    /// Interaction logic for GridControl.xaml
    /// </summary>
    public partial class GridControl : UserControl {
        public GridSettings GridSettings { get; private set; }
        public bool Valid { get; private set; }

        MainWindow mainWindow;
        Map map;
        bool ignoreCoordChanges;

        ProgressWindow progressWindow;

        public bool IsGenerating { get; private set; }

        public GridControl() {
            GridSettings = new GridSettings();
            InitializeComponent();
        }

        public void Init(MainWindow mainWindow, Map map) {
            this.mainWindow = mainWindow;
            this.map = map;
            map.GridCenterChanged += OnMapGridChanged;
            GridSettings.PropertyChanged += OnUIChanged;
        }

        public void LoadSettings(GridSettings gridSettings) {
            GridSettings.Copy(gridSettings);
            ValidateAll();

            if (Valid) {
                map.Ready += OnMapReady;
            }
        }

        void OnMapReady(object sender, EventArgs e) {
            map.SetGridSize(GridSettings.GridSize, GridSettings.TileCount);
        }

        void GenerateHeightMap(object sender, RoutedEventArgs e) {
            if (IsGenerating) return;

            Valid = GridSettings.Validate();
            if (!Valid) return;

            var generator = new Generator(mainWindow, this);
            var extent = mainWindow.Map.GetGridExtent(GridSettings.CoordinateX, GridSettings.CoordinateY, GridSettings.GridSize, GridSettings.OutputSize);

            IsGenerating = true;
            generator.Run(extent);
        }

        public ProgressWindow BeginGenerating(int steps) {
            progressWindow = new ProgressWindow();
            progressWindow.SetMaximum(steps);
            progressWindow.Owner = mainWindow;
            progressWindow.Show();
            return progressWindow;
        }

        public void FinishGenerating() {
            if (!IsGenerating) return;
            IsGenerating = false;
            progressWindow.Close();
            progressWindow = null;
        }

        void OnMapGridChanged(object sender, EventArgs e) {
            if (ignoreCoordChanges) return;
            ignoreCoordChanges = true;

            GeoLocation location = map.GridCenter;
            GridSettings.CoordinateX = location.Longitude;
            GridSettings.CoordinateY = location.Latitude;
            ignoreCoordChanges = false;
        }

        void OnUIChanged(object sender, PropertyChangedEventArgs e) {
            Valid = GridSettings.Validate();

            if (e.PropertyName == nameof(GridSettings.CoordinateX) || e.PropertyName == nameof(GridSettings.CoordinateY)) {
                OnGridCoordsChanged();
            } else if (e.PropertyName == nameof(GridSettings.GridSize) || e.PropertyName == nameof(GridSettings.TileCount) || e.PropertyName == nameof(GridSettings.OutputSize)) {
                OnGridSizeChanged();
                OnOutputSizeChanged();
            }
        }

        void ValidateAll() {
            Valid = GridSettings.Validate();
            OnGridCoordsChanged();
            OnGridSizeChanged();
            OnOutputSizeChanged();
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

        void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }
    }
}
