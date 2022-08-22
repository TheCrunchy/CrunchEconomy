using System;
using System.Collections.Generic;

namespace CrunchEconomy.Station_Stuff.Objects
{
    public class WhitelistFile
    {
        public List<Whitelist> Values = new List<Whitelist>();
        public class Whitelist
        {
            public string ListName = "List1";
            public List<string> FactionTags = new List<string>();
        }
    }
}
