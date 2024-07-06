using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public struct ColorDouble {
        public double r;
        public double g;
        public double b;
        public double a;

        public ColorDouble(double r, double g, double b, double a) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public ColorDouble(Color color) {
            r = color.r / 255f;
            g = color.g / 255f;
            b = color.b / 255f;
            a = color.a / 255f;
        }

        public static ColorDouble operator * (ColorDouble a, double b) {
            return new ColorDouble(
                a.r * b,
                a.g * b,
                a.b * b,
                a.a * b
            );
        }

        public static ColorDouble operator *(double a, ColorDouble b) {
            return b * a;
        }

        public static ColorDouble operator + (ColorDouble a, ColorDouble b) {
            return new ColorDouble(
                a.r + b.r,
                a.g + b.g,
                a.b + b.b,
                a.a + b.a
            );
        }

        public static explicit operator Color(ColorDouble a) {
            return new Color(
                (byte)Utility.Clamp(a.r * 255, 0, 255),
                (byte)Utility.Clamp(a.g * 255, 0, 255),
                (byte)Utility.Clamp(a.b * 255, 0, 255),
                (byte)Utility.Clamp(a.a * 255, 0, 255)
            );
        }
    }
}
