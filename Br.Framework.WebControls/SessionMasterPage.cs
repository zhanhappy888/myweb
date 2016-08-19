using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;


namespace Br.Framework.WebControls
{
    public class SessionMasterPage : BaseMasterPage
    {
        public Client client;
   
        protected override void OnInit(EventArgs e)
        {
            AssertClient();
            base.OnInit(e);

        }

        protected bool AssertClient()
        {
            client = this.Session["Client"] as Client;
            if (client == null || !client.Loged)
            {
                base.Response.Redirect(this.rootPath+"/"+this.loginUrl, true);
                return false;
            }
            else {
                client.AccessCount++;
                return true;
            }
        }

    
    }
    
}
