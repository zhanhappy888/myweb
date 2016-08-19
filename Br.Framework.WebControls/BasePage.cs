using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Br.Framework.Security;
using Br.Framework.Security.AppSettings;
using Br.Common;


namespace Br.Framework.WebControls
{
    public class BasePage: System.Web.UI.Page
    {
        public bool isMobile = false;

        protected string loginUrl;

        protected string mainUrl;

        protected string noPermissionUrl;

        protected string sessionExpiredUrl;

        protected string uiConnectString;

        protected string scrollY = "0";

        protected string isWritePerfomanceLog;

        protected string uniqueMark;

        protected string rootPath;
        protected ConfigSettings settings;

        public string MainURL
        {
            get
            {
                return this.mainUrl;
            }
        }
        public string RootPath
        {
            get
            {
                return this.rootPath;
            }
        }
        public string LoginURL
        {
            get
            {
                return this.loginUrl;
            }
        }
        public string NoPermissionUrl
        {
            get
            {
                return this.noPermissionUrl;
            }
        }


        protected override void OnPreInit(EventArgs e)
        {
            settings = Application.Get("configSetting") as ConfigSettings;
            this.getSettings();
            //zjangjl 2016-1-4 判断是否为移动端设备
            HttpBrowserCapabilities b = Request.Browser;
            isMobile = b.IsMobileDevice;

            base.OnPreInit(e);
        }
        protected virtual void getSettings()
        {
            this.loginUrl = this.settings.AppConfigItem("LoginURL");
            this.mainUrl = this.settings.AppConfigItem("MainURL");
            this.ErrorPage = this.settings.AppConfigItem("ErrorURL");
            this.sessionExpiredUrl = this.settings.AppConfigItem("SessionExpiredUrl");
            this.isWritePerfomanceLog = this.settings.AppConfigItem("IsWritePerfomanceLog");
            Guid guid = Guid.NewGuid();
            this.uniqueMark = guid.ToString().ToLower();
            this.rootPath = Global.AbsoluteRootPath;
        }

       

    }
}
