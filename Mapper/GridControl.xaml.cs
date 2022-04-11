using MapboxNetCore;
using MapboxNetWPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Mapper {
    /// <summary>
    /// Interaction logic for GridControl.xaml
    /// </summary>
    public partial class GridControl : UserControl {
        public GridSettings GridSettings { get; private set; }

        Map map;
        bool ignoreCoordChanges;

        public GridControl() {
            GridSettings = new GridSettings();
            InitializeComponent();
        }

        public void Init(Map map) {
            this.map = map;
            map.GridCenterChanged += OnMapGridChanged;
            GridSettings.PropertyChanged += OnUIChanged;
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
            if (e.PropertyName == nameof(GridSettings.CoordinateX) || e.PropertyName == nameof(GridSettings.CoordinateY)) {
                OnGridCoordsChanged();
            } else if (e.PropertyName == nameof(GridSettings.GridSize) || e.PropertyName == nameof(GridSettings.TileCount)) {
                OnGridSizeChanged();
            }
        }

        void OnGridCoordsChanged() {
            if (ignoreCoordChanges) return;
            ignoreCoordChanges = true;

            GeoLocation location = new GeoLocation(GridSettings.CoordinateY, GridSettings.CoordinateX);
            map.GridCenter = location;
            ignoreCoordChanges = false;
        }

        void OnGridSizeChanged() {
            map.SetGridSize(GridSettings.GridSize, GridSettings.TileCount);
        }

        void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox textBox = (TextBox)sender;
            textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }
    }
}
