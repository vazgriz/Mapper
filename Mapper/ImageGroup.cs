using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public class ImageGroup<T> : IEnumerable<PointInt>, IImage<T> where T : struct {
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

        int IImage<T>.Width {
            get {
                return tileCount * tileSize;
            }
        }

        int IImage<T>.Height {
            get {
                return tileCount * tileSize;
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

        public PointInt GetTilePoint(int index) {
            if (index < 0 || index > TileCount * TileCount) throw new ArgumentOutOfRangeException(nameof(index));
            int x = index % TileCount;
            int y = index / TileCount;
            return new PointInt(x, y);
        }

        public PointInt GetPoint(int index) {
            int totalSize = TileCount * TileSize;
            if (index < 0 || index > totalSize * totalSize) throw new ArgumentOutOfRangeException(nameof(index));
            int x = index % totalSize;
            int y = index / totalSize;
            return new PointInt(x, y);
        }

        public IEnumerator<PointInt> GetEnumerator() {
            return new ImageGroupIterator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        public T GetData(PointInt pos) {
            int tileX = pos.x / tileSize;
            int tileY = pos.y / tileSize;
            int tileLocalX = pos.x % tileSize;
            int tileLocalY = pos.y % tileSize;

            int tileIndex = tileX + tileY * tileCount;
            var tile = images[tileIndex];
            PointInt localPos = new PointInt(tileLocalX, tileLocalY);

            return tile[localPos];
        }

        public void SetData(PointInt pos, T data) {
            int tileX = pos.x / tileSize;
            int tileY = pos.y / tileSize;
            int tileLocalX = pos.x % tileSize;
            int tileLocalY = pos.y % tileSize;

            int tileIndex = tileX + tileY * tileCount;
            var tile = images[tileIndex];
            PointInt localPos = new PointInt(tileLocalX, tileLocalY);

            tile[localPos] = data;
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
                    return group.GetTilePoint(index);
                }
            }

            object IEnumerator.Current {
                get {
                    return group.GetTilePoint(index);
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
