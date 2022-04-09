using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MapboxNetCore
{
    [TypeConverter(typeof(GeoLocationTypeConverter))]
    public class GeoLocation
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public GeoLocation()
        {
        }

        public GeoLocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return "(" + Latitude + ", " + Longitude + ")";
        }
    }

    public class GeoLocationTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture, object value)
        {
            var text = (string)value;
            text = text.Substring(1, text.Length - 2);
            var parts = text.Split(',').Select(p => Convert.ToDouble(p)).ToArray();
            return new GeoLocation(parts[0], parts[1]);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
            Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            return value == null ? null : value.ToString();
        }
    }

    public class GeoLocationDisplayConverter : IValueConverter {
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
