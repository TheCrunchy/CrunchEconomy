using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.Contracts
{
    public class RewardItem
    {
        public Boolean Enabled = false;
        public double Chance = 1;
        public int ItemMinAmount = 1;
        public int ItemMaxAmount = 2;
        public string TypeId = "Ingot";
        public string SubTypeId = "Uranium";
        public int ReputationRequired = 100;

    }
}
