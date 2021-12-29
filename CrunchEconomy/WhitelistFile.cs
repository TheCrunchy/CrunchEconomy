using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class WhitelistFile
    {
        public List<Whitelist> whitelist = new List<Whitelist>();
        public class Whitelist
        {
            public string ListName = "List1";
            public List<String> FactionTags = new List<string>();
        }
    }
}
