﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper {
    public class OutputMapData {
        public ImageGroup<ushort> HeightData { get; set; }
        public ImageGroup<Color> SatelliteData { get; set; }
    }
}
