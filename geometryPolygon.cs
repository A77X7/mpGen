using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mpGen
{
    class geometryPolygon : geometry
    {
        public geometryPolygon()
        {
            type = 0x01;
            subType = null;
            coordinates = new Dictionary<uint, List<List<coordinate>>>();
        }
        public Dictionary<UInt32, List<List<coordinate>>> coordinates;
        public override string ToString()
        {
            var s = "";
            s += "[RGN80]" + mpFile.lineSeparator;
            s += "Type=0x" + type.ToString("X2") + mpFile.lineSeparator;
            if (subType.HasValue)
                s += "SubType=0x" + subType.Value.ToString("X2") + mpFile.lineSeparator;
            if (label != null)
                s += "Label=" + label + mpFile.lineSeparator;
            foreach (var pair in coordinates)
            {
                if (pair.Value != null)
                    foreach (var item in pair.Value)
                    {
                        if (pair.Value != null)
                            s += "Data" + pair.Key.ToString() + "=" + String.Join(",", item) + mpFile.lineSeparator;
                    }
            }
            s += "[END]" + mpFile.lineSeparator;
            return s;
        }
        override public void reprojectByGeoserver(String urlGeoserverWps, String user, String password, String srsSource, String srsTarget)
        {
            foreach (var c in coordinates)
                foreach (var cc in c.Value)
                    reprojectInplaceByGeoserver(urlGeoserverWps, user, password, srsSource, srsTarget, cc);
        }
    }
}
