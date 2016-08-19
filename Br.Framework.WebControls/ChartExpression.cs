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



namespace Br.Framework.WebControls
{
    public class ChartExpression
    {

        public string ExpressInitOption(PageRole role, string opts)
        {
            ChartActorBase ab = role.Actor as ChartActorBase;
            StringBuilder sb = new StringBuilder();
           
            if (ab.DataSetID != -1)//DataSetID为-1时，未绑定数据
            {
                sb.AppendFormat("{0}.title={1};",opts,buildChartTitle(role));
                sb.AppendFormat("{0}.legend={1};", opts, buildChartLegend(role));
                if(ab.ChartCategory.ViewType != ViewType.Pie){
                    sb.AppendFormat("{0}.xAxis={1};", opts, buildChartxAxis(role));
                }
                sb.AppendFormat("{0}.series={1};", opts, buildChartseries(role));
                Rectangle r= ab.GetDiagramBounds();
                if (r.Width != 0)
                {
                    string grid = " {{ width: {0},height: {1} ,left:{2} ,top:{3}}} ";
                    sb.AppendFormat("{0}.grid={1};", opts,string.Format(grid,r.Width-(role.Size.Width - r.Width),r.Height-(role.Size.Height - r.Height), role.Size.Width - r.Width-10, role.Size.Height - r.Height));

                }
               
            }
            return sb.ToString();
        }

