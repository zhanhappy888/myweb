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
    public class ExcelExpression
    {

        public string containerId ;
        public string spreadId ;
        private ExcelPage page;
        private string fileString;
        private float px;
        private float py;
        public ExcelExpression(string containerid, string spreadid, ExcelPage ep, string filestring,float x,float y)
        {
            spreadId = spreadid;
            containerId = containerid;
            page=ep;
            fileString = filestring;
            px = x;
            py = y;
        }
        public string ExpressDataSet(ReportRange rr)
        {
            StringBuilder sb = new StringBuilder();
            DataSetRange dsr =new DataSetRange();

            if (rr is DataSetRange)
            {
                dsr = rr as DataSetRange;
                string getdata = "getExcelData(dataSet{0},{1},{2})";
                sb.AppendFormat("populateFromArray({0}, {1}, {2}) ;\r\n", rr.Bounds.Y, rr.Bounds.X, string.Format(getdata, dsr.DataSetID, dsr.HorExpandDimension, dsr.VerExpandDimension));

                return sb.ToString();

            }
            else if (rr is TitleRange)
            {
                TitleRange tr = rr as TitleRange;
                sb.AppendFormat("setDataAtCell({0}, {1}, '{2}') ;\r\n", rr.Bounds.Y, rr.Bounds.X, tr.Format.Resolve());

                return sb.ToString();
            }
            else {
                return "";
            }
        }

        public string ExpressInitScript(Boolean posionrelative = false)
        {
            StringBuilder sb = new StringBuilder();

            //声明对象数组
            sb.AppendFormat("\tvar {0},{1},{1}sheet;\r\n", this.containerId, this.spreadId);
            sb.AppendFormat("var {0}data;\r\n",  this.containerId);
            //函数开始标记
            sb.Append(" function initExcelPage(){ \r\n");

            sb.Append("\tvar oriBounds = {};\r\n");
            sb.Append("\toriBounds.width  = " + page.Width + ";\r\n");
            sb.Append("\toriBounds.height = " + page.Height + ";\r\n");
            sb.Append("\toriBounds.left =" + px.ToString() + " ;\r\n");
            sb.Append("\toriBounds.top =" + py.ToString() + " ;\r\n");
            sb.Append("\toricanBounds.push(oriBounds);\r\n");

            sb.Append("\tca.push('" + page.GetType().Namespace + ".ExcelPage');\r\n");
            sb.Append("\tcadivid.push('" + containerId + "');\r\n");  //推入excelpage的divid

            //地图控件宽度高度和位置

            //init 初始化表格
            //设置表格宽和高
            //sb.AppendFormat("\t$('#{0}').height($(document).height()); \r\n", this.containerId);

            sb.AppendFormat("var spreaddiv = document.getElementById('{0}');      \r\n",this.containerId);
            if (!posionrelative)
            {
                sb.Append("spreaddiv.style.position = 'absolute';\r\n");
            }
  
            sb.AppendFormat("spreaddiv.style.left = {0} + 'px';         \r\n",px);
            sb.AppendFormat("spreaddiv.style.top = {0} + 'px';          \r\n",py);
            sb.AppendFormat("spreaddiv.style.width = {0} + 'px';    \r\n",page.Width);
            sb.AppendFormat("spreaddiv.style.height = {0} + 'px';  \r\n", page.Height);
            //显示边框
            string borderStylestr = "none";
            if (page.BorderVisible)
            {
                borderStylestr = "solid";
            }
            sb.AppendFormat("spreaddiv.style.borderStyle = \"{0}\" ;  \r\n", borderStylestr);


            sb.AppendFormat("{0} = new GcSpread.Sheets.Spread(document.getElementById(\"{1}\"));\r\n", this.spreadId, this.containerId);
            sb.AppendFormat("excelObj = {0};\r\n",this.spreadId);
            //解析数据集
            //load data from excel jason data
            sb.AppendFormat("{0}.fromJSON({1});\r\n", this.spreadId, fileString);  //上线前去掉注释
            //调试用，显示基本excel表格，数据来源于getExcelSampleTable()  in ActorsBase
            //sb.AppendFormat("{0}.getSheet(0).setDataSource(getExcelSampleTable());\r\n", this.spreadId);

            sb.AppendFormat("{0}sheet = {0}.getSheet(0);\r\n", this.spreadId);
            //设置表格格式
            sb.AppendFormat("  {0}sheet.setRowCount(50, GcSpread.Sheets.SheetArea.viewport);  \r\n", this.spreadId);
            sb.AppendFormat("  {0}sheet.setColumnCount(50, GcSpread.Sheets.SheetArea.viewport);  \r\n", this.spreadId);

            if (!page.ShowSheetTab) {

                //去掉导航和行列标题   by zhanj 
                sb.AppendFormat("  {0}.tabNavigationVisible(false); \r\n", this.spreadId);
                sb.AppendFormat("  {0}.tabStripVisible(false); \r\n", this.spreadId);
                sb.AppendFormat("  {0}.newTabVisible(false); \r\n", this.spreadId);
            }else {
                sb.AppendFormat("  {0}.tabNavigationVisible(true); \r\n", this.spreadId);
                sb.AppendFormat("  {0}.tabStripVisible(true); \r\n", this.spreadId);
                sb.AppendFormat("  {0}.newTabVisible(true); \r\n", this.spreadId);
            }
         
            //设置Header
            if (!page.ShowHeader)
            {
                sb.AppendFormat("  {0}sheet.setRowHeaderVisible(false);\r\n", this.spreadId);
                sb.AppendFormat("  {0}sheet.setColumnHeaderVisible(false);\r\n", this.spreadId);
            }
            
       

            //函数结束标记
            sb.Append("}\r\n");
            return sb.ToString();
        }
        public string ExpressUpdateScript()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" function updateExcelPage(){ \r\n");
            //更新数据
            sb.Append(" var is_ready=true;           \r\n");
            sb.Append(" for (var i = 0; i < dataSets.length; i ++){              \r\n");
            sb.Append("        var dataSet = dataSets[i];              \r\n");
            sb.Append("        for(var j = 0; j < dataSet.commands.length; j++){              \r\n");
            sb.Append("            var command = dataSet.commands[j];              \r\n");
            sb.Append("            if (!command.ready) {              \r\n");
            sb.Append("               is_ready=false;           \r\n");
            sb.Append("            }              \r\n");
            sb.Append("        }              \r\n");
            sb.Append(" 	}              \r\n");
            sb.Append("if(is_ready){             \r\n");
            sb.Append( ExpressData(this.page.Ranges, this.spreadId+"sheet"));

            sb.Append(" }            \r\n");



            sb.Append("}\r\n");

            return sb.ToString();
        }

        //public string ExpressMergeCells(ReportRangeCollection rrc)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("  mergeCells: [              \r\n");
        //    for (int i = 0; i < rrc.Count; i++)
        //    {
        //        if (rrc[i] is TitleRange)
        //        {
        //            TitleRange tr = rrc[i] as TitleRange;
        //            sb.AppendFormat("{{row: {0}, col: {1}, rowspan: {2}, colspan: {3}}}", tr.Bounds.Y, tr.Bounds.X, tr.Bounds.Height,tr.Bounds.Width);

        //        }
        //    }
        //    sb.Append(" ],              \r\n");
        //    return sb.ToString();

        //}
        public string ExpressData(ReportRangeCollection rrc,string controlid)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rrc.Count; i++)
            {
                if (rrc[i] is DataSetRange)
                {
                    DataSetRange dsr = rrc[i] as DataSetRange;
                    sb.AppendFormat("{0}.setArray({1}, {2}, getExcelCommandData(dataSet{3},{4}));   \r\n", controlid,dsr.Bounds.Y + 1, dsr.Bounds.X + 1, dsr.DataSetID, dsr.VerExpandDimension);
                }
            }
 
            return sb.ToString();

        }
        

    }
}
