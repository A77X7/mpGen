# mpGen
Convertor of MapInfo MIF, PostgreSQL (WKT) to Polish format (MP/PFM)

I had needed convertor from popular GIS formats (MapInfo, AutoCAD, KML, GML, ...) to Garmin IMG map file...

There is many conversion utilities for many GIS formats.
For example, OGR2OGR converts tens or hundreds formats but MP/PFM.
cGPSmapper, MapTk converts from MP/PFM and GMap from OSM to IMG.
No good convertor exist for conversion from GIS formats to MP/PFM or directly to IMG.
GMap can convert OSM direct to IMG but no good utilities to convert to OSM. OGR2OGR OSM driver support read only mode only. Then it can't create OSM from many GIS formats.

...and I decided to develop absent chain link myself.

I chose the MIF format because it's the main one for me and it has good support by OGR2OGR.
Then I added PostgreSQL support. I read from PostgreSQL geometry field as WKT and then convert it to MP/PFM. Respectively mpGen potentially can convert from WKT.
In addition, as a by-product, mpGen can convert text objects in MIF to points with moving text and rotation angle into a new column in MID. It's needed to folowing conversion to PostgreSQL wich can store POINTs, LINEs or POLYGONs but no text. Beside, many GIS products and formats such as GeoServer and shapefiles and WKT can hold (or work with) limited kinds of geometries.

Usage from command line (terminal):
mpGen.exe 
