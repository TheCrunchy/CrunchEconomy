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

        public int MinimumRepRequiredForItem = 50;
        public Boolean DoRareItemReward = false;
        public double ItemRewardChance = 1;
      
        public string RewardItemType = "Ingot";
        public string RewardItemSubType = "Uranium";
        public double ItemRewardAmount = 5;
    }
}
