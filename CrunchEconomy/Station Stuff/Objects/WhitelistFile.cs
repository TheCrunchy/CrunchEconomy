using System;
using System.Collections.Generic;

namespace CrunchEconomy.Station_Stuff.Objects
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
