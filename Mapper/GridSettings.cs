using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace Mapper {
    public class GridSettings : INotifyPropertyChanged {
        const double coordXDefault = 0;
        const double coordYDefault = 0;
        const double gridSizeDefault = 20;
        const int outputSizeDefault = 1024;
        const int tileSizeDefault = 1024;
        const int tileCountDefault = 1;
        const float heightMinDefault = 0;
        const float heightMaxDefault = 0;
        const float heightDifferenceDefault = 0;
        const bool flipOutputDefault = true;
        const bool downloadSatelliteImagesDefault = false;
        const bool applyWaterOffsetDefault = false;
        const float waterOffsetDefault = 0;
        const bool forceZipExportDefault = false;

        double coordX = coordXDefault;
        double coordY = coordYDefault;
        double gridSize = gridSizeDefault;
        int outputSize = outputSizeDefault;
        int tileSize = tileSizeDefault;
        int tileCount = tileCountDefault;
        float heightMin = heightMinDefault;
        float heightMax = heightMaxDefault;
        float heightDifference = heightDifferenceDefault;
        bool flipOutput = flipOutputDefault;
        bool downloadSatelliteImages = downloadSatelliteImagesDefault;
        bool applyWaterOffset = applyWaterOffsetDefault;
        float waterOffset = waterOffsetDefault;
        bool forceZipExport = forceZipExportDefault;

        public Version Version { get; set; }    //not used by XAML

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

        public bool DownloadSatelliteImages {
            get {
                return downloadSatelliteImages;
            }
            set {
                downloadSatelliteImages = value;
                OnPropertyChanged(nameof(DownloadSatelliteImages));
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

        public bool ForceZipExport {
            get {
                return forceZipExport;
            }
            set {
                forceZipExport = value;
                OnPropertyChanged(nameof(ForceZipExport));
            }
        }

        public GridSettings() {
            Version = Version.CurrentVersion;
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

        int GetOrDefault(JObject obj, string name, int defaultValue) {
            if (obj.TryGetValue(name, out var value)) {
                return (int)value;
            } else {
                return defaultValue;
            }
        }

        double GetOrDefault(JObject obj, string name, double defaultValue) {
            if (obj.TryGetValue(name, out var value)) {
                return (double)value;
            } else {
                return defaultValue;
            }
        }

        float GetOrDefault(JObject obj, string name, float defaultValue) {
            if (obj.TryGetValue(name, out var value)) {
                return (float)value;
            } else {
                return defaultValue;
            }
        }

        bool GetOrDefault(JObject obj, string name, bool defaultValue) {
            if (obj.TryGetValue(name, out var value)) {
                return (bool)value;
            } else {
                return defaultValue;
            }
        }

        public void CopyFrom(JObject other) {
            var versionString = (string)other[nameof(Version)];
            var otherVersion = Version.Parse(versionString);

            if (Version != otherVersion) {
                UpdateSettings(other);
            }

            CoordinateX = GetOrDefault(other, nameof(CoordinateX), coordXDefault);
            CoordinateY = GetOrDefault(other, nameof(CoordinateY), coordYDefault);
            GridSize = GetOrDefault(other, nameof(GridSize), gridSizeDefault);
            OutputSize = GetOrDefault(other, nameof(OutputSize), outputSizeDefault);
            TileSize = GetOrDefault(other, nameof(TileSize), tileSizeDefault);
            TileCount = GetOrDefault(other, nameof(TileCount), tileCountDefault);
            HeightMin = GetOrDefault(other, nameof(HeightMin), heightMinDefault);
            HeightMax = GetOrDefault(other, nameof(HeightMax), heightMaxDefault);
            HeightDifference = GetOrDefault(other, nameof(HeightDifference), heightDifferenceDefault);
            FlipOutput = GetOrDefault(other, nameof(FlipOutput), flipOutputDefault);
            DownloadSatelliteImages = GetOrDefault(other, nameof(DownloadSatelliteImages), downloadSatelliteImagesDefault);
            ApplyWaterOffset = GetOrDefault(other, nameof(ApplyWaterOffset), applyWaterOffsetDefault);
            WaterOffset = GetOrDefault(other, nameof(WaterOffset), waterOffsetDefault);
            ForceZipExport = GetOrDefault(other, nameof(ForceZipExport), forceZipExportDefault);
        }

        void UpdateSettings(JObject other) {
            //nothing
        }
    }
}
