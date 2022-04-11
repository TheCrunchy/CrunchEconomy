using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.Contracts
{
    public class GeneratedContract
    {
        public ContractType type = ContractType.Mining;
        public string Name = "Example1";
        public Boolean Enabled = false;
        public int ReputationGain = 1;
        public int minimum = 450000;
        public int maximum = 500000;
        public double PricePerOre = 450;
        public Boolean PutTheHaulInStation = false;
        public Boolean SpawnItemsInPlayerInvent = false;
        public List<RewardItem> PlayerLoot = new List<RewardItem>();
        public string StationCargoName = "Change this";
        public List<RewardItem> PutInStation = new List<RewardItem>();
        public int CooldownInSeconds = 1;
        public List<ContractInfo> ItemsToPickFrom = new List<ContractInfo>();
        public class ContractInfo
        {
            public string TypeId = "Ore";
            public string SubTypeId = "Stone";
            public float chance = 100;
       
        }
        public List<StationDelivery> StationsToDeliverTo = new List<StationDelivery>();

        public class StationDelivery
        {
            public string Name = "TAKEN";
            public double chance = 100;
           
        }
    }
}