        /// <summary>
        /// 构建标题
        /// </summary>
        /// <param name="ctp"></param>
        /// <returns></returns>
        public string buildChartTitle(PageRole role)
        {
            ChartActorBase cab = role.Actor as ChartActorBase;
            ChartTitleProp ctp = cab.Title;
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            //设置Title内容和样式
            sb.AppendFormat("text: \"{0}\",", ctp.Text);
            sb.AppendFormat("textStyle: {{fontSize: {0},color: '{1}' }},", Math.Round(ctp.Font.Size*4/3), System.Drawing.ColorTranslator.ToHtml(ctp.TextColor));
            //设置Title位置
            string y;
            if (ctp.Dock == ChartTitleDockStyle.Left || ctp.Dock == ChartTitleDockStyle.Right)
            {
                y = "center";
            }
            else
            {
                y = ctp.Dock.ToString().ToLower();
            }
            sb.AppendFormat("y:\"{0}\",", y);//Top 0 Bottom 1 Left 2 Right 3
            string x;
            if (ctp.Alignment == StringAlignment.Near)
            {
                x = "left";
            }
            else if (ctp.Alignment == StringAlignment.Far)
            {
                x = "right";
            }
            else
            {
                x = ctp.Alignment.ToString().ToLower();
            }
            sb.AppendFormat("x:\"{0}\"", x);
           
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// 构建图例
        /// </summary>
        /// <param name="cab"></param>
        /// <returns></returns>
        public string buildChartLegend(PageRole role)
        {
            ChartActorBase cab = role.Actor as ChartActorBase;
            ChartLegend cl=cab.ChartLegend;
            StringBuilder sb = new StringBuilder();

            sb.Append("{");
            //如果是饼图则图例取AxisDimension
            sb.AppendFormat("data: getLegendArray(dataSet{0}.dimensions[{1}],true),", cab.DataSetID,cab.ChartCategory.ViewType == ViewType.Pie?cab.AxisDimension:( 1 - cab.AxisDimension));
      
            sb.AppendFormat("borderWidth: {0},", cl.Border.Thickness);
            sb.AppendFormat("borderColor: \"{0}\",", System.Drawing.ColorTranslator.ToHtml(cl.Border.Color));
            //sb.AppendFormat("backgroundColor: \"{0}\",", System.Drawing.ColorTranslator.ToHtml(cl.BackColor));//ECharts3设置背景后有bug
            sb.AppendFormat("textStyle: {{fontSize: {0},color: '{1}' }},", Math.Round(cl.TextFont.Size * 4 / 3), System.Drawing.ColorTranslator.ToHtml(cl.TextColor));
            if (cl.Direction == LegendDirection.TopToBottom || cl.Direction == LegendDirection.BottomToTop)//纵向排列
            {
                sb.Append("orient: \"vertical\",");
            }
            else
            {
                sb.Append("orient: \"horizontal\",");
            }
            if (cl.AlignmentHorizontal == LegendAlignmentHorizontal.RightOutside || cl.AlignmentHorizontal == LegendAlignmentHorizontal.Right)
            {
                sb.Append("left:\"right\",");

            }
            else if (cl.AlignmentHorizontal == LegendAlignmentHorizontal.LeftOutside || cl.AlignmentHorizontal == LegendAlignmentHorizontal.Left)
            {
                sb.Append("left:\"left\",");
            }
            else
            {
                sb.Append("left:\"center\",");
            }
            if (cl.AlignmentVertical == LegendAlignmentVertical.Top || cl.AlignmentVertical == LegendAlignmentVertical.TopOutside)
            {
                Rectangle r = cab.GetDiagramBounds();
                sb.AppendFormat("top:\"{0}\"", role.Size.Height-r.Height);
            }
            else if (cl.AlignmentVertical == LegendAlignmentVertical.BottomOutside || cl.AlignmentVertical == LegendAlignmentVertical.Bottom)
            {
                sb.Append("top:\"bottom\"");

            }
            else
            {
                sb.Append("top:\"center\"");
            }

            sb.Append("}");
            return sb.ToString();
        }
        /// <summary>
        /// 构建横轴
        /// </summary>
        /// <param name="cab"></param>
        /// <returns></returns>
        public string buildChartxAxis(PageRole role)
        {
            ChartActorBase cab = role.Actor as ChartActorBase;

            ChartLegend cl = cab.ChartLegend;
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("data: getxAxisArray(dataSet{0}.dimensions[{1}],true),", cab.DataSetID, cab.AxisDimension);//设置X轴数据

            sb.AppendFormat("type: '{0}'", "category");
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// 构建系列
        /// </summary>
        /// <param name="cab"></param>
        /// <returns></returns>
        public string buildChartseries(PageRole role)
        {
            ChartActorBase cab = role.Actor as ChartActorBase;


            ChartLegend cl = cab.ChartLegend;
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            if (cab.SeriesCollection.Count > 0)
            {
                for (int i = 0; i < cab.SeriesCollection.Count; i++)//cab.SeriesCollection判断有问题
                { 
                    sb.Append("{");
                    if (cab.ChartCategory.ViewType != ViewType.Pie)
                    {
                        sb.AppendFormat("data: getSeriesArray(dataSet{0}.getSeries({1},{2}),false,true),", cab.DataSetID, cab.ChartCategory.ViewType == ViewType.Pie ? cab.AxisDimension : (1 - cab.AxisDimension), i);//设置X轴数据

                        ViewType vt = cab.SeriesCollection[i].ViewType;
                        if (vt == ViewType.Bar || vt == ViewType.Line || vt == ViewType.Pie)
                        {
                            sb.AppendFormat("type: '{0}',", vt.ToString().ToLower());

                        }
                        else if (vt == ViewType.Spline)
                        {
                            sb.Append("type: 'line',");
                            sb.Append("smooth: true,");
                        }
                        else if (vt == ViewType.Area || vt == ViewType.SplineArea)
                        {
                            sb.Append("type: 'line',");
                            sb.Append("areaStyle: {normal: {}},");
                            if (vt == ViewType.SplineArea)
                            {
                                sb.Append("smooth: true,");
                            }
                        }
                        sb.AppendFormat("name: getLegendArray(dataSet{0}.dimensions[{1}],true)[{2}]", cab.DataSetID, cab.ChartCategory.ViewType == ViewType.Pie ? cab.AxisDimension : (1 - cab.AxisDimension), i);
                    }
                    else {
                        sb.AppendFormat("type: '{0}',", cab.SeriesCollection[i].ViewType.ToString().ToLower());
                        sb.AppendFormat("data:getPieSeriesData(getSeriesArray(dataSet{0}.getSeries(0,{2}),false,true),getLegendArray(dataSet{0}.dimensions[{1}],true))", cab.DataSetID, cab.AxisDimension, i);//设置X轴数据

                        
                    }
                    sb.Append("},");
                }
                if (sb.ToString().EndsWith(","))
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }
            sb.Append("]");
            return sb.ToString();
        }
        
    }
}
