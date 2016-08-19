using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Refine.Orm;
using Ems.Data;
using Ems.Client.View;
using Ems.Interface;
using Ems.Data;
using Ems.Client.Lib;
using Ems.Client.View;
using Refine.Oql;
using System.Collections;
using System.Web;

namespace Br.Framework.WebControls
{
    public partial class MenuTreeView : TreeView
    {

        public MenuTreeView(Client client)
        {
            this.client = client;
        }
        protected Client client;
        public Client Client
        {
            get
            {
                return this.client;
            }
            set
            {
                this.client = value;
            }
        }

        public string initLinkTrees(string parentid, string pidparent)
        {
            if (parentid == "0")
            {
                return loadRootTree(parentid);
            }
            else
            {
                if (!string.IsNullOrEmpty(parentid))
                {
                    if (parentid.IndexOf("page_category") >= 0)
                    {
                        return pagesToJson(parentid.Replace("page_category", ""), pidparent);
                    }
                    else
                    {
                        return loadChildTree(new Guid(parentid));
                    }
                }
                else
                {
                    return "[]";
                }

            }

        }
        protected string loadPageCategory(Guid linkid)
        {
            ILinkQueryService service = this.client.GetLinkQueryService(linkid);
            OqlBool criteria = ClientPage.Table.OwnerID.PureNameField == linkid &&
                (ClientPage.Table.UserID.PureNameField == this.client.User.UserID ||
                ClientPage.Table.IsShared.PureNameField != 0);
            DataCollection<ClientPage> pages = service.Load(typeof(ClientPage), criteria, false, 0) as DataCollection<ClientPage>;
            StringBuilder sb = new StringBuilder();

            Hashtable ht = new Hashtable();
            if (pages.Count > 0)
            {

                foreach (ClientPage cp in pages)
                {
                    if (ht.Contains(cp.Category))
                    {

                    }
                    else
                    {
                        sb.Append("{");
                        sb.AppendFormat("\"name\":\"{0}\",", cp.Category);
                        sb.AppendFormat("\"id\":\"{0}\",", "page_category" + cp.Category);
                        sb.AppendFormat("\"pid\":\"{0}\",", linkid.ToString());
                        sb.AppendFormat("\"isParent\":\"{0}\"", "true");
                        sb.Append("},");
                        ht.Add(cp.Category, "");
                    }

                }


            }


            return sb.ToString();
        }
        protected string pagesToJson(string category, string linkid)
        {
            ILinkQueryService service = this.client.GetLinkQueryService(new Guid(linkid));
            OqlBool criteria = ClientPage.Table.OwnerID.PureNameField == linkid &&
                (ClientPage.Table.UserID.PureNameField == this.client.User.UserID ||
                ClientPage.Table.IsShared.PureNameField != 0);
            DataCollection<ClientPage> pages = service.Load(typeof(ClientPage), criteria, false, 0) as DataCollection<ClientPage>;

            //陈勇 2016/5/25
            //表单录入页面不可共享
            criteria = InputPage.Table.OwnerID.PureNameField == linkid;
//#if DEBUG
//            criteria = InputPage.Table.OwnerID.PureNameField == linkid;   
//#else
//            criteria = InputPage.Table.OwnerID.PureNameField == linkid &&
//                (InputPage.Table.UserID.PureNameField == this.client.User.UserID);
//#endif

            DataCollection<InputPage> inputPages = service.Load(typeof(InputPage), criteria, false, 0) as DataCollection<InputPage>;
            if (inputPages != null)
            {
                pages?.AddRange(inputPages);
            }
            //陈勇 2016/5/25

            StringBuilder sb = new StringBuilder();
            if (pages.Count > 0)
            {
                sb.Append("[");
                foreach (ClientPage cp in pages)
                {
                    if (cp.Category == category)
                    {
                        sb.Append("{");
                        sb.AppendFormat("\"name\":\"{0}\",", cp.DisplayName);
                        sb.AppendFormat("\"id\":\"{0}\",", cp.PageID);
                        sb.AppendFormat("\"pid\":\"{0}\",", linkid.ToString());
                        sb.AppendFormat("\"isParent\":\"{0}\",", "false");
                        string para = "?pageid={0}&linkid={1}&title={2}";
                        para = string.Format(para, cp.PageID, linkid.ToString(), HttpUtility.UrlEncode(cp.DisplayName));
                        switch (cp.PageType)
                        {
                            case "MapPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/MapWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"MapPage\"");
                                break;
                            case "StagePage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/StageWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"StagePage\"");
                                break;
                            case "ExcelPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/ExcelWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"ExcelPage\"");
                                break;
                            case "HeatMapPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/HeatMapWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"HeatMapPage\"");
                                break;
                            case "RichEditPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/WordWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"RichEditPage\"");
                                break;
                            case "LayoutPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/LayoutWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"LayoutPage\"");
                                break;
                            case "TablePage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/TableWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"TablePage\"");
                                break;
                            case "InputFormPage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/InputWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"InputPage\"");
                                break;
                            case "SlidePage":
                                sb.AppendFormat("\"click\":\"beforeRedirect('Pages/SlideWebPage.aspx{0}');\",", para);
                                sb.Append("\"iconSkin\":\"SlidePage\"");
                                break;
                                
                            default:
                                break;
                        }

                        sb.Append("},");
                    }
                }
                if (sb.ToString().EndsWith(","))
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Append("]");
            }

            return sb.ToString();
        }
        protected string loadChildTree(Guid parentid)
        {


            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            //加载页面类型
            sb.Append(loadPageCategory(parentid));
            if (this.client.EmsLinks.FindByID(parentid).Children.Count > 0)
            {
                foreach (EmsLink el in this.client.EmsLinks.FindByID(parentid).Children)
                {
                    sb.Append("{");
                    sb.AppendFormat("\"name\":\"{0}\",", el.DisplayName);
                    sb.AppendFormat("\"id\":\"{0}\",", el.LinkID.ToString());
                    sb.AppendFormat("\"pid\":\"{0}\",", parentid.ToString());
                    sb.AppendFormat("\"isParent\":\"{0}\"", "true");
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
        protected string loadRootTree(string parentid)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (EmsLink el in this.client.EmsLinks)
            {
                if (el.ParentID == null)
                {
                    sb.Append("{");
                    sb.AppendFormat("\"name\":\"{0}\",", el.DisplayName);
                    sb.AppendFormat("\"id\":\"{0}\",", el.LinkID.ToString());
                    sb.Append("\"pid\":\"0\",");
                    sb.AppendFormat("\"isParent\":\"{0}\"", "true");
                    sb.Append("},");
                }
            }
            if (sb.ToString().EndsWith(","))
            {
                sb.Remove(sb.Length - 1, 1);
            }
            sb.Append("]");
            return sb.ToString();
        }

        protected string NodeToJson(DataCollection<EmsLink> EmsLinks, Guid? parentid)
        {
            StringBuilder sb = new StringBuilder();

            foreach (EmsLink el in EmsLinks)
            {
                if (el.ParentID == parentid)
                {
                    sb.Append("{");
                    sb.AppendFormat("\"name\":\"{0}\",", el.DisplayName);
                    sb.AppendFormat("\"id\":\"{0}\",", el.LinkID.ToString());
                    sb.AppendFormat("\"pid\":\"{0}\",", parentid == null ? "0" : parentid.ToString());
                    sb.AppendFormat("\"isParent\":\"{0}\"", el.ParentID == null ? "true" : "false");
                    if (el.Children.Count > 0)
                    {
                        sb.Append(",\"children\":[");
                        sb.Append(NodeToJson(el.Children, el.LinkID));
                        sb.Append("]");
                    }
                    sb.Append("},");
                }


            }
            if (sb.ToString().EndsWith(","))
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

    }
}
