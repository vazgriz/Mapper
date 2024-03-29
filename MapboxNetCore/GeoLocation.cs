﻿using System;
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

        public static GeoLocation Parse(string text) {
            text = text.Substring(1, text.Length - 2);
            var parts = text.Split(',').Select(p => Convert.ToDouble(p)).ToArray();
            return new GeoLocation(parts[0], parts[1]);
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
            return GeoLocation.Parse(text);
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
}
