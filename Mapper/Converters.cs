using System;
using System.Globalization;
using MapboxNetCore;

namespace Mapper {
    public class GeoLocationDisplayConverter : System.Windows.Data.IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var location = (GeoLocation)value;

            if (location == null) return "ERROR";

            return string.Format("{0:0.000}, {1:0.000}", location.Latitude, location.Longitude);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
