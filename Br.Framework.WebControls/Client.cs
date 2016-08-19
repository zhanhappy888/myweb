using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.SessionState;
using System.Threading;
using System.Web.Services.Protocols;
using Br.Common;
using Refine.Oql;
using Refine.Orm;

using Refine.Common;

using Ems.Data;
using Ems.Data.Common;
using Ems.Expression;
using Ems.Interface;
using Br.Framework.Security.AppSettings;
using Ems.Client.Lib;

namespace Br.Framework.WebControls
{
    public enum LoginError { NoError, LogNameError, PasswordError, MultiLogin };

    public class Client:IPageFactory
    {
        protected HttpSessionState session;
        protected string computer;
        protected string ipAddress;
        public IUserPass cur_userpass;

        public Client(HttpSessionState session)
        {
            this.session = session;
        }

        private RWLocked<int> accessCount = new RWLocked<int>(0);
        public int AccessCount
        {
            get
            {
                return this.accessCount.Value;
            }
            set
            {
                this.accessCount.Value = value;
            }
        }

        public bool Loged
        {
            get
            {
                return user != null;
            }
        }

        private static IUserPass userPass;
        public static IUserPass UserPass
        {
            get
            {
                return userPass;
            }
        }

        protected IdentifiedUser user = null;
        public IdentifiedUser User
        {
            get
            {
                return user;
            }
        }

        protected EmsTreeCollection emsTrees;
        public EmsTreeCollection EmsTrees
        {
            get
            {
                return emsTrees;
            }
        }

        protected DataCollection<EmsLink> emsLinks;
        public DataCollection<EmsLink> EmsLinks
        {
            get
            {
                return emsLinks;
            }
        }

    

        private DataCollection<EmsNode> emsNodes = new DataCollection<EmsNode>();
        public EmsNode GetEmsNode(Guid nodeID)
        {
            EmsNode node = emsNodes.FindByID(nodeID);
            if (node == null)
            {
                ServerBroker server = GetDefaultServer();
                node = server.GetEmsNode(nodeID);
                if (node != null)
                {
                    node.MapReferences(Global.Machines);
                    emsNodes.Add(node);
                }
            }

            return node;
        }
        public ServerBroker GetDefaultServer()
        {
            if (user.OwnerID == null || userEntry == null)
                return Global.GetTopServer();
            else
                return Global.ConnectServer(userEntry);
        }
        private void LoadEmsTrees(ServiceEntry entry, IdentifiedUser user)
        {
            userEntry = entry;
            ServerBroker server = Global.ConnectServer(entry);

            // 装入分类目录和分类节点
            emsTrees = server.GetEmsTrees(user.Evidence);
            emsLinks = emsTrees.GetAllLinks();

            // 映射服务器
            emsTrees.MapReferences(Global.Machines);
        }

        private static TimeSpan leaseTimeOut = TimeSpan.FromMinutes(20);
        private ServiceEntry userEntry;
        public bool Logon(string account, byte[] password, out string msg)
        {
            string accountType = "loginname";
            if (account.Contains("@"))
                accountType = "email";
            else if (Refine.Common.SysUtils.IsNumeric(account))
            {
                if (account.Length == 11 && account[0] == '1')
                    accountType = "mobilephone";
            }

            TopServerBroker topServer = Global.GetTopServer();
            Guid? userID;
            string error;
            ServiceEntry entry = topServer.LocateUser(ServiceType.Query, accountType, account, out userID, out error);
            if (!string.IsNullOrEmpty(error))
            //throw new SoapException(error, SoapException.ClientFaultCode);
            {
                msg = "login failed";
                return false;
            }
            else if (userID == null || entry == null)
            //throw new SoapException("系统内部错误。", SoapException.ClientFaultCode);
            {
                msg = "login failed";
                return false;
            }
            ServerBroker server = Global.ConnectServer(entry.Address, entry.Port, typeof(ServerBroker)) as ServerBroker;
            try
            {
                userPass = server.Login((Guid)userID, password, leaseTimeOut, out msg);
            }
            catch (Exception e)
            {
                throw e;
            }
            if (userPass != null)
            {
                // 设置用户的角色类型和所属所辖节点
                this.cur_userpass = userPass;
                user = userPass.User;
                LoadEmsTrees(entry, user);
                //user.MapEmsNodes(emsNodes);
                user.BuildPermissions(Global.Resources, emsLinks);

                return true;
            }

            return false;
        }

