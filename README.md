# mpGen
Convertor of MapInfo MIF, PostgreSQL (WKT) to Polish format (MP/PFM)

I had needed convertor from popular GIS formats (MapInfo, AutoCAD, KML, GML, ...) to Garmin IMG map file...

There is many conversion utilities for many GIS formats.
For example, OGR2OGR converts tens or hundreds formats but MP/PFM.
cGPSmapper, MapTk converts from MP/PFM and GMap from OSM to IMG.
No good convertor exist for conversion from GIS formats to MP/PFM or directly to IMG.
GMap can convert OSM direct to IMG but no good utilities to convert to OSM. OGR2OGR OSM driver support read only mode only. Then it can't create OSM from many GIS formats.

...and I decided to develop missing chain link myself.

I chose the MIF format because it's the main one for me and it has good support by OGR2OGR.
Then I added PostgreSQL support. I read from PostgreSQL geometry field as WKT and then convert it to MP/PFM. Respectively mpGen potentially can convert from WKT.
In addition, as a by-product, mpGen can convert text objects in MIF to points with moving text and rotation angle into a new column in MID. It's needed to folowing conversion to PostgreSQL wich can store POINTs, LINEs or POLYGONs but no text. Beside, many GIS products and formats such as GeoServer and shapefiles and WKT can hold (or work with) limited kinds of geometries.

# Usage from command line (terminal)
mpGen.exe <options>
options are:

silent <true|false>
Silent mode. If true, no messages will be written to console. Optional. Default is false

append <true|false>
If true, file will be appended with new data. Usable for combine number of sources to one destination. Optional. Default is false

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
Target coordinate system to reproject to. Optional. Default is "EPSG:4326"

Recomended to use WGS 84 coordinate system only. You have to reproject source MIF/WKT before convert it to MP/PFM by this tool or use reprojectGeoServerUrl option. Use GDAL/OGR to reproject. For example:
ogr2ogr.exe -skipfailures -t_srs ""EPSG:4326"" -f ""MapInfo File"" D:\\tmp\\dstw84.MIF D:\\tmp\\src.tab");

# Sources
geometry.cs contains abstract geometry class for internal representation.

geometryPoint.cs contains descendant of geometry for points.

geometryPolyline.cs contains descendant of geometry for polylines.

geometryPolygon.cs contains descendant of geometry for polygons.

coordinate.cs contains representation of coordinate (x, y).

header.cs contains MP/PFM header part.

mpFile.cs contains representation of MP/PFM file (header + geometry collection).

mifParser.cs contains static methods to parse MIF files.

pgParser.cs contains static methods to parse WKT from PostgreSQL.

Program.cs contains main entry point with multithreaded processing.
