using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mpGen
{
    class geometryPoint : geometry
    {
        public geometryPoint()
        {
            type = 0x01;
            subType = 0x00;
            coordinate = new Dictionary<UInt32, coordinate>();
            isCity = false;
        }
        public Dictionary<UInt32, coordinate> coordinate;
        public Boolean isCity;
        public override string ToString()
        {
            var s = "";
            s += (isCity ? "[RGN20]" : "[RGN10]") + mpFile.lineSeparator;
            s += "Type=0x" + type.ToString("X2") + mpFile.lineSeparator;
            if (subType.HasValue)
                s += "SubType=0x" + subType.Value.ToString("X2") + mpFile.lineSeparator;
            if (label != null)
                s += "Label=" + label + mpFile.lineSeparator;
            //s += "City=" + (isCity ? "Y" : "N") + mpFile.lineSeparator;
            foreach (var pair in coordinate)
            {
                s += "Data" + pair.Key.ToString() + "=" + pair.Value.ToString() + mpFile.lineSeparator;
            }
            s += "[END]" + mpFile.lineSeparator;
            return s;
        }
        override public void reprojectByGeoserver(String urlGeoserverWps, String user, String password, String srsSource, String srsTarget)
        {
            foreach (var c in coordinate)
                reprojectInplaceByGeoserver(urlGeoserverWps, user, password, srsSource, srsTarget, new coordinate[] { c.Value });
        }
    }
}
