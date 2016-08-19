using Br.Framework.Security.AppSettings;
using log4net.Config;
using System;
using System.Globalization;
using System.Collections.Generic;

using System.Threading;
using System.Web;

using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using Refine.Common;
using Refine.Orm;

using Ems.Common;
using Ems.Data;
using Ems.Expression;
using Ems.Interface;
using Ems.PlugIn;
using Ems.DeviceDriver;
using Ems.Data.Helper;

namespace Br.Framework.WebControls
{
    public class Global : System.Web.HttpApplication
    {
        private static object initLock = new object();
        private static bool inited = false;
        private static string rootPath;
        private static System.Threading.Timer timer;
        private static IUserPass userPass;
        private static UserEvidence evidence;


        private static string absoluteRootPath;

        public Thread schedulerThread = null;

        public Global()
        {
        }

        public void Initialize()
        {
            lock (initLock)
            {
                if (!inited)
                {
                    ConfigSettings configSetting = new ConfigSettings("BrEngine", AppDomain.CurrentDomain.BaseDirectory);
                    // 为兼容老数据而进行数据类型名替换
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Site.Utils.", "Ems.Data.Helper.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Site.", "Ems.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Business.", "Ems.Data.Common.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Controlling.", "Ems.Data.Common.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Transforming.", "Ems.Data.Helper.");

                    rootPath = AppDomain.CurrentDomain.BaseDirectory;//  获取跟路径


                    // 配置远程调用
                    RemotingConfiguration.Configure(Path.Combine(rootPath, "web.config"), true);

                    // 获取服务器信息
                    topServerEntry = configSetting.AppConfigItem("TopServerEntry");//AppUtils.GetAppSettingValue("TopServerEntry", "emscloud.cn:6721");
                    if (!topServerEntry.Contains(":"))
                        topServerEntry += ":6721";
                    reportServerEntry = configSetting.AppConfigItem("ReportServerEntry");//AppUtils.GetAppSettingValue("ReportServerEntry", "localhost:6734");
                    if (!reportServerEntry.Contains(":"))
                        reportServerEntry += ":6734";
                    //装入chart
                    Ems.Data.Helper.ActorManager.ActorLoader.AddAssembly(System.Reflection.Assembly.Load("Ems.Client.Chart"));
                    // 装入插件
                    string plugInPath = AppUtils.GetPath("PlugInPath", "PlugIn", rootPath);

                    OperatorManager.LoadOperators(Path.Combine(plugInPath, "Operator"));
                    ActorManager.LoadActors(Path.Combine(plugInPath, "Actor"));
                    ScopeKernelManager.LoadKernels(Path.Combine(plugInPath, "ScopeKernel"));
                    LogicKernelManager.LoadKernels(Path.Combine(plugInPath, "LogicKernel"));

                    SolutionLoader.LoadCommonPlugIn(Path.Combine(plugInPath, "Solution"), false, false, false, false, false, false, false, false);

                    //lly
                    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

                    string value = configSetting.AppConfigItem("ShowStackTrace"); //AppUtils.GetAppSettingValue("ShowStackTrace", "false");
                    showStackTrace = bool.Parse(value);

                    // 登录查询账户
                    string text = configSetting.AppConfigItem("Account");// AppUtils.GetAppSettingValue("Account");
                    byte[] data = CryptUtils.TextToBytes(text.Trim());
                    text = CryptUtils.DecryptStr(data);
                    Guid userID = new Guid(text);
                    text = configSetting.AppConfigItem("Password");// AppUtils.GetAppSettingValue("Password");
                    data = CryptUtils.TextToBytes(text);
                    data = CryptUtils.Decrypt(data);
                    TopServerBroker topServer = GetTopServer();
                    string error;
                    userPass = topServer.Login(userID, data, TimeSpan.FromMinutes(20), out error);
                    evidence = userPass.User.Evidence;
                    LoadServerMachines();

                    InitLazyLoadService(RemoteClientType.Viewer);

                    // 启动定时事件
                    timer = new System.Threading.Timer(new System.Threading.TimerCallback(timer_Timeout), null, 10000, 30000);
                    inited = true;
                }
            }
        }
        private static void timer_Timeout(object target)
        {
            // 发用户心跳
            userPass.Beat();
            for (int i = 0; i < clients.Count; i++)
            {
                Client client = clients[i];
                if (client != null)
                {
                    int count = client.AccessCount;
                    client.AccessCount -= count;
                    if (count > 0)
                        client.Beat();
                }
            }
        }
        protected static RemoteLazyLoadManager remoteLazyLoadManager = new RemoteLazyLoadManager();
        private static Dictionary<short, Ems.Data.ServerMachine> servers = new Dictionary<short, ServerMachine>();
        public static void InitLazyLoadService(RemoteClientType clientType)
        {
            remoteLazyLoadManager.ServerByCodeHandler += new ServerByCodeEventHandler(remoteLazyLoadManager_ServerByCodeHandler);

        }
        private static DataCollection<ServerMachine> machines = null;
        public static DataCollection<ServerMachine> Machines
        {
            get
            {
                return machines;
            }
        }

