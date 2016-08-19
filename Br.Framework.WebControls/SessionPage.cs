using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;


namespace Br.Framework.WebControls
{
    public class SessionPage : BasePage
    {
        public Client client;
   
        protected override void OnPreInit(EventArgs e)
        {
            AssertClient();
            base.OnPreInit(e);

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
