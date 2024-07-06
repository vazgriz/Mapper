using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public struct Color {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color(byte r, byte g, byte b, byte a) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static ColorDouble operator * (Color a, double b) {
            ColorDouble c = new ColorDouble(a);
            return c * b;
        }

        public static ColorDouble operator *(double a, Color b) {
            return b * a;
        }
    }
}
