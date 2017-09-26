using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace mpGen
{
    class coordinate
    {
        static NumberFormatInfo nfi;
        static coordinate()
        {
            nfi = new NumberFormatInfo();
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalSeparator = ".";
        }
        public coordinate() : this(0, 0) { }
        public coordinate(Double xx, Double yy)
        {
            x = xx;
            y = yy;
        }
        public Double x;
        public Double y;
        public override string ToString()
        {
            var s = "";
            s = "(" + y.ToString("F5", nfi) + "," + x.ToString("F5", nfi) + ")";
            return s;
        }
    }
}
