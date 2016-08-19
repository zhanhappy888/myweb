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
    public class HeatMapExpression
    {

        public string containerId ;
        public string itemId ;
        private HeatMapPage heatmapPage;
        public string rootPath;
        public HeatMapExpression(string rootpath, HeatMapPage mp)
        {
            heatmapPage = mp;
            rootPath = rootpath;
        }

        public string ExpressInitScript(float x, float y,string controlid,Boolean posionrelative=false)
        {
            //初始化scripts,from HeatMapExpression.cs的ExpressInitScript()函数
            StringBuilder initSb = new StringBuilder();
            //hm1是网页上的heatmap对象；
           
            string initchart = "\tvar hm1 = echarts.init({{ X: {0}, Y: {1}, Width: {2}, Height: {3},parentid:'{4}',position:{5} }});\r\n";
            //string setoption = "\tra[{0}].setOption(opts[{0}]);\r\n";

            //函数开始标记
            initSb.Append(" function initHeatMapPage(){ \r\n");
            //initSb.Append("\tvar ph= document.body.scrollHeight-2;\r\n");
            //initSb.Append("\tvar pw= document.body.scrollWidth-2;\r\n");
            //初始化热力图chart,设置其初始化大小和位置
            initSb.AppendFormat(initchart, x, y, heatmapPage.Width, heatmapPage.Height, controlid, posionrelative.ToString().ToLower());
            initSb.Append("\theatMapObj.obj = hm1;\r\n"); //热力图对象加入

            initSb.Append("\tvar oriBounds = {};\r\n");

            initSb.Append("\toriBounds.width  = " + heatmapPage.Width + ";\r\n");
            initSb.Append("\toriBounds.height = " + heatmapPage.Height + ";\r\n");
            initSb.Append("\toriBounds.left =" + x.ToString() + " ;\r\n");
            initSb.Append("\toriBounds.top =" + y.ToString() + " ;\r\n");
            initSb.Append("\toricanBounds.push(oriBounds);\r\n");

            initSb.Append("\tca.push('" + heatmapPage.GetType().Namespace + ".HeatMapPage');\r\n");
            initSb.Append("\tcadivid.push(hm1.getDom());\r\n");   //推入heatmap(hetmap属于echart)的divid对象


            if (!string.IsNullOrEmpty(heatmapPage.JsonName))
            {
                //获取geoJson数据
                string name = heatmapPage.JsonName.Replace(".json", "Json");
                initSb.Append("\t$.ajax({   \r\n");
                initSb.Append("\t    type: 'get',\r\n");
                initSb.AppendFormat("\t	 url: '{0}/Scripts/echarts/mapjson/{1}',  \r\n", this.rootPath, heatmapPage.JsonName);
                initSb.Append("\t    dataType: \"json\",\r\n");
                initSb.Append("\t    success: function (data) {\r\n");

                initSb.AppendFormat("\t     echarts.registerMap('{0}', data);   \r\n", name);

                initSb.AppendFormat("\t    heatMapObj.opt =BuildChartOption('{0}',{{ 'file': '{1}', 'name': '{2}'}});\r\n", "heatmap", heatmapPage.JsonName, name);
                initSb.AppendFormat("heatMapObj.opt.legend={0};", buildChartLegend(heatmapPage.LegendItems));
                initSb.Append("\r\n heatMapObj.obj.setOption(heatMapObj.opt);\r\n");
                //if (posionrelative)
                //{
                //    initSb.Append("\thm1.getDom().style.position = 'relative';\r\n");
                //}
                //else
                //{
                //    initSb.Append("\thm1.getDom().style.position = 'absolute';\r\n");
                //}
                initSb.Append("\t    },\r\n");
                initSb.Append("\t    error: function (msg) {  \r\n");
                initSb.Append("\t      alert(\"数据加载失败！\");  \r\n");
                initSb.Append("\t    }  \r\n");

                initSb.Append("\t });   \r\n");
            }

            //函数结束标记
            initSb.Append("}\r\n");

            return initSb.ToString();

        }
        /// <summary>
        /// 构建图例
        /// </summary>
        /// <param name="lis"></param>
        /// <returns></returns>
        public string buildChartLegend(List<Ems.Client.HeatMap.LegendItem> lis)
        {
   
            StringBuilder sb = new StringBuilder();

            sb.Append("{");
            if (lis.Count > 0)
            {
                sb.Append("left: 'left',");
                sb.Append("top: 'top',");
                sb.Append("orient: 'vertical',");

                sb.Append("data:[");
                foreach (Ems.Client.HeatMap.LegendItem li in lis)
                {
                    //如果是饼图则图例取AxisDimension
                    sb.AppendFormat("'{0}',", li.DisplayHeader);
                }
                if (sb.ToString().EndsWith(","))
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append("]");

            }
            

            sb.Append("}");
            return sb.ToString();
        }
        public string ExpressUpdateScript()
        {
            StringBuilder updateSb = new StringBuilder();
            //string updatesetoption = "\tra[{0}].setOption(opts[{0}]);\r\n";
            updateSb.Append(" function updateHeatMapPage(){ \r\n");
            //更新数据
            updateSb.Append(" var is_ready=true;           \r\n");
            updateSb.Append(" for (var i = 0; i < dataSets.length; i ++){              \r\n");
            updateSb.Append("        var dataSet = dataSets[i];              \r\n");
            updateSb.Append("        for(var j = 0; j < dataSet.commands.length; j++){              \r\n");
            updateSb.Append("            var command = dataSet.commands[j];              \r\n");
            updateSb.Append("            if (!command.ready) {              \r\n");
            updateSb.Append("               is_ready=false;           \r\n");
            updateSb.Append("            }              \r\n");
            updateSb.Append("        }              \r\n");
            updateSb.Append(" 	}              \r\n");
            updateSb.Append("if(is_ready && typeof(heatMapObj.opt) != 'undefined'){ \r\n");
            for (int idx = 0; idx < heatmapPage.LegendItems.Count; idx++)
            {
                if (idx != 0)
                {

                    updateSb.AppendFormat("\t     heatMapObj.opt.series.push(getHeatMapSeries('{0}'));", heatmapPage.JsonName.Replace(".json", "Json"));//深度复制，只能复制数据，不能复制方法

                }

                foreach (var item in heatmapPage.ItemNames)
                {

                    updateSb.AppendFormat("\t     heatMapObj.opt.series[{4}].data.push({{ 'name': '{0}', 'value': getAreaData(dataSet{1},{2},{3},{4})}});\r\n", item.Name, heatmapPage.DataSetID, heatmapPage.EmsLinkIndex, item.Index, idx);
                }
                updateSb.AppendFormat("\t     heatMapObj.opt.series[{0}].name='{1}';\r\n", idx, heatmapPage.LegendItems[idx].DisplayHeader);

            }

            //updateSb.AppendFormat(updatesetoption, index);
            updateSb.Append("\t     heatMapObj.obj.setOption(heatMapObj.opt);");
            updateSb.Append(" }            \r\n");
            updateSb.Append("}\r\n");
            return updateSb.ToString();
        }

        

    }
}
