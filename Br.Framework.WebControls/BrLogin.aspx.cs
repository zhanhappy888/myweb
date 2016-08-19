using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ems.Data;
using System.Web.Services;



namespace Br.Framework.WebControls
{
    public partial class BrLogin : BasePage
    {
        protected Client client;
        protected Button btnLogin;

        protected System.Web.UI.HtmlControls.HtmlInputText username;
        protected System.Web.UI.HtmlControls.HtmlInputPassword password;
        protected void Page_Load(object sender, EventArgs e)
        {
            btnLogin.Click += new EventHandler(btnLogin_Click);
        }


        void btnLogin_Click(object sender, EventArgs e)
        {
            Client tclient = this.client;
            try
            {
                string account = username.Value;
                string pwd = password.Value;
                Logon(account, pwd);
                if (!this.isMobile)
                {
                    if (this.client.User == null)
                        return;
                    //login succeed
                    this.Session["userName"] = this.client.User.ToString();
                    string returnUrl = null;
                    if (this.Session["returnUrl"] != null)
                        returnUrl = this.Session["returnUrl"].ToString();

                    if (returnUrl != null)
                    {
                        this.Session["returnUrl"] = null;
                        this.Response.Redirect(Server.UrlEncode(returnUrl));
                    }
                    else
                    {
                        base.Response.Redirect(this.mainUrl, true);
                    }
                }
                else {
                    if (this.client.User == null)
                    {
                        return;
                     
                    }
                    else {
                        base.Response.Redirect("main.html", true);
                       
                    }
              
                
                }

            }
            finally
            {
                this.client = tclient;
            }
        }

        [WebMethod(EnableSession = true)]
        public string MobileLogon()
        {

            string account = Request.Params["account"];
            string password = Request.Params["password"];
            Client client = HttpContext.Current.Session["Client"] as Client;
            try
            {
                Logon(account, password);
                if (this.client.User == null)
                    return "{'err','登录失败'}";
                //login succeed
                this.Session["userName"] = this.client.User.ToString();
                return "{'success','"+ this.Session["userName"]+"登录成功'}";

            }
            finally
            {
                this.client = client;
            }
        }

        public UserEvidence Logon(string account, string password)
        {
            account = HttpUtility.UrlDecode(account);
            password = HttpUtility.UrlDecode(password);
            client = this.Session["Client"] as Client;

            if (client != null)
            {
                byte[] data = Ems.Common.PasswordUtils.HashTransform(password);
                string msg;
                if (client.Logon(account, data, out msg))
                {
                    this.Session["Client"] = client;
                    return client.User.Evidence;
                }
                else
                {
                    client.Logout();
                    this.Response.Write("<script language='javascript'>alert('登录失败！请重新登录！');window.location.href=window.location.href;</script>");
                }
            }

            return null;
        }


    }
}
