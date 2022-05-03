using System;
using System.Collections.Generic;
using System.ComponentModel;
using MapboxNetCore;
using Newtonsoft.Json.Linq;

namespace Mapper {
    public class AppSettings : INotifyPropertyChanged {
        string apiKey = "";
        string savePath = "";
        string lastFile = "";
        string exportPath = "";
        GeoLocation coordinates = new GeoLocation();
        double zoom = 5;
        bool allowRotation = true;
        bool debugMode = false;

        public Version Version { get; set; }

        public string APIKey {
            get {
                return apiKey;
            }
            set {
                apiKey = value;
                OnPropertyChanged(nameof(APIKey));
            }
        }

        public string SavePath {
            get {
                return savePath;
            }
            set {
                savePath = value;
                OnPropertyChanged(nameof(SavePath));
            }
        }

        public string LastFile {
            get {
                return lastFile;
            }
            set {
                lastFile = value;
                OnPropertyChanged(nameof(LastFile));
            }
        }

        public string ExportPath {
            get {
                return exportPath;
            }
            set {
                exportPath = value;
                OnPropertyChanged(nameof(ExportPath));
            }
        }

        public GeoLocation Coordinates {
            get {
                return coordinates;
            }
            set {
                coordinates = value;
                OnPropertyChanged(nameof(Coordinates));
            }
        }

        public double Zoom {
            get {
                return zoom;
            }
            set {
                zoom = value;
                OnPropertyChanged(nameof(Zoom));
            }
        }

        public bool AllowRotation {
            get {
                return allowRotation;
            }
            set {
                allowRotation = value;
                OnPropertyChanged(nameof(AllowRotation));
            }
        }

        public bool DebugMode {
            get {
                return debugMode;
            }
            set {
                debugMode = value;
                OnPropertyChanged(nameof(DebugMode));
            }
        }

        public AppSettings() {
            Version = Version.CurrentVersion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CopyFrom(JObject other) {
            var versionString = (string)other[nameof(Version)];
            var otherVersion = Version.Parse(versionString);

            if (Version != otherVersion) {
                UpdateSettings(other);
            }

            APIKey = (string)other[nameof(APIKey)];
            SavePath = (string)other[nameof(SavePath)];
            LastFile = (string)other[nameof(LastFile)];
            ExportPath = (string)other[nameof(ExportPath)];
            Coordinates = GeoLocation.Parse((string)other[nameof(Coordinates)]);
            Zoom = (double)other[nameof(Zoom)];
            AllowRotation = (bool)other[nameof(AllowRotation)];
            DebugMode = (bool)other[nameof(DebugMode)];
        }

        public void Validate() {
            if (APIKey == null) {
                APIKey = "";
            }

            if (Coordinates == null) {
                Coordinates = new GeoLocation();
            }

            if (LastFile == null) {
                LastFile = "";
            }
        }

        void UpdateSettings(JObject settings) {
            //nothing
        }
    }
}
