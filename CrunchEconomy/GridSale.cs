using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class GridSale
    {
        public Boolean Enabled = false;
        public string ItemTypeId = "Ingot";
        public string ItemSubTypeId = "Iron";

        public string StoreBlockName = "EXAMPLESGRIDSTORE";
        public string OwnerFactionTag = "GAIA";
        public string ExportedGridName = "BOB";
        public Boolean GiveOwnerShipToNpc = false;

        public Boolean PayPercentageToPlayer = false;
        public ulong SteamId = 0;
        public double Percentage = 0.05;
    }
}
