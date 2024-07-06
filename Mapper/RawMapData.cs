using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public class RawMapData {
        public ImageGroup<float> HeightData { get; set; }
        public ImageGroup<float> WaterData { get; set; }
        public ImageGroup<Color> SatelliteData { get; set; }
    }
}
