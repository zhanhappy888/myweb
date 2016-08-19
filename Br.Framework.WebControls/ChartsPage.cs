using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Br.Framework.WebControls
{
    public class ChartsPage : SessionPage
    {
        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            //动态注册js 页面添加的js需要放到body末尾
            ClientScript.RegisterClientScriptInclude("jquery", rootPath + "/Scripts/js/jquery-1.10.2.js");
            ClientScript.RegisterClientScriptInclude("jquery-ui", rootPath + "/Scripts/js/jquery-ui-1.10.4.custom.js");
            ClientScript.RegisterClientScriptInclude("jquery.fullpage", rootPath + "/js/jquery.fullpage.min.js");
            ClientScript.RegisterClientScriptInclude("brinclude", rootPath + "/Scripts/brinclude.js");
            ClientScript.RegisterClientScriptInclude("echarts-all", rootPath + "/Scripts/echarts-all.js");
            ClientScript.RegisterClientScriptInclude("jsonFormater", rootPath + "/Scripts/jsonFormater.js");
       
            if (!this.isMobile)
            {
                ClientScript.RegisterClientScriptInclude("jquery.mobile", rootPath + "/Scripts/bootstrap.min.js");
                ClientScript.RegisterStartupScript(this.GetType(), "ismobile", " var ismobile =false;", true);
            }
            else {
                ClientScript.RegisterClientScriptInclude("jquery.mobile", rootPath + "/Scripts/jquery.mobile-1.4.5.min.js");
                ClientScript.RegisterStartupScript(this.GetType(), "ismobile", " var ismobile =true;", true);
            }
        }
    }
}
