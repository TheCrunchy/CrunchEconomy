using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class Stations
    {
        public String Name = "Example";
        public Boolean Enabled = false;
        public Boolean DoBuyOrders = true;
        public Boolean DoSellOffers = true;
        public Boolean ViewOnlyNamedCargo = false;
        public string CargoName = "bob";
        public string stationGPS = "Put a GPS here";
        public int SecondsBetweenRefreshForBuyOrders = 1800;
        public int SecondsBetweenRefreshForSellOffers = 60;
        public string OwnerFactionTag = "GAIA";
        public DateTime nextBuyRefresh = DateTime.Now;
        public DateTime nextSellRefresh = DateTime.Now;

        public MyGps getGPS()
        {
            return CrunchEconCore.ScanChat(stationGPS);
        }
    }
}
