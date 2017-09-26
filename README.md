# mpGen
Convertor of MapInfo MIF, PostgreSQL (WKT) to Polish format (MP/PFM)

I had needed convertor from popular GIS formats (MapInfo, AutoCAD, KML, GML, ...) to Garmin IMG map file...

There is many conversion utilities for many GIS formats.
For example, OGR2OGR converts tens or hundreds formats but MP/PFM.
cGPSmapper, MapTk converts from MP/PFM and GMap from OSM to IMG.
No good convertor exist for conversion from GIS formats to MP/PFM or directly to IMG.
GMap can convert OSM direct to IMG but no good utilities to convert to OSM. OGR2OGR OSM driver support read only mode only. Then it can't create OSM from many GIS formats.

...and I decided to develop absent chain link myself.
