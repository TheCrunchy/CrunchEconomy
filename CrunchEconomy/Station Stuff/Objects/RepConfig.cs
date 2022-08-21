using System;
using System.Collections.Generic;

namespace CrunchEconomy.Station_Stuff.Objects
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
