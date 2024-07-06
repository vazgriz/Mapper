using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public interface IImage<T> {
        T GetData(PointInt pos);
        void SetData(PointInt pos, T data);
        int Width { get; }
        int Height { get; }
    }
}
