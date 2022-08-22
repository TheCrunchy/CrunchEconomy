using System;
using System.Collections.Generic;
using CrunchEconomy.Helpers;
using Sandbox.Game.Screens.Helpers;

namespace CrunchEconomy.Station_Stuff.Objects
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
        public string StationGps = "Put a GPS here";
        public int SecondsBetweenRefreshForBuyOrders = 1800;
        public int SecondsBetweenRefreshForSellOffers = 60;
        public int SecondsBetweenClearEntireInventory = 432000;
        public string OwnerFactionTag = "GAIA";
        public DateTime NextBuyRefresh = DateTime.Now;
        public DateTime NextSellRefresh = DateTime.Now;
        public Boolean DoPeriodGridClearing = false;
        public DateTime NextGridInventoryClear = DateTime.Now;
        public long StationEntityId = 0;
        public string WorldName = "default";
        public Boolean GiveGpsOnLogin = false;
        public Boolean WhitelistedSafezones = false;
        public Boolean DoBlacklist = false;
        public List<String> Whitelist = new List<string>();
        public MyGps GetGps()
        {
            return GpsHelper.ParseGps(StationGps);
        }

        public List<PriceModifier> Modifiers = new List<PriceModifier>();
        private Dictionary<string, float> _priceModifiers = new Dictionary<string, float>();
        public void SetupModifiers()
        {
            _priceModifiers.Clear();
            foreach (var mod in Modifiers)
            {
                if (!_priceModifiers.ContainsKey(mod.StationModifierInItemFile))
                {
                    _priceModifiers.Add(mod.StationModifierInItemFile, mod.Modifier);
                }
            }
        }
        public float GetModifier(String Input)
        {
            if (_priceModifiers.TryGetValue(Input, out var f))
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

        public List<CraftedItem> CraftableItems = new List<CraftedItem>();
        public Boolean EnableStationCrafting = false;
        public int SecondsBetweenCrafting = 60;
        public DateTime NextCraftRefresh = DateTime.Now;
        
        public class RecipeItem
        {
            public string Typeid;
            public string Subtypeid;
            public int Amount;
        }

        public class CraftedItem
        {
            public string Typeid;
            public string Subtypeid;
            public double ChanceToCraft = 0.5;
            public int AmountPerCraft;
            public List<RecipeItem> RequriedItems = new List<RecipeItem>();
        }
    }
}