        public void Logout()
        {
            if (Loged)
            {
                try
                {
                    userPass.Logout();
                    userPass = null;
                    user = null;
                }
                catch
                {
                }
            }
        }

        private Ems.Data.Common.EventTimer timer = new EventTimer(TimeSpan.FromSeconds(30));
        public void Beat()
        {
            if (timer.IsTimeout)
            {
                timer.Reset();
                if (Loged)
                {
                    try
                    {
                        userPass.Beat();
                    }
                    catch
                    {
                    }
                }
            }
        }

        public IDataLoader GetDataLoader(Guid? nodeID)
        {
            EmsNode node = this.emsNodes.FindByID(nodeID);
            ServerBroker server = Global.ConnectServer(node.QueryEntry);
            return server.GetDataLoader(nodeID, user.Evidence, TimeSpan.FromMinutes(1));
        }

        public Guid? GetQueryServerID(EmsLink link)
        {
            if (link != null)
            {
                Guid? nodeID = link.GetDataNodeID();
                if (nodeID == null)
                {
                    EmsTree tree = emsTrees.FindByCode(link.TreeCode);
                    if (tree != null)
                        return tree.QueryServerID;
                }
                else
                {
                    EmsNode node = GetEmsNode((Guid)nodeID);
                    if (node != null)
                        return node.QueryServerID;
                }
            }

            return null;
        }

        public Guid? FindLinkID(Guid nodeID, int? scopeID)
        {
            Guid? linkID = Global.NodeLinks.FindLinkID(nodeID, scopeID);
            if (linkID == null)
            {
                EmsLink[] links = emsTrees.LinksByNodeID(nodeID, scopeID);
                if (links != null && links.Length > 0)
                    linkID = links[0].LinkID;
            }

            return linkID;
        }

        public Guid GetLinkID(string extNodeID)
        {
            string[] items = extNodeID.Split('.');
            if (items[0].ToLower() == "link")
                return new Guid(items[1]);

            Guid nodeID = new Guid(items[0]);
            int? scopeID = items.Length < 2 ? null : (int?)int.Parse(items[1]);
            Guid? linkID = FindLinkID(nodeID, scopeID);
            if (linkID != null)
                return (Guid)linkID;

            return nodeID;
        }


        //public static INodeQueryService GetNodeQueryService(Guid nodeID, int? scopeID)
        //{
        //    EmsNode node = GetEmsNode(nodeID);
        //    QueryServerBroker server = ConnectServer(GetServiceEntry(node, ServiceType.Query)) as QueryServerBroker;
        //    INodeQueryService queryService = server.GetNodeQueryService(nodeID, scopeID, user.Evidence, TimeSpan.FromMinutes(1));
        //    if (queryService == null)
        //        //MessageBox.Show("您未被授予查询该节点的权限！");
        //        throw new Exception("您未被授予查询该节点的权限！");
        //    else
        //        queryService = new NodeQueryServiceProxy(queryService, true, TimeSpan.FromMinutes(1));

        //    return queryService;
        //}

        public  ILinkQueryService GetLinkQueryService(Guid linkID)
        {
            //useri.MapEmsLinks(emsLinks);
            EmsLink link = emsLinks.FindByID(linkID);
            Guid? serverID = GetQueryServerID(link);
            ServerMachine machine = Global.Machines.FindByID(serverID);
            if (machine != null)
            {
                QueryServerBroker server = Global.ConnectServer(machine.GetServiceEntry(ServiceType.Query)) as QueryServerBroker;
                ILinkQueryService queryService = server.GetLinkQueryService(linkID, user.Evidence, TimeSpan.FromMinutes(1));
                if (queryService == null)
                    //MessageBox.Show("您未被授予查询该节点的权限！");
                    throw new Exception("您未被授予查询该节点的权限！");
                else
                    queryService = new LinkQueryServiceProxy(queryService, false, TimeSpan.FromMinutes(1));//web端不缓存数据
                
                return queryService;
            }

            return null;
        }
        #region 
        public ClientPage LoadClientPage(Guid linkid, Guid pageid)
        {
            ILinkQueryService service = this.GetLinkQueryService(linkid);
            return service.Load(typeof(ClientPage), pageid) as ClientPage;
        }

        public DataCollection<ClientPage> Pages
        {
            get
            {
                return null;
            }
        }
        #endregion 
    }
}
