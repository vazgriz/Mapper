using System;
using System.Collections.Generic;
using System.Text;

namespace MapboxNetCore
{
    public class GeoExtent
    {
        public GeoLocation TopLeft { get; set; }
        public GeoLocation BottomRight { get; set; }
    }
}
