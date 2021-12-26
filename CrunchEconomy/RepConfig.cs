using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class RepConfig
    {
        public List<RepItem> RepConfigs = new List<RepItem>();
        public class RepItem
        {
            public Boolean Enabled = false;
            public string FactionTag = "EXP";
            public int FactionToFactionRep = 1500;
            public int PlayerToFactionRep = 1500;
            public Boolean AlwaysAtWar = false;
        }
    }
}
