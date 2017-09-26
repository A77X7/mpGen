using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.Globalization;

namespace mpGen
{
    class pgParser
    {
        public static void parse(String connectionString, String query, String fieldNameGeometryWkt, String fieldNameLabel, mpFile destination)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
                connectionString = "DSN=PostgreSQL35W;HOST=gis1;DB=testgis1;UID=postgres;PWD=!234werty;PORT=5432;";//PostgreSQL Unicode(x64)
            using (OdbcConnection odbcConnection = new OdbcConnection(connectionString))
            {
                odbcConnection.Open();
                using (var cmd = odbcConnection.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = System.Data.CommandType.Text;
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var wkt = dr[fieldNameGeometryWkt].ToString();
                            var lbl = String.IsNullOrWhiteSpace(fieldNameLabel) ? null : dr[fieldNameLabel].ToString();
                            var gg = parseWkt(wkt, lbl);
                            lock (Program.mpLock)
                            {
                                destination.geometries.AddRange(gg);
                            }
                        }
                    }
                }
            }
        }

        public static List<geometry> parseWkt(String wkt, String lbl)
        {
            var retval = new List<geometry>();

            var nfi = new NumberFormatInfo();
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalSeparator = ".";
            var ns = NumberStyles.Float;

            wkt = wkt.Replace("\t", " ").Replace("\r\n", " ").Replace("\n\r", " ").Replace("\r", " ").Replace("\n", " ").Replace("\r\n", " ").Replace("\n\r", " ").ToUpper();
            while (wkt.Contains("  "))
                wkt = wkt.Replace("  ", " ");
            wkt = wkt.Trim();

            var i = wkt.IndexOfAny(new Char[] { '(', ' ' });
            var geomType = wkt.Substring(0, i);
            wkt = wkt.Substring(i).Trim();
            if (!wkt.ToUpper().Contains("EMPTY"))
            {
                if ("POINT".Equals(geomType))
                {
                    wkt = wkt.TrimStart('(').TrimEnd(')').Trim();
                    var xy = wkt.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Double x, y;
                    if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                    {
                        var g = new geometryPoint();
                        g.label = lbl;
                        g.coordinate[0] = new coordinate(x, y);
                        retval.Add(g);
                    }
                }
                else if ("MULTIPOINT".Equals(geomType))
                {
                    wkt = wkt.TrimStart('(').TrimEnd(')').Trim();
                    var pts = wkt.Split(',');
                    foreach (var pt in pts)
                    {
                        var xy = pt.Trim().TrimStart('(').TrimEnd(')').Trim().Split(' ');
                        Double x, y;
                        if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                        {
                            var g = new geometryPoint();
                            g.label = lbl;
                            g.coordinate[0] = new coordinate(x, y);
                            retval.Add(g);
                        }
                    }
                }
                else if ("LINESTRING".Equals(geomType))
                {
                    var g = new geometryPolyline();
                    g.coordinates[0] = new List<coordinate>();
                    wkt = wkt.TrimStart('(').TrimEnd(')').Trim();
                    var pts = wkt.Split(',');
                    foreach (var pt in pts)
                    {
                        var xy = pt.Trim().Split(' ');
                        Double x, y;
                        if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                        {
                            g.coordinates[0].Add(new coordinate(x, y));
                        }
                    }
                    if (g.coordinates.Count > 1)
                    {
                        g.label = lbl;
                        retval.Add(g);
                    }
                }
                else if ("MULTILINESTRING".Equals(geomType))
                {
                    wkt = wkt.TrimStart('(').TrimEnd(')').Trim();
                    var parensOpen = new List<int>();
                    var parensClose = new List<int>();
                    int tmp = -1;
                    while ((tmp = wkt.IndexOf('(', tmp + 1)) >= 0)
                        parensOpen.Add(tmp);
                    tmp = -1;
                    while ((tmp = wkt.IndexOf(')', tmp + 1)) >= 0)
                        parensClose.Add(tmp);
                    tmp = Math.Min(parensOpen.Count, parensClose.Count);
                    for (int j = 0; j < tmp; j++)
                    {
                        if (parensOpen[j] > parensClose[j])
                            break;
                        var pts = wkt.Substring(parensOpen[j] + 1, parensClose[j] - parensOpen[j] - 1).Trim().Split(',');
                        var g = new geometryPolyline();
                        g.coordinates[0] = new List<coordinate>();
                        foreach (var pt in pts)
                        {
                            var xy = pt.Trim().Split(' ');
                            Double x, y;
                            if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                            {
                                g.coordinates[0].Add(new coordinate(x, y));
                            }
                        }
                        if (g.coordinates.Count > 1)
                        {
                            g.label = lbl;
                            retval.Add(g);
                        }
                    }
                }
                else if ("POLYGON".Equals(geomType))
                {
                    var g = new geometryPolygon();
                    g.coordinates[0] = new List<List<coordinate>>();
                    wkt = wkt.Substring(1, wkt.Length - 2).Trim();
                    var parensOpen = new List<int>();
                    var parensClose = new List<int>();
                    int tmp = -1;
                    while ((tmp + 1) < wkt.Length && (tmp = wkt.IndexOf('(', tmp + 1)) >= 0)
                        parensOpen.Add(tmp);
                    tmp = -1;
                    while ((tmp + 1) < wkt.Length && (tmp = wkt.IndexOf(')', tmp + 1)) >= 0)
                        parensClose.Add(tmp);
                    tmp = Math.Min(parensOpen.Count, parensClose.Count);
                    for (int j = 0; j < tmp; j++)
                    {
                        var cc = new List<coordinate>();
                        if (parensOpen[j] > parensClose[j])
                            break;
                        var pts = wkt.Substring(parensOpen[j] + 1, parensClose[j] - parensOpen[j] - 1).Trim().Split(',');
                        foreach (var pt in pts)
                        {
                            var xy = pt.Trim().Split(' ');
                            Double x, y;
                            if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                            {
                                cc.Add(new coordinate(x, y));
                            }
                        }
                        if (cc.Count > 2)
                        {
                            g.coordinates[0].Add(cc);
                        }
                    }
                    if (g.coordinates[0].Count > 0)
                    {
                        g.label = lbl;
                        retval.Add(g);
                    }
                }
                else if ("MULTIPOLYGON".Equals(geomType))
                {
                    wkt = wkt.Replace("( (", "((").Replace(") )", "))");
                    wkt = wkt.Substring(1, wkt.Length - 2).Trim();
                    var doubleParensOpen = new List<int>();
                    var doubleParensClose = new List<int>();
                    int doubleTmp = -1;
                    while ((doubleTmp + 1) < wkt.Length && (doubleTmp = wkt.IndexOf("((", doubleTmp + 1)) >= 0)
                        doubleParensOpen.Add(doubleTmp);
                    doubleTmp = -1;
                    while ((doubleTmp + 1) < wkt.Length && (doubleTmp = wkt.IndexOf("))", doubleTmp + 1)) >= 0)
                        doubleParensClose.Add(doubleTmp);
                    doubleTmp = Math.Min(doubleParensOpen.Count, doubleParensClose.Count);
                    for (int doubleJ = 0; doubleJ < doubleTmp; doubleJ++)
                    {
                        var g = new geometryPolygon();
                        g.coordinates[0] = new List<List<coordinate>>();
                        var wkt2 = wkt.Substring(1, wkt.Length - 2).Trim();
                        var parensOpen = new List<int>();
                        var parensClose = new List<int>();
                        int tmp = -1;
                        while ((tmp + 1) < wkt2.Length && (tmp = wkt2.IndexOf('(', tmp + 1)) >= 0)
                            parensOpen.Add(tmp);
                        tmp = -1;
                        while ((tmp + 1) < wkt2.Length && (tmp = wkt2.IndexOf(')', tmp + 1)) >= 0)
                            parensClose.Add(tmp);
                        tmp = Math.Min(parensOpen.Count, parensClose.Count);
                        for (int j = 0; j < tmp; j++)
                        {
                            var cc = new List<coordinate>();
                            if (parensOpen[j] > parensClose[j])
                                break;
                            var pts = wkt2.Substring(parensOpen[j] + 1, parensClose[j] - parensOpen[j] - 1).Trim().Split(',');
                            foreach (var pt in pts)
                            {
                                var xy = pt.Trim().Split(' ');
                                Double x, y;
                                if (xy.Length >= 2 && Double.TryParse(xy[0], ns, nfi, out x) && Double.TryParse(xy[1], ns, nfi, out y))
                                {
                                    cc.Add(new coordinate(x, y));
                                }
                            }
                            if (cc.Count > 2)
                            {
                                g.coordinates[0].Add(cc);
                            }
                        }
                        if (g.coordinates[0].Count > 0)
                        {
                            g.label = lbl;
                            retval.Add(g);
                        }
                    }
                }
            }

            return retval;
        }
    }
}
