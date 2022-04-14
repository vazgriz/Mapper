using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapper {
    class Sampler {
        Image source;
        int sizeX;
        int sizeY;

        public Sampler(Image source) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            this.source = source;
            sizeX = source.Width - 1;
            sizeY = source.Height - 1;
        }

        public float Sample(Point pos) {
            if (pos.X < 0 || pos.X > 1) throw new ArgumentOutOfRangeException(nameof(pos));
            if (pos.Y < 0 || pos.Y > 1) throw new ArgumentOutOfRangeException(nameof(pos));

            int x = (int)Math.Round(pos.X * sizeX);
            int y = (int)Math.Round(pos.Y * sizeY);

            return source[new PointInt(x, y)];
        }
    }
}
