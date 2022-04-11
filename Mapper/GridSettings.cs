using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public class GridSettings : INotifyPropertyChanged {
        double coordX;
        double coordY;

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

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
