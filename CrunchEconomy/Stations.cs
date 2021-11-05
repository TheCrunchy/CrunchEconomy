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
        public Boolean UseAsDeliveryLocationForContracts = true;
        public String Name = "Example";
        public Boolean Enabled = false;
        public Boolean DoBuyOrders = true;
        public Boolean DoSellOffers = true;
        public Boolean ViewOnlyNamedCargo = false;
        public string CargoName = "bob";
        public string stationGPS = "Put a GPS here";
        public int SecondsBetweenRefreshForBuyOrders = 1800;
        public int SecondsBetweenRefreshForSellOffers = 60;
        public int SecondsBetweenClearEntireInventory = 432000;
        public string OwnerFactionTag = "GAIA";
        public DateTime nextBuyRefresh = DateTime.Now;
        public DateTime nextSellRefresh = DateTime.Now;
        public Boolean DoPeriodGridClearing = false;
        public DateTime nextGridInventoryClear = DateTime.Now;
        public long StationEntityId = 0;
        public string WorldName = "default";
        public Boolean GiveGPSOnLogin = false;

        public MyGps getGPS()
        {
            return CrunchEconCore.ScanChat(stationGPS);
        }

        public List<PriceModifier> Modifiers = new List<PriceModifier>();
        private Dictionary<string, float> PriceModifiers = new Dictionary<string, float>();
        public void SetupModifiers()
        {
            PriceModifiers.Clear();
            foreach (PriceModifier mod in Modifiers)
            {
                if (!PriceModifiers.ContainsKey(mod.StationModifierInItemFile))
                {
                    PriceModifiers.Add(mod.StationModifierInItemFile, mod.Modifier);
                }
            }
        }
        public float GetModifier(String input)
        {
            if (PriceModifiers.TryGetValue(input, out float f))
            {
                return f;

            }

            return 1f;
        }
     //   public Boolean DoCraftingFromInventory = false;
      //  public int SecondsBetweenCrafting = 60;
      public class PriceModifier
        {
            public float Modifier = 1.0f;
            public string StationModifierInItemFile = "Example 100%";
        }
    }
}
