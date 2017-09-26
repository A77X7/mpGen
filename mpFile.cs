using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mpGen
{
    class mpFile
    {
        public mpFile()
        {
            header = new header();
            geometries = new List<geometry>();
        }
        public static String lineSeparator = "\r\n";
        public header header;
        public List<geometry> geometries;
        public override string ToString()
        {
            var s = header.ToString() + lineSeparator + String.Join(lineSeparator, geometries);
            return s;
        }
    }
}
