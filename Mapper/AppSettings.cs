using System;
using System.Collections.Generic;
using System.ComponentModel;
using MapboxNetCore;

namespace Mapper {
    public class AppSettings : INotifyPropertyChanged {
        string apiKey = "";
        GeoLocation coordinates = new GeoLocation();
        double zoom = 5;
        bool allowRotation = true;

        public string APIKey {
            get {
                return apiKey;
            }
            set {
                apiKey = value;
                OnPropertyChanged(nameof(APIKey));
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

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Validate() {
            if (APIKey == null) {
                APIKey = "";
            }

            if (Coordinates == null) {
                Coordinates = new GeoLocation();
            }
        }
    }
}
