
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Br.Framework.Security.AppSettings;

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
    public class brGlobal
    {
        private static object initLock = new object();
        private static bool inited = false;
        private static string rootPath;
        private static System.Threading.Timer timer;
        private static IUserPass userPass;
        private static UserEvidence evidence;

        public void Initialize(string root)
        {
            lock (initLock)
            {
                if (!inited)
                {
                    ConfigSettings configSetting = new ConfigSettings("BrEngine", root);
                    // 为兼容老数据而进行数据类型名替换
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Site.Utils.", "Ems.Data.Helper.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Site.", "Ems.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Business.", "Ems.Data.Common.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Controlling.", "Ems.Data.Common.");
                    Refine.Common.TypeUtils.AddReplaceToken("Ems.Transforming.", "Ems.Data.Helper.");

                    rootPath = root;

                    // 配置远程调用
                    RemotingConfiguration.Configure(Path.Combine(rootPath, "web.config"), true);

                    // 获取服务器信息
                    topServerEntry = configSetting.AppConfigItem("TopServerEntry");//AppUtils.GetAppSettingValue("TopServerEntry", "emscloud.cn:6721");
                    if (!topServerEntry.Contains(":"))
                        topServerEntry += ":6721";
                    reportServerEntry = configSetting.AppConfigItem("ReportServerEntry");//AppUtils.GetAppSettingValue("ReportServerEntry", "localhost:6734");
                    if (!reportServerEntry.Contains(":"))
                        reportServerEntry += ":6734";

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

            // 装入实时统计值
            //CacheCenter.LoadDirtyValues();
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

        private static SafeList<Client> clients = new SafeList<Client>();

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
    }
}
