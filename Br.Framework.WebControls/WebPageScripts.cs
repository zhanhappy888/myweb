using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;

namespace Br.Framework.WebControls
{
    public class WebPageScripts
    {
        private string canvasScript;
        private string updateScript;
        private string initScript;
        private string optionsb;
        private string timerefreshStr;   //定时刷新对象，by zhanj
        private string getfreshRateStr;   //by zhanj
        private string canvasid;  //传出stagepage主页的canvasid
        #region 初始化函数
        public WebPageScripts(string canvasScript,string updateScript, string initScript, string optionsb, string timerefreshStr, string getfreshRateStr)
        {
            this.canvasScript = canvasScript;
            this.updateScript = updateScript;
            this.initScript = initScript;
            this.optionsb = optionsb;
            this.timerefreshStr = timerefreshStr;
            this.getfreshRateStr = getfreshRateStr;
        }
        public WebPageScripts()
        {
            this.canvasScript = "";
            this.updateScript = "";
            this.initScript = "";
            this.optionsb = "";
            this.timerefreshStr = "";
            this.getfreshRateStr = "";
            this.canvasid = "";
        }
        #endregion

        #region 属性
        public string UpdateScript
        {
            get
            {
                return updateScript;
            }

            set
            {
                updateScript = value;
            }
        }

        public string InitScript
        {
            get
            {
                return initScript;
            }

            set
            {
                initScript = value;
            }
        }

        public string Optionsb
        {
            get
            {
                return optionsb;
            }

            set
            {
                optionsb = value;
            }
        }

        public string TimerefreshStr
        {
            get
            {
                return timerefreshStr;
            }

            set
            {
                timerefreshStr = value;
            }
        }

        public string GetfreshRateStr
        {
            get
            {
                return getfreshRateStr;
            }

            set
            {
                getfreshRateStr = value;
            }
        }

        public string CanvasScript
        {
            get
            {
                return canvasScript;
            }

            set
            {
                canvasScript = value;
            }
        }

        public string CanvasId
        {
            get
            {
                return canvasid;
            }

            set
            {
                canvasid = value;
            }
        }
        #endregion

        #region 连接另一个WebPageScripts的函数
        public WebPageScripts concat(WebPageScripts wps)
        {
            StringBuilder canvasScriptSB = new StringBuilder(canvasScript);
            StringBuilder updateScriptSB = new StringBuilder(updateScript);
            StringBuilder initScriptSB = new StringBuilder(initScript);
            StringBuilder optionsbSB = new StringBuilder(optionsb);
            StringBuilder timerefreshStrSB = new StringBuilder(timerefreshStr);
            StringBuilder getfreshRateStrSB = new StringBuilder(getfreshRateStr);
            //StringBuilder canvasIdSB = new StringBuilder(canvasid);

            canvasScriptSB.Append(wps.canvasScript);
            updateScriptSB.Append(wps.updateScript);
            initScriptSB.Append(wps.initScript);
            optionsbSB.Append(wps.optionsb);
            timerefreshStrSB.Append(wps.timerefreshStr);
            getfreshRateStrSB.Append(wps.getfreshRateStr);

            this.canvasScript = canvasScriptSB.ToString();
            this.updateScript = updateScriptSB.ToString();
            this.initScript = initScriptSB.ToString();
            this.optionsb = optionsbSB.ToString();
            this.timerefreshStr = timerefreshStrSB.ToString();
            this.getfreshRateStr = getfreshRateStrSB.ToString();
            if (this.canvasid == "")   //获取cavasid的值？？
            {
                this.CanvasId = wps.CanvasId;
            }
            return this;

        }

        #endregion
    }
}
