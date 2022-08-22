using System;
using System.Collections.Generic;

namespace CrunchEconomy.Station_Stuff.Objects
{
    public class SellOffer
    {
        public Boolean Enabled = false;
        public bool BuyingGivesGps = false;
        public List<string> GpsToPickFrom = new List<string>();
        public string StationModifierItemName = "Example 100%";
        public string TypeId = "Ore";
        public string SubtypeId = "Iron";
        public long MinPrice = 1;
        public long MaxPrice = 3;
        public double Chance = 1;
        public Boolean SpawnItemsIfNeeded = false;
        public int SpawnIfCargoLessThan = 10;

        public int MinAmountToSpawn = 1;
        public int MaxAmountToSpawn = 5; 
        public Boolean BuyingGivesMiningContract = false;
        public string ContractName = "example";
        public Boolean BuyingGivesHaulingContract = false;
        public string IfGivesContractNpcTag = "GAIA";

        public Boolean IndividualRefreshTimer = false;
        public DateTime NextRefresh = DateTime.Now;
        public int SecondsBetweenRefresh = 600;
        public string Path = "ignore me";
    }
}