        public static void LoadServerMachines()
        {
            TopServerBroker topServer = GetTopServer();
            machines = topServer.GetServerMachines(null);

            servers.Clear();
            foreach (ServerMachine machine in machines)
                servers[machine.Code] = machine;
        }

        private static DataCollection<Resource> resources = null;
        public static DataCollection<Resource> Resources
        {
            get
            {
                if (resources == null)
                {
                    TopServerBroker topServer = GetTopServer();
                    resources = topServer.GetResources();
                }

                return resources;
            }
        }

        private static NodeLinkCollection nodeLinks = null;
        public static NodeLinkCollection NodeLinks
        {
            get
            {
                if (nodeLinks == null)
                {
                    TopServerBroker topServer = GetTopServer();
                    nodeLinks = topServer.GetNodeLinks();
                }
                return nodeLinks;
            }
        }

        static void remoteLazyLoadManager_ServerByCodeHandler(object sender, ServerByCodeEventArgs e)
        {
            if (e.Code == 0)
                e.Server = GetTopServer();
            else if (servers.ContainsKey(e.Code))
            {
                ServerMachine sv = servers[e.Code];
                e.Server = ConnectServer(sv.Address, sv.Port, typeof(ServerBroker));
            }

        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.FullName == args.Name || assembly.GetName().Name == args.Name)
                        return assembly;
                }

