﻿using Sandbox.Game.Screens.Helpers;
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
        public Boolean WhitelistedSafezones = false;
        public Boolean DoBlacklist = false;
        public List<String> Whitelist = new List<string>();
        public bool MaintainNPCBalance = false;
        public long MoneyToHave = 100000000000;
        public MyGps getGPS()
        {
            return CrunchEconCore.ParseGPS(stationGPS);
        }

        public List<PriceModifier> Modifiers = new List<PriceModifier>();
        private Dictionary<string, float> PriceModifiers = new Dictionary<string, float>();
        public void SetupModifiers()
        {
            PriceModifiers.Clear();
            foreach (PriceModifier mod in Modifiers.Where(x => x.Enabled))
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
            public bool Enabled = false;
            public float Max = 1.3f;
            public float Min = 0.7f;
            public DateTime NextRefresh = DateTime.Now;
            public int SecondsBetweenRefreshes = 43200;
            public string StationModifierInItemFile = "Example 100%";
        }

        public List<CraftedItem> CraftableItems = new List<CraftedItem>();
        public Boolean EnableStationCrafting = false;
        public int SecondsBetweenCrafting = 60;
        public DateTime nextCraftRefresh = DateTime.Now;
        
        public class RecipeItem
        {
            public string typeid;
            public string subtypeid;
            public int amount;
        }

        public class CraftedItem
        {
            public bool Enabled = true;
            public bool OnlyCraftIfStationSellsThisItem = false;
            public string typeid;
            public string subtypeid;
            public double chanceToCraft = 0.5;
            public int amountPerCraft;
            public List<RecipeItem> RequriedItems = new List<RecipeItem>();
        }
    }
}
