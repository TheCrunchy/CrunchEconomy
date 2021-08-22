using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using Torch.API.Managers;
using System.IO;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Torch.Mod.Messages;
using Torch.Mod;
using Torch.Managers.ChatManager;
using Torch.Managers;
using Torch.API.Plugins;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;
using Sandbox.Game.Screens.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using NLog;

namespace CrunchEconomy
{
    public class CrunchEconCore : TorchPluginBase
    {
        public static TorchSessionState TorchState;
        public static ITorchBase TorchBase;

        public static Logger Log = LogManager.GetLogger("CrunchEcon");

        public static Config config;

        private TorchSessionManager sessionManager;
        public static string path;
        public static string basePath;
        public DateTime NextFileRefresh = DateTime.Now.AddMinutes(1);


        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id)
        {
            MyFixedPoint targetAmount = 0;
            foreach (VRage.Game.ModAPI.IMyInventory inv in inventories)
            {
                VRage.Game.ModAPI.IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    targetAmount += invItem.Amount;
                }
            }
            return targetAmount;
        }
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {

                for (int i = 0; i < block.InventoryCount; i++)
                {
                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }

        int ticks = 0;
        public override void Update()
        {
            ticks++;
            if (DateTime.Now >= NextFileRefresh)
            {
                NextFileRefresh = DateTime.Now.AddMinutes(2);
                Log.Info("Loading stuff for CrunchEcon");
                try
                {
                    LoadAllSellOffers();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Sell offers " + ex.ToString());


                }
                try
                {
                    LoadAllStations();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Stations " + ex.ToString());


                }
                try
                {
                    LoadAllBuyOrders();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Buy Orders " + ex.ToString());


                }


            }

            if (ticks % 32 == 0 && TorchState == TorchSessionState.Loaded)
            {
                DateTime now = DateTime.Now;
                foreach (Stations station in stations)
                {
                    //first check if its any, then we can load the grid to do the editing
                    try
                    {
                        if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh)
                        {
                            MyGps gps = station.getGPS();
                            BoundingSphereD sphere = new BoundingSphereD(gps.Coords, 200);

                            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                            {
                                foreach (MyStoreBlock store in grid.GetFatBlocks().OfType<MyStoreBlock>())
                                {
                                    if (store.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                                    {

                                        if (now >= station.nextSellRefresh && station.DoSellOffers)
                                        {
                                            station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                                            if (sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                            {
                                               

                                                ClearStoreOfPlayersBuyingOffers(store);
                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                inventories.AddRange(GetInventories(grid));
                                                Random rnd = new Random();
                                                foreach (SellOffer offer in offers)
                                                {
                                                 
                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + offer.typeId, offer.subtypeId, out MyDefinitionId id))
                                                    {
                                                     
                                                        int hasAmount = CountComponents(inventories, id).ToIntSafe();
                                                        if (hasAmount > 0)
                                                        {
                                                       
                                                            SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);


                                                            rnd = new Random();

                                                            int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);

                                                            MyStoreItemData item = new MyStoreItemData(itemId, hasAmount, price, null, null);
                                                            MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);
                                                            if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                            {
                                                                Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name  + " " + result.ToString());
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                         


                                        }
                                        if (now >= station.nextBuyRefresh && station.DoBuyOrders)
                                        {
                                            station.nextBuyRefresh = now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);
                             
                                            if (buyOrders.TryGetValue(store.DisplayNameText, out List<BuyOrder> orders))
                                            {
                                                
                                                station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                                                ClearStoreOfPlayersSellingOrders(store);
                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                inventories.AddRange(GetInventories(grid));
                                                Random rnd = new Random();
                                                foreach (BuyOrder order in orders)
                                                {
                                                
                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + order.typeId, order.subtypeId, out MyDefinitionId id))
                                                    {

                                                        SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, order.subtypeId);

                                                        rnd = new Random();
                                                        double chance = rnd.NextDouble();
                                                        if (chance <= order.chance)
                                                        {
                                                        int price = rnd.Next((int)order.minPrice, (int)order.maxPrice);
                                                        int amount = rnd.Next((int)order.minAmount, (int)order.maxAmount);
                                                        MyStoreItemData item = new MyStoreItemData(itemId, amount, price, null, null);
                                                            MyStoreInsertResults result = store.InsertOrder(item, out long notUsingThis);
                                                            if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                            {
                                                                Log.Error("Unable to insert this order into store " + order.typeId + " " + order.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                        }

                                    }
                                }
                                SaveStation(station);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SaveStation(station);
                        Log.Error(ex.ToString());
                    }

                }
            }
        }

        public void SaveStation(Stations station)
        {
            utils.WriteToXmlFile<Stations>(path + "//Stations//" + station.Name + ".xml", station);


        }
        public void ClearStoreOfPlayersSellingOrders(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Order)
                {
                    yeet.Add(item);
                }
            }
            foreach (MyStoreItem item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public void ClearStoreOfPlayersBuyingOffers(MyStoreBlock store)
        {

            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Offer)
                {
                    yeet.Add(item);
                }
            }
            foreach (MyStoreItem item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public List<Stations> stations = new List<Stations>();

        Dictionary<String, List<BuyOrder>> buyOrders = new Dictionary<string, List<BuyOrder>>();
        Dictionary<String, List<SellOffer>> sellOffers = new Dictionary<string, List<SellOffer>>();
        FileUtils utils = new FileUtils();
        public void LoadAllStations()
        {
            stations.Clear();
            foreach (String s in Directory.GetFiles(path + "//Stations//"))
            {


                try
                {
                    Stations stat = utils.ReadFromXmlFile<Stations>(s);
                    if (stat.Enabled)
                    {
                        stations.Add(stat);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                    throw;
                }

            }
        }
        public void LoadAllBuyOrders()
        {
            buyOrders.Clear();
            foreach (String s in Directory.GetDirectories(path + "//BuyOrders//"))
            {
                String temp = new DirectoryInfo(s).Name;

                List<BuyOrder> temporaryList = new List<BuyOrder>();
                foreach (String s2 in Directory.GetFiles(s))
                {
                    try
                    {
                        BuyOrder order = utils.ReadFromXmlFile<BuyOrder>(s2);
                        if (order.Enabled)
                        {
                            temporaryList.Add(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading buy orders " + s2 + " " + ex.ToString());

                    }
                }
                buyOrders.Add(temp, temporaryList);
            }

        }
        public void LoadAllSellOffers()
        {
            sellOffers.Clear();
            foreach (String s in Directory.GetDirectories(path + "//SellOffers//"))
            {
                String temp = new DirectoryInfo(s).Name;
                List<SellOffer> temporaryList = new List<SellOffer>();
                foreach (String s2 in Directory.GetFiles(s))
                {
                    try
                    {
                        SellOffer order = utils.ReadFromXmlFile<SellOffer>(s2);
                        if (order.Enabled)
                        {
                            temporaryList.Add(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading sell offers " + s2 + " " + ex.ToString());

                    }
                }
                sellOffers.Add(temp, temporaryList);
            }

        }

        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            TorchState = state;
            if (state == TorchSessionState.Loaded)
            {
                try
                {
                    LoadAllSellOffers();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Sell offers " + ex.ToString());


                }
                try
                {
                    LoadAllStations();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Stations " + ex.ToString());


                }
                try
                {
                    LoadAllBuyOrders();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Buy Orders " + ex.ToString());


                }
            }
        }
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            if (!Directory.Exists(path + "//Stations//"))
            {
                Directory.CreateDirectory(path + "//Stations//");
                Stations station = new Stations();
                station.Enabled = false;
                utils.WriteToXmlFile<Stations>(path + "//Stations//Example.xml", station);
            }
            if (!Directory.Exists(path + "//BuyOrders//"))
            {
                Directory.CreateDirectory(path + "//BuyOrders//");
            }
            if (!Directory.Exists(path + "//BuyOrders//Example//"))
            {
                Directory.CreateDirectory(path + "//BuyOrders//Example//");
                BuyOrder example = new BuyOrder();
                utils.WriteToXmlFile<BuyOrder>(path + "//BuyOrders//Example//Example.xml", example);

            }
            if (!Directory.Exists(path + "//SellOffers//"))
            {
                Directory.CreateDirectory(path + "//SellOffers//");
            }
            if (!Directory.Exists(path + "//SellOffers//Example//"))
            {
                Directory.CreateDirectory(path + "//SellOffers//Example//");
                SellOffer example = new SellOffer();
                utils.WriteToXmlFile<SellOffer>(path + "//SellOffers//Example//Example.xml", example);
            }
            if (!Directory.Exists(path + "//GridSelling//"))
            {
                Directory.CreateDirectory(path + "//GridSelling//");
            }
            TorchBase = Torch;
        }

        public void SetupConfig()
        {
            FileUtils utils = new FileUtils();
            path = StoragePath;
            if (File.Exists(StoragePath + "\\CrunchEconomy.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder = config.StoragePath;
            }
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder2 = config.StoragePath + "//CrunchEcon//";
            }

            Directory.CreateDirectory(folder2);


            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            FileUtils utils = new FileUtils();

            config = utils.ReadFromXmlFile<Config>(basePath + "\\CrunchEconomy.xml");


            return config;
        }
        public static void saveConfig()
        {
            FileUtils utils = new FileUtils();

            utils.WriteToXmlFile<Config>(basePath + "\\CrunchEconomy.xml", config);

            return;
        }
    }
}
