using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public class ImageGroup<T> : IEnumerable<PointInt> where T : struct {
        int tileCount;
        int tileSize;
        List<Image<T>> images;

        public int TileCount {
            get {
                return tileCount;
            }
        }

        public int TileSize {
            get {
                return tileSize;
            }
        }

        public ImageGroup(int tileCount, int tileSize) {
            this.tileCount = tileCount;
            this.tileSize = tileSize;

            images = new List<Image<T>>(tileCount * tileCount);

            for (int y = 0; y < tileCount; y++) {
                for (int x = 0; x < tileCount; x++) {
                    images.Add(new Image<T>(tileSize, tileSize));
                }
            }
        }

        public Image<T> this[PointInt pos] {
            get {
                return images[GetIndex(pos)];
            }
        }

        public int GetIndex(PointInt pos) {
            return (pos.y * tileCount) + pos.x;
        }

        public PointInt GetPoint(int index) {
            if (index < 0 || index > TileCount * TileCount) throw new ArgumentOutOfRangeException(nameof(index));
            int x = index % TileCount;
            int y = index / TileCount;
            return new PointInt(x, y);
        }

        public IEnumerator<PointInt> GetEnumerator() {
            return new ImageGroupIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        public struct ImageGroupIterator : IEnumerator<PointInt> {
            ImageGroup<T> group;
            int index;
            int max;

            public ImageGroupIterator(ImageGroup<T> group) {
                this.group = group;
                index = -1;
                max = group.TileCount * group.TileCount;
            }

            PointInt IEnumerator<PointInt>.Current {
                get {
                    return group.GetPoint(index);
                }
            }

            object IEnumerator.Current {
                get {
                    return group.GetPoint(index);
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
