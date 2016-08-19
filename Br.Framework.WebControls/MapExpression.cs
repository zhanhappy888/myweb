using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Refine.Orm;
using Refine.Oql;
using Ems.Data;
using Ems.Interface;
using Ems.AppCommon;
using Ems.Client.Lib;
using Ems.Expression;
using Ems.StdLib;
using Ems.Client.Charts;
using System.Drawing;
using DevExpress.XtraCharts;
using BMap.NET.WindowsForm;



namespace Br.Framework.WebControls
{
    public class MapExpression
    {

        public string containerId ;
        public string itemId ;
        private MapPage mapPage;
        public MapExpression(string containerid, string itemid, MapPage mp)
        {
            itemId = itemid;
            containerId = containerid;
            mapPage = mp;
        }
        public string ExpressMarkers( )
        {
            StringBuilder sb = new StringBuilder();
            if (this.mapPage.Markers.Count > 0)
            {
                //定义marker数组
                sb.Append("\tvar mapmarkers=new Array();\r\n");
                foreach(BMap.NET.WindowsForm.BMapElements.BMarker marker in this.mapPage.Markers.Values){
                    sb.AppendFormat("\tvar marker={{ \"lat\": \"{0}\", \"lng\":\"{1}\", \"remarks\": \"{2}\", \"name\": \"{3}\", \"address\": \"{4}\"}};\r\n", marker.Location.Lat, marker.Location.Lng, marker.Remarks, marker.Name, marker.Address);
                    sb.Append("\t mapmarkers.push(marker);\r\n");
                }
                sb.Append("\t createMapMarker(mapmarkers,map);");
                return sb.ToString();
            }
            else {
                return "";
            }

        }

        

    }
}
