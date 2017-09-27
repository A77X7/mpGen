using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace mpGen
{
    class Program
    {
        public static Boolean silent = false;
        public enum sourceType { Unknown, MIF, PostgreSQL }
        public static sourceType srcType = sourceType.Unknown;
        public static String mifSource;
        public static String pgConnectionString;
        public static String pgQuery;
        public static String pgFieldNameGeometryWkt;
        public static String pgFieldNameLabel;
        public static String mpDestination;
        public static String reprojectGeoServerUrl;
        public static String reprojectGeoServerUser;
        public static String reprojectGeoServerPassword;
        public static String reprojectSourceSrs;
        public static String reprojectTargetSrs;
        public static Boolean append = false;
        public static Object mpLock = new Object();
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    writeLine(
@"Convertor of MapInfo MIF or PostgreSQL WKT to MP/PFM (polish format)
(C) Pavel Aristarhov, 2017, arist77@mail.ru
USAGE:
mpGen.exe <options>
options are:

silent <true|false>
Silent mode. If true, no messages will be written to console. Optional. Default is false

append <true|false>
If true, file will be appended with new data. Optional. Default is false

mpDestination <filepath>
Path to destination .mp-file. Required

mifSource <filepath>
Path to source MapInfo .mif-file. Required if no pgXXX options

pgConnectionString <string>
Connection string to PostgreSQL database. Required if no mifXXX options

pgQuery <string>
SQL query to PostgreSQL database returning table of geometry primitives and may be it's labels. Required if no mifXXX options

pgFieldNameGeometryWkt <string>
Field name with WKT in table which is result of SQL query (see pgQuery option). Required if no mifXXX options

pgFieldNameLabel <string>
Field name with label in table which is result of SQL query (see pgQuery option). Optional

reprojectGeoServerUrl <string>
Url to GeoServer WPS. For example, http://gis1:8080/geoserver/wps. Optional

reprojectGeoServerUser <string>
User name on GeoServer. Required for reprojectGeoServerUrl option

reprojectGeoServerPassword <string>
User password on GeoServer. Required for reprojectGeoServerUrl option

reprojectSourceSrs <string>
Source coordinate system to reproject from. For example, EPSG:28413. Required for reprojectGeoServerUrl option

reprojectTargetSrs <string>
Target coordinate system to reproject to. Optional. Default is ""EPSG:4326""

Recomended use WGS 84 coordinate system only. You have to reproject source MIF/WKT before convert it to MP/PFM by this tool or use reprojectGeoServerUrl option. Use GDAL/OGR to reproject. For example:
ogr2ogr.exe -skipfailures -t_srs ""EPSG:4326"" -f ""MapInfo File"" D:\\tmp\\dstw84.MIF D:\\tmp\\src.tab");
                    Environment.ExitCode = 2;
                    return;
                }
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("silent") && args.Length > i + 1)
                    {
                        silent = Boolean.Parse(args[i + 1]);
                        i++;
                    }
                    else if (args[i].Equals("append") && args.Length > i + 1)
                    {
                        append = Boolean.Parse(args[i + 1]);
                        i++;
                    }
                    else if (args[i].Equals("mifSource") && args.Length > i + 1)
                    {
                        srcType = sourceType.MIF;
                        mifSource = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("pgConnectionString") && args.Length > i + 1)
                    {
                        srcType = sourceType.PostgreSQL;
                        pgConnectionString = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("pgQuery") && args.Length > i + 1)
                    {
                        srcType = sourceType.PostgreSQL;
                        pgQuery = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("pgFieldNameGeometryWkt") && args.Length > i + 1)
                    {
                        srcType = sourceType.PostgreSQL;
                        pgFieldNameGeometryWkt = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("pgFieldNameLabel") && args.Length > i + 1)
                    {
                        srcType = sourceType.PostgreSQL;
                        pgFieldNameLabel = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("mpDestination") && args.Length > i + 1)
                    {
                        mpDestination = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("reprojectGeoServerUrl") && args.Length > i + 1)
                    {
                        mpDestination = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("reprojectGeoServerUser") && args.Length > i + 1)
                    {
                        reprojectGeoServerUrl = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("reprojectGeoServerPassword") && args.Length > i + 1)
                    {
                        reprojectGeoServerPassword = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("reprojectSourceSrs") && args.Length > i + 1)
                    {
                        reprojectSourceSrs = args[i + 1];
                        i++;
                    }
                    else if (args[i].Equals("reprojectTargetSrs") && args.Length > i + 1)
                    {
                        reprojectTargetSrs = args[i + 1];
                        i++;
                    }
                }
                var st = DateTime.Now;
                var mp = new mpFile();
                var t = new Thread(() =>
                {
                    if (srcType == sourceType.MIF)
                    {
                        mifParser.parse(mifSource, mp.geometries);
                    }
                    else if (srcType == sourceType.PostgreSQL)
                    {
                        pgParser.parse(pgConnectionString, pgQuery, pgFieldNameGeometryWkt, pgFieldNameLabel, mp);
                    }
                });
                t.Start();
                lock (mpLock)
                {
                    if (!File.Exists(mpDestination) || !append)
                        File.WriteAllText(mpDestination, mp.header.ToString() + mpFile.lineSeparator);
                }
                int c = 0;
                //while (t.ThreadState != ThreadState.Stopped)
                //    Thread.Sleep(0);
                while (t.ThreadState != ThreadState.Stopped || mp.geometries.Count > 0)
                {
                    lock (mpLock)
                    {
                        //foreach (var g in mp.geometries)
                        //    g.label = c.ToString();
                        if (mp.geometries.Count > 0)
                        {
                            //reproject
                            if (!String.IsNullOrWhiteSpace(reprojectGeoServerUrl))
                            {
                                var coords = new List<coordinate>();
                                foreach (var g in mp.geometries)
                                {
                                    if (g is geometryPoint)
                                    {
                                        foreach (var coord in (g as geometryPoint).coordinate)
                                            coords.Add(coord.Value);
                                    }
                                    else if (g is geometryPolyline)
                                    {
                                        foreach (var coord in (g as geometryPolyline).coordinates)
                                            coords.AddRange(coord.Value);
                                    }
                                    else if (g is geometryPolygon)
                                    {
                                        foreach (var coord in (g as geometryPolygon).coordinates)
                                            foreach (var cc in coord.Value)
                                                coords.AddRange(cc);
                                    }
                                }
                                geometry.reprojectInplaceByGeoserver(reprojectGeoServerUrl, reprojectGeoServerUser, reprojectGeoServerPassword, reprojectSourceSrs, reprojectTargetSrs, coords);
                                //преобразование координат лучше делать ogr2ogr и сразу из TAB
                                //пример командной строки преобразования в WGS 84:
                                //ogr2ogr.exe -skipfailures -t_srs "EPSG:4326" -f "MapInfo File" D:\tmp\rekiw84.MIF D:\tmp\reki.tab
                            }

                            File.AppendAllText(mpDestination, String.Join(mpFile.lineSeparator, mp.geometries) + mpFile.lineSeparator);
                            if (!silent)
                                Console.CursorLeft = 0;
                            c += mp.geometries.Count;
                            mp.geometries.Clear();
                            //GC.Collect();
                            write(c.ToString() + " / " + (Environment.WorkingSet / 1024.0 / 1024.0).ToString("N1") + " MB              ");
                        }
                    }
                    Thread.Sleep(0);
                }
                writeLine("\r\nobjects processed (" + (DateTime.Now - st).ToString() + ")");
                //Console.ReadLine();
                Environment.ExitCode = 0;
            }
            catch (Exception err)
            {
                writeLine(err.ToString());
                Environment.ExitCode = 1;
            }
        }
        static void writeLine(String msg)
        {
            if (!silent)
                Console.WriteLine(msg);
        }
        static void write(String msg)
        {
            if (!silent)
                Console.Write(msg);
        }
    }
}
