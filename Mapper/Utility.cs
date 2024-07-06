using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    class Utility {
        public static double Clamp(double value, double min, double max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }
        public static float Clamp(float value, float min, float max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }

        public static double Lerp(double a, double b, double t) {
            return a + (b - a) * Clamp(t, 0, 1);
        }

        public static double InverseLerp(double a, double b, double value) {
            return Clamp((value - a) / (b - a), 0, 1);
        }
    }
}
