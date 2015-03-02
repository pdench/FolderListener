using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolderListener
{
    public class Client
    {

        private string clientName="";
        private string fye = "12";

        public string ClientName
        {
            get
            {
                return clientName;
            }
            set
            {
                clientName = value;
            }
        }

        public string FYE
        {
            get
            {
                return fye.Trim();
            }
            set
            {
                fye = value;
            }
        }

    }
}
