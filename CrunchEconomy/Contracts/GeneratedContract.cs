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
        public double chance = 100;
        public string TypeIfHauling = "Ingot";
        public string SubType = "Iron";
        public int minimum = 450000;
        public int maximum = 500000;
        public int PricePerOre = 450;
        public Boolean SpawnItemsInPlayerInvent = false;
        public List<RewardItem> PlayerLoot = new List<RewardItem>();
        public string StationCargoName = "Change this";
        public List<RewardItem> PutInStation = new List<RewardItem>();
    }
}
