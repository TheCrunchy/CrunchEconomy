using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class BuyOrder
    {
        public Boolean Enabled = false;
        public string typeId = "Ore";
        public string subtypeId = "Iron";
        public int minAmount = 10;
        public int maxAmount = 500;
        public long minPrice = 1;
        public long maxPrice = 3;

        public double chance = 0.5;
    }
}
