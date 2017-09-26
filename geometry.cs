using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace mpGen
{
    abstract class geometry
    {
        static NumberFormatInfo nfi;
        static geometry()
        {
            nfi = new NumberFormatInfo();
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalSeparator = ".";
        }
        public geometry()
        {
            label = null;
        }
        public Byte type;
        public Byte? subType;
        public String label;

        public abstract void reprojectByGeoserver(String urlGeoserverWps, String user, String password, String srsSource, String srsTarget);

        public static void reprojectInplaceByGeoserver(String urlGeoserverWps, String user, String password, String srsSource, String srsTarget, IEnumerable<coordinate> coordinates)
        {
            urlGeoserverWps = urlGeoserverWps ?? "http://gis1:8080/geoserver/wps";
            user = user ?? "admin";
            password = password ?? "!234werty";
            srsTarget = srsTarget ?? "EPSG:4326";
            var uri = new Uri(Uri.EscapeUriString(urlGeoserverWps));
            var request = HttpWebRequest.Create(uri);
            request.ContentType = "application/xml";
            request.Headers[HttpRequestHeader.ContentEncoding] = "utf-8";
            request.Method = "POST";
            request.Credentials = new NetworkCredential(user, password);
            var xmlTemplate = Properties.Resources.ResourceManager.GetObject("reprojectRequest").ToString();
            xmlTemplate = xmlTemplate.Replace("MULTIPOINT(0 0)", "MULTIPOINT(" + String.Join(",", from c in coordinates select (c.x.ToString(nfi) + " " + c.y.ToString(nfi))) + ")").Replace("EPSG:SOURCE", srsSource).Replace("EPSG:TARGET", srsTarget);
            var bb = Encoding.UTF8.GetBytes(xmlTemplate);
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bb, 0, bb.Length);
                requestStream.Close();

                var res = request.GetResponse() as HttpWebResponse;
                //var bbb = new Byte[res.ContentLength > 0 ? res.ContentLength : 0];
                using (var responseStream = res.GetResponseStream())
                {
                    using (var responseReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        //responseStream.Read(bbb, 0, bbb.Length);
                        //var wktResponse = Encoding.UTF8.GetString(bbb);
                        var wktResponse = responseReader.ReadToEnd();
                        wktResponse = wktResponse.Substring("MULTIPOINT(".Length + 1);
                        wktResponse = wktResponse.Substring(0, wktResponse.Length - 1);
                        var strCoords = wktResponse.Split(',');
                        int i = 0;
                        foreach (var coordinate in coordinates)
                        {
                            var strXY = strCoords[i].Trim().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            coordinate.x = Double.Parse(strXY[0].TrimStart('('), nfi);
                            coordinate.y = Double.Parse(strXY[1].TrimEnd(')'), nfi);
                            i++;
                        }
                    }
                }
            }
        }
    }
}
