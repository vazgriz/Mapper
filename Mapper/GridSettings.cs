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
        float heightMin;
        float heightMax;
        float heightDifference;
        bool flipOutput;
        bool applyWaterOffset;
        float waterOffset;

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

        public float HeightMin {
            get {
                return heightMin;
            }
            set {
                heightMin = value;
                OnPropertyChanged(nameof(HeightMin));
            }
        }

        public float HeightMax {
            get {
                return heightMax;
            }
            set {
                heightMax = value;
                OnPropertyChanged(nameof(HeightMax));
            }
        }

        public float HeightDifference {
            get {
                return heightDifference;
            }
            set {
                heightDifference = value;
                OnPropertyChanged(nameof(HeightDifference));
            }
        }

        public bool FlipOutput {
            get {
                return flipOutput;
            }
            set {
                flipOutput = value;
                OnPropertyChanged(nameof(FlipOutput));
            }
        }

        public bool ApplyWaterOffset {
            get {
                return applyWaterOffset;
            }
            set {
                applyWaterOffset = value;
                OnPropertyChanged(nameof(ApplyWaterOffset));
            }
        }

        public float WaterOffset {
            get {
                return waterOffset;
            }
            set {
                waterOffset = value;
                OnPropertyChanged(nameof(WaterOffset));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Validate() {
            bool customHeightValid = true;
            if (HeightMin != 0 || HeightMin != 0) {
                customHeightValid = HeightMax >= HeightMin;
            }
            return GridSize > 0 && OutputSize >= 256 && TileCount > 0 && customHeightValid;
        }

        public void Copy(GridSettings other) {
            CoordinateX = other.CoordinateX;
            CoordinateY = other.CoordinateY;
            GridSize = other.GridSize;
            OutputSize = other.OutputSize;
            TileSize = other.TileSize;
            TileCount = other.TileCount;
            HeightMin = other.HeightMin;
            HeightMax = other.HeightMax;
            HeightDifference = other.HeightDifference;
            FlipOutput = other.FlipOutput;
            ApplyWaterOffset = other.ApplyWaterOffset;
            WaterOffset = other.WaterOffset;
        }
    }
}
