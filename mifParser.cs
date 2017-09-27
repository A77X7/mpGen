using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace mpGen
{
    class mifParser
    {
        public static List<String> geometryBlockKeywords = new List<String>(new String[] { "NONE", "POINT", "LINE", "PLINE", "REGION", "ARC", "TEXT", "RECT", "ROUNDRECT", "ELLIPSE", "MULTIPOINT" });
        //public static String lineSepeartor = "\r\n";
        public static String delimiter = "\t";

        public static void parse(String fileNameSource, List<geometry> geometries)
        {
            using (var stream = new FileStream(fileNameSource, FileMode.Open, FileAccess.Read))
            {
                parse(stream, geometries);
            }
        }

        public static void parse(Stream streamSource, List<geometry> geometries)
        {
            using (var reader = new StreamReader(streamSource))
            {
                var s = null as String;
                var inDataSection = false;
                var lastGeometryBlock = new List<String>();
                UInt32 c = 0;
                while ((s = reader.ReadLine()) != null)
                {
                    c++;
                    s = s.Trim();
                    if (String.IsNullOrWhiteSpace(s))
                    {
                        ;
                    }
                    else if (inDataSection)
                    {
                        var keyword = s.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0].ToUpper();
                        if (geometryBlockKeywords.Contains(keyword))
                        {
                            if (lastGeometryBlock.Count > 0)
                            {
                                var geom = parseGeometryBlock(lastGeometryBlock, c.ToString());
                                if (geom != null)
                                {
                                    //foreach (var g in geom)
                                    //    g.reproject(null, null, null, "EPSG:28413", "EPSG:4326");
                                    //так долго
                                    lock (Program.mpLock)
                                    {
                                        geometries.AddRange(geom);
                                    }
                                }
                                lastGeometryBlock.Clear();
                            }
                        }
                        lastGeometryBlock.Add(s);
                    }
                    else if ("DATA".Equals(s.ToUpper()))
                    {
                        inDataSection = true;
                    }
                    else//read header
                    {
                        var words = s.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var keyword = words[0].ToUpper();
                        if ("DELIMITER".Equals(keyword) && words.Length >= 2)
                        {
                            delimiter = words[1].Trim('\"');
                        }
                    }
                }
                if (lastGeometryBlock.Count > 0)
                {
                    var geom = parseGeometryBlock(lastGeometryBlock, c.ToString());
                    if (geom != null)
                    {
                        //foreach (var g in geom)
                        //    g.reproject(null, null, null, "EPSG:28413", "EPSG:4326");
                        //так долго
                        lock (Program.mpLock)
                        {
                            geometries.AddRange(geom);
                        }
                    }
                    lastGeometryBlock.Clear();
                }
            }
        }

        public static List<geometry> parseGeometryBlock(List<String> text, String label)
        {
            var nfi = new NumberFormatInfo();
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalSeparator = ".";
            var ns = NumberStyles.Float;

            var retval = new List<geometry>();
            var words = text[0].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var keyword = words[0].ToUpper();
            if ("NONE".Equals(keyword))
            {
                ;
            }
            else if ("POINT".Equals(keyword))
            {
                Double x, y;
                if (words.Length == 1)
                {
                    words = text[1].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 2 && Double.TryParse(words[0], ns, nfi, out x) && Double.TryParse(words[1], ns, nfi, out y))
                    {
                        var g = new geometryPoint();
                        g.label = label;
                        g.coordinate[0] = new coordinate(x, y);
                        retval.Add(g);
                    }
                }
                else if (words.Length >= 3 && Double.TryParse(words[1], ns, nfi, out x) && Double.TryParse(words[2], ns, nfi, out y))
                {
                    var g = new geometryPoint();
                    g.coordinate[0] = new coordinate(x, y);
                    retval.Add(g);
                }
            }
            else if ("LINE".Equals(keyword))
            {
                Double x1, y1, x2, y2;
                if (words.Length == 1)
                {
                    words = text[1].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length >= 4 && Double.TryParse(words[0], ns, nfi, out x1) && Double.TryParse(words[1], ns, nfi, out y1) && Double.TryParse(words[2], ns, nfi, out x2) && Double.TryParse(words[3], ns, nfi, out y2))
                    {
                        var g = new geometryPolyline();
                        g.label = label;
                        g.coordinates[0] = new List<coordinate>();
                        g.coordinates[0].Add(new coordinate(x1, y1));
                        g.coordinates[0].Add(new coordinate(x2, y2));
                        retval.Add(g);
                    }
                }
                else if (words.Length >= 5 && Double.TryParse(words[1], ns, nfi, out x1) && Double.TryParse(words[2], ns, nfi, out y1) && Double.TryParse(words[3], ns, nfi, out x2) && Double.TryParse(words[4], ns, nfi, out y2))
                {
                    var g = new geometryPolyline();
                    g.label = label;
                    g.coordinates[0] = new List<coordinate>();
                    g.coordinates[0].Add(new coordinate(x1, y1));
                    g.coordinates[0].Add(new coordinate(x2, y2));
                    retval.Add(g);
                }
            }
            else if ("PLINE".Equals(keyword))
            {
                Boolean hasMULTIPLE = false;
                UInt32 mSections = 1;
                if (words.Length < 3 || !words[1].ToUpper().Equals("MULTIPLE") || !UInt32.TryParse(words[2], out mSections))
                {
                    mSections = 1;
                }
                else
                {
                    hasMULTIPLE = true;
                }
                int row = 1;
                for (UInt32 i = 0; i < mSections; i++)
                {
                    var g = new geometryPolyline();
                    g.label = label;
                    UInt32 numPts;
                    if (!hasMULTIPLE && words.Length >= 2 && UInt32.TryParse(words[1], out numPts))
                    {
                        ;
                    }
                    else if ((words = text[row].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)).Length >= 1 && UInt32.TryParse(words[0], out numPts))
                    {
                        row++;
                    }
                    else
                    {
                        numPts = 0;
                        row++;
                    }
                    if (numPts > 0)
                    {
                        g.coordinates[0] = new List<coordinate>();
                        for (UInt32 j = 0; j < numPts; j++)
                        {
                            Double x, y;
                            words = text[row].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length >= 2 && Double.TryParse(words[0], ns, nfi, out x) && Double.TryParse(words[1], ns, nfi, out y))
                            {
                                g.coordinates[0].Add(new coordinate(x, y));
                            }
                            row++;
                        }
                    }
                    if (g.coordinates[0].Count > 1)
                    {
                        retval.Add(g);
                    }
                }
            }
            else if ("REGION".Equals(keyword))
            {
                UInt32 nPolygons;
                if (words.Length >= 2 && UInt32.TryParse(words[1], out nPolygons))
                {
                    int row = 1;
                    var g = new geometryPolygon();
                    g.label = label;
                    retval.Add(g);
                    for (UInt32 i = 0; i < nPolygons; i++)
                    {
                        UInt32 numPts;
                        g.coordinates[0] = new List<List<coordinate>>();
                        words = text[row].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length >= 1 && UInt32.TryParse(words[0], out numPts))
                        {
                            g.coordinates[0].Add(new List<coordinate>());
                            row++;
                            for (UInt32 j = 0; j < numPts - 1; j++)
                            {
                                Double x, y;
                                words = text[row].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                if (words.Length >= 2 && Double.TryParse(words[0], ns, nfi, out x) && Double.TryParse(words[1], ns, nfi, out y))
                                {
                                    g.coordinates[0][g.coordinates[0].Count - 1].Add(new coordinate(x, y));
                                }
                                row++;
                            }
                        }
                        else
                        {
                            row++;
                        }
                    }
                }
            }
            else if ("TEXT".Equals(keyword))
            {
                var lbl = "";
                if (words.Length == 1)
                {
                    lbl = text[1].Trim().Trim('\"');
                    words = text[2].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (words.Length >= 2)
                {
                    lbl = text[0].Substring(4).Trim().Trim('\"');
                    words = text[1].Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                }
                Double x1, y1, x2, y2;
                if (words.Length >= 4 && Double.TryParse(words[0], ns, nfi, out x1) && Double.TryParse(words[1], ns, nfi, out y1) && Double.TryParse(words[2], ns, nfi, out x2) && Double.TryParse(words[3], ns, nfi, out y2))
                {
                    var g = new geometryPoint();
                    g.coordinate[0] = new coordinate((x2 - x1) / 2, (y2 - y1) / 2);
                    g.label = lbl + (label == null ? "" : " / " + label);
                    retval.Add(g);
                }
            }
            return retval;
        }

        static coordinate getTextCenter(Double x1, Double y1, Double x2, Double y2, Double angleDegrees)
        {
            var x = x1 + (x2 - x1) / 2;
            var y = y1 + (y2 - y1) / 2;

        }
    }
}
