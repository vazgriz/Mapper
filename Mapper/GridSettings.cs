using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mapper {
    public class GridSettings : INotifyPropertyChanged {
        double coordX;
        double coordY;
        double gridSize = 20;
        int outputSize = 1024;
        int tileSize = 1024;
        int tileCount = 1;

        public double CoordinateX {
            get {
                return coordX;
            }
            set {
                coordX = value;
                OnPropertyChanged(nameof(CoordinateX));
            }
        }

        public double CoordinateY {
            get {
                return coordY;
            }
            set {
                coordY = value;
                OnPropertyChanged(nameof(CoordinateY));
            }
        }

        public double GridSize {
            get {
                return gridSize;
            }
            set {
                gridSize = value;
                OnPropertyChanged(nameof(GridSize));
            }
        }

        public int OutputSize {
            get {
                return outputSize;
            }
            set {
                outputSize = value;
                OnPropertyChanged(nameof(OutputSize));
            }
        }

        public int TileSize {
            get {
                return tileSize;
            }
            set {
                tileSize = value;
                OnPropertyChanged(nameof(TileSize));
            }
        }

        public int TileCount {
            get {
                return tileCount;
            }
            set {
                tileCount = value;
                OnPropertyChanged(nameof(TileCount));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Validate() {
            return GridSize > 0 && OutputSize >= 256 && TileCount > 0;
        }

        public void Copy(GridSettings other) {
            CoordinateX = other.CoordinateX;
            CoordinateY = other.CoordinateY;
            GridSize = other.GridSize;
            OutputSize = other.OutputSize;
            TileSize = other.TileSize;
            TileCount = other.TileCount;
        }
    }
}
