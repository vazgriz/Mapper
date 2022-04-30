using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapper {
    public enum EdgeBehaviorType {
        Error,
        Clamp
    }

    public enum FilteringType {
        Point,
        Linear
    }

    class Sampler<T> where T : struct {

        Image<T> source;
        int sizeX;
        int sizeY;

        public bool FlipVertically { get; set; }
        public EdgeBehaviorType EdgeBehavior { get; set; }
        public FilteringType Filtering { get; set; }

        public Sampler(Image<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            this.source = source;
            sizeX = source.Width - 1;
            sizeY = source.Height - 1;
        }

        public T Sample(Point pos) {
            if (pos.X < 0 || pos.X > 1) {
                if (EdgeBehavior == EdgeBehaviorType.Clamp) {
                    pos.X = Utility.Clamp(pos.X, 0, 1);
                } else {
                    throw new ArgumentOutOfRangeException(nameof(pos));
                }
            }

            if (pos.Y < 0 || pos.Y > 1) {
                if (EdgeBehavior == EdgeBehaviorType.Clamp) {
                    pos.Y = Utility.Clamp(pos.Y, 0, 1);
                } else {
                    throw new ArgumentOutOfRangeException(nameof(pos));
                }
            }

            if (FlipVertically) {
                pos.Y = 1 - pos.Y;
            }

            if (Filtering == FilteringType.Linear) {
                return LinearFilter(pos);
            } else {
                return SampleNormalized(pos);
            }
        }

        (double, double) GetFilterPoints(double value) {
            double min = Math.Floor(value);
            double max;
            if (min == value) {
                max = min + 1;
            } else {
                max = Math.Ceiling(value);
            }

            return (min, max);
        }

        T LinearFilter(Point pos) {
            pos.X *= sizeX;
            pos.Y *= sizeY;

            var (xMin, xMax) = GetFilterPoints(pos.X);
            var (yMin, yMax) = GetFilterPoints(pos.Y);

            var xT = Utility.InverseLerp(xMin, xMax, pos.X);
            var yT = Utility.InverseLerp(yMin, yMax, pos.Y);

            if (xMax > sizeX) xMax = sizeX;
            if (yMax > sizeY) yMax = sizeY;

            //get sample points
            //use dynamic since this version of C#
            //doesn't support INumber<T>
            dynamic p1 = SampleDirect(new Point(xMin, yMin));
            dynamic p2 = SampleDirect(new Point(xMax, yMin));
            dynamic p3 = SampleDirect(new Point(xMin, yMax));
            dynamic p4 = SampleDirect(new Point(xMax, yMax));

            dynamic intermediate1 = ((1 - xT) * p1) + (xT * p2);
            dynamic intermediate2 = ((1 - xT) * p3) + (xT * p4);

            return (T)(((1 - yT) * intermediate1) + (yT * intermediate2));
        }

        T SampleDirect(Point pos) {
            int x = (int)Math.Round(pos.X);
            int y = (int)Math.Round(pos.Y);
            return source[new PointInt(x, y)];
        }

        T SampleNormalized(Point pos) {
            int x = (int)Math.Round(pos.X * sizeX);
            int y = (int)Math.Round(pos.Y * sizeY);
            return source[new PointInt(x, y)];
        }
    }
}
