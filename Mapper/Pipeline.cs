using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    class Pipeline {
        public float NormalizeMin { get; set; }
        public float NormalizeMax { get; set; }
        public bool ApplyWaterOffset { get; set; }
        public float WaterOffset { get; set; }

        public void Process(Image<float> sourceData, Image<ushort> output, PointInt sourcePos, PointInt outputPos) {

        }
    }
}
