using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public struct PointInt {
        public int x;
        public int y;

        public PointInt(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public static PointInt operator + (PointInt a, PointInt b) {
            return new PointInt(a.x + b.x, a.y + b.y);
        }

        public static PointInt operator - (PointInt a, PointInt b) {
            return new PointInt(a.x - b.x, a.y - b.y);
        }

        public static PointInt operator * (PointInt a, int b) {
            return new PointInt(a.x / b, a.y / b);
        }
    }

    public class Image : IEnumerable<PointInt> {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float[] Data { get; private set; }

        public Image(int width, int height) {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            Data = new float[width * height];
        }

        public float this[PointInt pos] {
            get {
                return Data[GetIndex(pos)];
            }
            set {
                Data[GetIndex(pos)] = value;
            }
        }

        public int GetIndex(PointInt pos) {
            if (pos.x < 0 || pos.x > Width) throw new ArgumentOutOfRangeException(nameof(pos));
            if (pos.y < 0 || pos.y > Height) throw new ArgumentOutOfRangeException(nameof(pos));

            return pos.x + pos.y * Width;
        }

        public PointInt GetPoint(int index) {
            if (index < 0 || index > Width * Height) throw new ArgumentOutOfRangeException(nameof(index));
            int x = index % Width;
            int y = index / Width;
            return new PointInt(x, y);
        }

        public IEnumerator<PointInt> GetEnumerator() {
            return new ImageIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        public struct ImageIterator : IEnumerator<PointInt> {
            Image image;
            int index;
            int max;

            public ImageIterator(Image image) {
                this.image = image;
                index = 0;
                max = image.Width * image.Height;
            }

            PointInt IEnumerator<PointInt>.Current {
                get {
                    return image.GetPoint(index);
                }
            }

            object IEnumerator.Current {
                get {
                    return image.GetPoint(index);
                }
            }

            public void Dispose() {
                //nothing
            }

            public bool MoveNext() {
                index++;
                return index < max;
            }

            public void Reset() {
                index = 0;
            }
        }
    }
}