                return null;
            }
            catch (Exception e)
            {
                string msg = e.Message;
                return null;
            }
        }
        //for Authentification
        protected static string topServerEntry;
        public static string TopServerEntry
        {
            get
            {
                return topServerEntry;
            }
        }

        protected static string reportServerEntry;
        public static string ReportServerEntry
        {
            get
            {
                return reportServerEntry;
            }
        }

        private static bool showStackTrace;
        public static bool ShowStackTrace
        {
            get
            {
                return showStackTrace;
            }
        }
        public static string AbsoluteRootPath
        {
            get
            {
                return absoluteRootPath;
            }
        }

        public static ReportServerBroker GetReportServer()
        {
            string url = string.Format("tcp://{0}/ReportServer", reportServerEntry);
            return Activator.GetObject(typeof(ReportServerBroker), url) as ReportServerBroker;
        }

        public static TopServerBroker GetTopServer()
        {
            string url = string.Format("tcp://{0}/TopServer", topServerEntry);
            return Activator.GetObject(typeof(TopServerBroker), url) as TopServerBroker;
        }

        public static ServerBroker ConnectServer(string address, int port, Type serverType)
        {
            string uri = serverType == typeof(ServerBroker) ? "EmsServer" : serverType.Name.Replace("Broker", string.Empty);
            string url = "tcp://" + address + ":" + port + "/" + uri;
            return Activator.GetObject(serverType, url) as ServerBroker;
        }

        public static ServerBroker ConnectServer(ServiceEntry entry)
        {
            Type type = ServerBroker.GetServerType(entry.ServiceType);
            return ConnectServer(entry.Address, entry.Port, type);
        }

        public static ServerBroker ConnectServer(Guid serverID, Ems.Data.Common.ServiceType serviceType)
        {
            ServerMachine machine = machines.FindByID(serverID);
            if (machine != null)
            {
                ServiceEntry entry = machine.GetServiceEntry(serviceType);
                if (entry != null)
                    return ConnectServer(entry);
            }

            return null;
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(absoluteRootPath)) {
                absoluteRootPath = getBasePath();
            }
           

        }

        protected void Application_End(object sender, EventArgs e)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            userPass.Logout();
            foreach (Client client in clients)
                client.Logout();
        }
        private static SafeList<Client> clients = new SafeList<Client>();
        void Session_Start(object sender, EventArgs e)
        {
            // 在新会话启动时运行的代码
            Client client = new Client(Session);
            if (Session["Client"] as Client == null)
                Session["Client"] = client;

            clients.Add(client);

        }
        void Session_End(object sender, EventArgs e)
        {
            // 在会话结束时运行的代码。 
            Client client = (Client)Session["Client"];
            client.Logout();
            Session["Client"] = null;
            clients.Remove(client);

        }
        protected void Application_Error(object sender, EventArgs e)
        {
            this.ExceptionHandle();
        }
        
        
        protected void Application_Start(object sender, EventArgs e)
        {

            ConfigSettings configSetting = new ConfigSettings("BrEngine", AppDomain.CurrentDomain.BaseDirectory);
            Application.Add("configSetting", configSetting);
            if ((configSetting.AppConfigItem("LocalizeForms") == null ? false : configSetting.AppConfigItem("LocalizeForms").ToUpper() == "YES"))
            {
                this.loadResources();
            }
            XmlConfigurator.ConfigureAndWatch(new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "/config/log4net.config"));
            Initialize();

            //启动websocket服务
            int websocketPortNumber = int.Parse(configSetting.AppConfigItem("WebsocketPortNumber"));

        }
        private string getBasePath()
        {
            string str;
            string applicationPath = HttpContext.Current.Request.Url.AbsolutePath;
            if (!string.IsNullOrEmpty(applicationPath))
            {
                string absoluteUri = HttpContext.Current.Request.Url.AbsoluteUri;
                if (!string.IsNullOrEmpty(absoluteUri))
                {
                    int num = absoluteUri.ToLower().IndexOf(applicationPath.ToLower());
                    str = absoluteUri.Substring(0, num);
                }
                else
                {
                    str = "";
                }
            }
            else
            {
                str = "";
            }
            return str;
        }

        /// <summary>
        /// 记录系统异常使用
        /// </summary>
        private void ExceptionHandle()
        {
            string str = string.Concat(this.getBasePath(), "/", "Error.aspx");
            //base.Response.Redirect(str);
            //Exception lastError;
            //string str = string.Concat(this.getBasePath(), "/Error.aspx");
            //bool flag = false;
            //try
            //{
            //    lastError = base.Server.GetLastError();
            //    if (lastError.InnerException is SocketException)
            //    {
            //        flag = true;
            //    }
            //    this.settings = new ConfigSettings("E6Engine");
            //    this.connectString = this.settings.AppConfigItem("ConnectString");
            //    int userID = -1;
            //    try
            //    {
            //        userID = this.GetUserID();
            //    }
            //    catch
            //    {
            //        userID = -1;
            //    }
            //    string path = base.Request.Path;
            //    string rawUrl = base.Request.RawUrl;
            //    string pathAndQuery = "";
            //    if (base.Request.UrlReferrer != null)
            //    {
            //        pathAndQuery = base.Request.UrlReferrer.PathAndQuery;
            //    }
            //    if ((path.IndexOf("images/TreeIcons/Icons/") < 0 ? true : lastError.Message.IndexOf("文件不存在") < 0))
            //    {
            //        int num = (base.Request.Browser.ActiveXControls ? 1 : 0);
            //        string str1 = string.Concat(base.Request.Browser.Type, "(", base.Request.Browser.Version, ")");
            //        int num1 = (base.Request.Browser.Beta ? 1 : 0);
            //        int num2 = (base.Request.Browser.Cookies ? 1 : 0);
            //        int num3 = (base.Request.Browser.Frames ? 1 : 0);
            //        string platform = base.Request.Browser.Platform;
            //        int num4 = 1;
            //        string str2 = "";
            //        Version[] clrVersions = base.Request.Browser.GetClrVersions();
            //        for (int i = 0; i < (int)clrVersions.Length; i++)
            //        {
            //            if (i > 0)
            //            {
            //                str2 = string.Concat(str2, "；");
            //            }
            //            str2 = string.Concat(str2, Convert.ToString(clrVersions[i].Major + clrVersions[i].Minor));
            //        }
            //        string str3 = essDAException.insertExceptionStr(userID, lastError.GetType(), lastError, lastError.Message, path, rawUrl, pathAndQuery, num, num1, num2, num3, platform, num4, str2, str1);
            //        (new daUpdater(this.connectString)).executeNonQuery(str3, CommandType.Text);
            //        string str4 = this.settings.AppConfigItem("ErrorURL");
            //        if ((str4 == null ? false : str4 != ""))
            //        {
            //            if (flag)
            //            {
            //                str4 = "APP/WorkFlow/WorkFlowError.aspx";
            //            }
            //            str = string.Concat(this.getBasePath(), "/", str4);
            //            base.Response.Redirect(str);
            //        }
            //    }
            //    else
            //    {
            //        return;
            //    }
            //}
            //catch (Exception exception)
            //{
            //    lastError = exception;
            //    string message = lastError.InnerException.Message;
            //}
        }



        /// <summary>
        /// 获取用户id
        /// </summary>
        /// <returns></returns>
        private int GetUserID()
        {
            bool flag;
            int num = -1;
        
            return num;
        }

        private void loadResources()
        {
            //预留 多语言使用
        
        }
      

       
        
        //void Application_Start(object sender, EventArgs e)
        //{
        //    Initialize();
            
        //    // 在应用程序启动时运行的代码
        //    ApplicationContext.Instance.AppRootPath = Server.MapPath("");

        //    //Ioc类型注册
        //    IocController.Instance.RegisterType();

        //    //启动websocket服务
        //    int websocketPortNumber = int.Parse(ConfigurationManager.AppSettings["WebsocketPortNumber"]);
        //    BizWebSocketServer.Instance.Start(websocketPortNumber);

        //}


        //void Application_End(object sender, EventArgs e)
        //{
        //    //  在应用程序关闭时运行的代码
        //    //for Auth
        //    if (timer != null)
        //    {
        //        timer.Dispose();
        //        timer = null;
        //    }

        //    userPass.Logout();
        //    foreach (Client client in clients)
        //        client.Logout();

        //    //停止websocket服务
        //    BizWebSocketServer.Instance.Stop();
        //}

        //void Application_Error(object sender, EventArgs e)
        //{
        //    // 在出现未处理的错误时运行的代码

        //}

    


    }
}
