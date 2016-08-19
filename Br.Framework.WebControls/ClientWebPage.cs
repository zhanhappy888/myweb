using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

using Ems.Interface;
using Ems.Data;
using Ems.Client.Lib;
using Ems.Client.View;


namespace Br.Framework.WebControls
{
    public class ClientWebPage : SessionPage
    {
        private Guid pageid ;
        private Guid linkid ;
        protected LivePage livePage;
        private void initclientpage(){
            string pid = base.Request.QueryString["pageid"];
            string lid = base.Request.QueryString["linkid"];
            if (!string.IsNullOrEmpty(pid)&&!string.IsNullOrEmpty(lid))
            {
                try
                {
                    pageid = new Guid(pid);
                    linkid = new Guid(lid);
                    ILinkQueryService service = client.GetLinkQueryService(linkid);
                    ClientPage page = service.Load(typeof(ClientPage), pageid) as ClientPage;
                    livePage = PageHelper.CreateLivePage(page, null, false);
                    this.Title = livePage.ToString();
                    RegisterDataSetScripts();
                }
                catch( Exception e)
                {
                    throw (e);
                   // base.Response.Redirect(this.rootpath + "/" + this.ErrorPage, true);
                }
            }
            else
            {
                base.Response.Redirect(this.rootPath + "/" + this.ErrorPage, true);
            }

        }
    
        protected override void OnPreInit(EventArgs e)
        {
            
            base.OnPreInit(e);
            initclientpage();
            //动态注册js 页面添加的js需要放到body末尾
          //  ClientScript.RegisterClientScriptInclude("jquery", rootpath + "/Scripts/js/jquery-1.10.2.js");

        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected string GetCreateScripts(DataSet dataSet)
        {
            if (dataSet is ResultDataSet)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("var dataSet{0} = new ResultMatrixDataSet({0}, [", dataSet.DataSetID);
                for (int i = 0; i < dataSet.Dimensions.Count; i++)
                {
                    IDimension dimension = dataSet.Dimensions[i];
                    if (i > 0)
                        sb.Append(",");
                    sb.AppendFormat("[");
                    for (int j = 0; j < dimension.Count; j++)
                    {
                        if (j > 0)
                            sb.Append(",");
                        IVector vector = dimension[j];
                        sb.AppendFormat("{{fullName:\"{0}\",shortName:\"{1}\"}}", vector.Name, vector.ShortName);
                    }
                    sb.AppendFormat("]");
                }
                sb.AppendFormat("]);");

                return sb.ToString();
            }
            else if(dataSet is DataCurveDataSet)
                return string.Format("var dataSet{0} = new DataCurveDataSte({0}, 1, {1});\r\n", dataSet.DataSetID, (dataSet as DataCurveDataSet).DataItems.Count);
            else if (dataSet is FreeValueDataSet)
                return string.Format("var dataSet{0} = new {1}({0}, {2});\r\n", dataSet.DataSetID, dataSet.GetType().Name, (dataSet as FreeValueDataSet).DataItemIDs.Length);
            else if (dataSet is ColumnExtractDataSet)
            {
                ColumnExtractDataSet ds = dataSet as ColumnExtractDataSet;
                return string.Format("var dataSet{0} = new ColumnExtractDataSet({0}, dataSet{1}, {2}, {3});\r\n", dataSet.DataSetID, ds.Parent.DataSetID, ds.DimIndex, ds.VectorIndex);
            }
            else if (dataSet is DimensionSummaryDataSet)
            {
                DimensionSummaryDataSet ds = dataSet as DimensionSummaryDataSet;
                return string.Format("var dataSet{0} = new DimensionSummaryDataSet({0}, dataSet{1}, {2});\r\n", dataSet.DataSetID, ds.Parent.DataSetID, ds.DimIndex);
            }

            return string.Empty;
        }

        protected void RegisterDataSetScripts()
        {
            StringBuilder scripts = new StringBuilder();
            scripts.Append("var dataSets = new Array();\r\n");
            foreach (DataSet dataSet in livePage.DataSets)
            {
                string line = this.GetCreateScripts(dataSet);
                scripts.AppendLine(line);
            }

            scripts.AppendLine("function initDataSets() {");
            foreach (DataSet dataSet in livePage.DataSets)
                scripts.AppendFormat("\tdataSets.push(dataSet{0});\r\n", dataSet.DataSetID);
            foreach (DataSet dataSet in livePage.DataSets)
            {
                if (dataSet.IsOriginal)
                {
                    for (int i = 0; i < dataSet.LinkCount; i++)
                    {
                        byte[] command = dataSet.GetRequestCommand(i);
                        if (command != null)
                        {
                            string text = Ems.Common.BinUtils.BinToText(command);

                            string url = "ws://218.241.131.239:4649/Type1";//218.241.131.239         192.168.1.68
                            scripts.AppendFormat("\tdataSet{0}.setCommand({1}, \"{2}\", \"{3}\", {4});\r\n", dataSet.DataSetID, i, url, text, dataSet.IsRealTime.ToString().ToLower());

                    
                        }
                    }
                }
            }
            scripts.AppendLine("}");

            scripts.AppendLine("initDataSets();");
            scripts.AppendLine("initWebSockets();");
            //scripts.AppendLine("loadValues();");

            ClientScript.RegisterClientScriptInclude(this.GetType(), "classScripts", "../js/class.js");
            ClientScript.RegisterClientScriptInclude(this.GetType(), "dataSetScripts", "../js/dataset.js");
            ClientScript.RegisterClientScriptBlock(this.GetType(), "dataInitialize", scripts.ToString(), true);
        }
    }
}
