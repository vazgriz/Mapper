using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapper {
    class Sampler<T> where T : struct{
        Image<T> source;
        int sizeX;
        int sizeY;

        public bool FlipVertically { get; set; }

        public Sampler(Image<T> source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            this.source = source;
            sizeX = source.Width - 1;
            sizeY = source.Height - 1;
        }

        public T Sample(Point pos) {
            if (pos.X < 0 || pos.X > 1) throw new ArgumentOutOfRangeException(nameof(pos));
            if (pos.Y < 0 || pos.Y > 1) throw new ArgumentOutOfRangeException(nameof(pos));

            int x = (int)Math.Round(pos.X * sizeX);
            int y;

            if (FlipVertically) {
                y = (int)Math.Round((1 - pos.Y) * sizeY);
            } else {
                y = (int)Math.Round(pos.Y * sizeY);
            }

            return source[new PointInt(x, y)];
        }
    }
}
