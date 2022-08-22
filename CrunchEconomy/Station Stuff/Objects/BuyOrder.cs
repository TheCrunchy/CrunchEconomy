using System;

namespace CrunchEconomy.Station_Stuff.Objects
{
    public class BuyOrder
    {
        public Boolean Enabled = false;
        public string StationModifierItemName = "Example 100%";
        public string TypeId = "Ore";
        public string SubtypeId = "Iron";
        public int MinAmount = 10;
        public int MaxAmount = 500;
        public long MinPrice = 1;
        public long MaxPrice = 3;

        public double Chance = 0.5;

        public Boolean DeleteTheseItemsInCargOnRefresh = false;

        public Boolean SellingThisCancelsContract = false;
        public string ContractToCancel = "Example";
        public string FactionTagOwnerForCancelling = "GAIA";

        public Boolean IndividualRefreshTimer = false;
        public DateTime NextRefresh = DateTime.Now;
        public int SecondsBetweenRefresh = 600;
        public string Path = "ignore me";
    }
}
