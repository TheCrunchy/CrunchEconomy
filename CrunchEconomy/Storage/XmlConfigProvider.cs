using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Helpers;
using CrunchEconomy.Station_Stuff;
using CrunchEconomy.Station_Stuff.Objects;
using CrunchEconomy.Storage.Interfaces;
using CrunchEconomy.SurveyMissions;
using NLog;
using NLog.Fluent;
using Sandbox.Engine.Multiplayer;

namespace CrunchEconomy.Storage
{
    public class XmlConfigProvider : IConfigProvider
    {
        public Logger Log = LogManager.GetLogger("CrunchEcon-ConfigProvider");
        public RepConfig RepConfig { get; set; }
        public WhitelistFile Whitelist { get; set; }
        public FileUtils Utils = new FileUtils();
        public string FolderLocation { get; set; }
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        public List<Stations> Stations { get; set; }= new List<Stations>();
        public Dictionary<string, List<BuyOrder>> BuyOrders { get; set; } = new Dictionary<string, List<BuyOrder>>();
        public Dictionary<string, List<SellOffer>> SellOffers { get; set; } = new Dictionary<string, List<SellOffer>>();
        public Dictionary<string, GridSale> GridsForSale { get; set; } = new Dictionary<string, GridSale>();

        public void LoadRepConfig()
        {
            if (File.Exists(FolderLocation + "\\ReputationConfig.xml"))
            {
                RepConfig = Utils.ReadFromXmlFile<RepConfig>(FolderLocation + "\\ReputationConfig.xml");
            Utils.WriteToXmlFile<RepConfig>(FolderLocation + "\\ReputationConfig.xml", RepConfig, false);
            }
            else
            {
                RepConfig = new RepConfig();
                var item = new RepConfig.RepItem();

                RepConfig.RepConfigs.Add(item);
                Utils.WriteToXmlFile<RepConfig>(FolderLocation + "\\ReputationConfig.xml", RepConfig, false);
            }
        }

        public void LoadWhitelist()
        {
            if (File.Exists(FolderLocation + "\\Whitelist.xml"))
            {
                Whitelist = Utils.ReadFromXmlFile<WhitelistFile>(FolderLocation + "\\Whitelist.xml");
                Utils.WriteToXmlFile<WhitelistFile>(FolderLocation + "\\Whitelist.xml", Whitelist, false);
            }
            else
            {
                Whitelist = new WhitelistFile();
                var temp = new WhitelistFile.Whitelist();
                temp.FactionTags.Add("BOB");
                temp.ListName = "LIST1";

                Whitelist.values.Add(temp);
                var temp2 = new WhitelistFile.Whitelist();
                temp2.FactionTags.Add("CAR");
                temp2.FactionTags.Add("BOB");
                temp2.ListName = "LIST2";
                Whitelist.values.Add(temp2);
                Utils.WriteToXmlFile<WhitelistFile>(FolderLocation + "\\Whitelist.xml", Whitelist, false);
            }
        }

        public XmlConfigProvider(string FolderLocation)
        {
            this.FolderLocation = FolderLocation;

            LoadWhitelist();
            LoadRepConfig();

            if (!Directory.Exists(FolderLocation + "//Stations//"))
            {
                Directory.CreateDirectory(FolderLocation + "//Stations//");
                var station = new Stations
                {
                    Enabled = false
                };
                var modifier = new Stations.PriceModifier();
                station.Modifiers.Add(modifier);
                station.Whitelist.Add("FAC:BOB");
                station.Whitelist.Add("LIST:LIST1");
                var item = new Stations.CraftedItem
                {
                    Typeid = "Ore",
                    Subtypeid = "Iron",
                    AmountPerCraft = 500,
                    ChanceToCraft = 1
                };

                var recipe = new Stations.RecipeItem
                {
                    Typeid = "Ore",
                    Subtypeid = "Stone",
                    Amount = 500
                };

                item.RequriedItems.Add(recipe);
                station.CraftableItems.Add(item);
                Utils.WriteToXmlFile<Stations>(FolderLocation + "//Stations//Example.xml", station);
            }

            if (!Directory.Exists(FolderLocation + "//BuyOrders//Example//"))
            {
                Directory.CreateDirectory(FolderLocation + "//BuyOrders//Example//");
                var example = new BuyOrder();
                Utils.WriteToXmlFile<BuyOrder>(FolderLocation + "//BuyOrders//Example//Example.xml", example);
            }

            if (!Directory.Exists(FolderLocation + "//SellOffers//Example//"))
            {
                Directory.CreateDirectory(FolderLocation + "//SellOffers//Example//");
                var example = new SellOffer();
                var gps = "put a gps string here";
                example.GpsToPickFrom.Add(gps);
                Utils.WriteToXmlFile<SellOffer>(FolderLocation + "//SellOffers//Example//Example.xml", example);
            }

            if (!Directory.Exists(FolderLocation + "//GridSelling//"))
            {
                var gridSale = new GridSale();

                Directory.CreateDirectory(FolderLocation + "//GridSelling//");
                Utils.WriteToXmlFile<GridSale>(FolderLocation + "//GridSelling//ExampleSale.xml", gridSale);
            }

            if (!Directory.Exists(FolderLocation + "//GridSelling//Grids//"))
            {
                Directory.CreateDirectory(FolderLocation + "//GridSelling//Grids//");
            }

            if (!Directory.Exists(FolderLocation + "//ContractConfigs//Survey//"))
            {
                var mission = new SurveyMission();
                mission.Configs.Add(new SurveyStage());
                Directory.CreateDirectory(FolderLocation + "//ContractConfigs//Survey//");
                Utils.WriteToXmlFile<SurveyMission>(FolderLocation + "//ContractConfigs//Survey//Example1.xml", mission);
                mission.Configs.Add(new SurveyStage());
                Utils.WriteToXmlFile<SurveyMission>(FolderLocation + "//ContractConfigs//Survey//Example2.xml", mission);
                mission.Configs.Add(new SurveyStage());
                Utils.WriteToXmlFile<SurveyMission>(FolderLocation + "//ContractConfigs//Survey//Example3.xml", mission);
            }

            if (!Directory.Exists(FolderLocation + "//ContractConfigs//Mining//"))
            {
                var contract = new GeneratedContract();

                Directory.CreateDirectory(FolderLocation + "//ContractConfigs//Mining//");
                contract.PlayerLoot.Add(new RewardItem());
                contract.PutInStation.Add(new RewardItem());
                contract.ItemsToPickFrom.Add(new GeneratedContract.ContractInfo());
                contract.ItemsToPickFrom.Add(new GeneratedContract.ContractInfo());
                contract.StationsToDeliverTo.Add(new GeneratedContract.StationDelivery());
                Utils.WriteToXmlFile<GeneratedContract>(FolderLocation + "//ContractConfigs//Mining//Example.xml", contract);
            }

            if (!Directory.Exists(FolderLocation + "//ContractConfigs//Hauling//"))
            {
                var contract = new GeneratedContract();
                Directory.CreateDirectory(FolderLocation + "//ContractConfigs//Hauling//");
                contract.Type = ContractType.Hauling;
                contract.PlayerLoot.Add(new RewardItem());
                contract.PutInStation.Add(new RewardItem());
                contract.ItemsToPickFrom.Add(new GeneratedContract.ContractInfo());
                contract.ItemsToPickFrom.Add(new GeneratedContract.ContractInfo());
                contract.StationsToDeliverTo.Add(new GeneratedContract.StationDelivery());
                Utils.WriteToXmlFile<GeneratedContract>(FolderLocation + "//ContractConfigs//Hauling//Example.xml", contract);
            }
        }

        public List<Stations> GetStations()
        {
            return Stations;
        }
        public Dictionary<string, GridSale> GetGridsForSale()
        {
            return GridsForSale;
        }
        public Dictionary<string, List<BuyOrder>> GetBuyOrders()
        {
            return BuyOrders;
        }
        public Dictionary<string, List<SellOffer>> GetSellOffers()
        {
            return SellOffers;
        }

        public void SaveStation(Stations Station)
        {
            Utils.WriteToXmlFile<Stations>(FolderLocation + "//Stations//" + Station.Name + ".xml", Station);
        }

        public void LoadStations()
        {
            Stations.Clear();
            foreach (var s in Directory.GetFiles(FolderLocation + "//Stations//"))
            {
                try
                {
                    var stat = Utils.ReadFromXmlFile<Stations>(s);
                    if (!stat.Enabled)
                        continue;

                    if (!stat.WorldName.Equals("default") && !stat.WorldName.Equals(MyMultiplayer.Static.HostName))
                        continue;

                    stat.SetupModifiers();
                    Stations.Add(stat);
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                }
            }
        }

        public void LoadAllBuyOrders()
        {
            BuyOrders.Clear();
            foreach (var s in Directory.GetFiles(FolderLocation + "//BuyOrders//", "*", SearchOption.AllDirectories))
            {
                var temp = new DirectoryInfo(s).Name;
                var temporaryList = new List<BuyOrder>();
                if (BuyOrders.ContainsKey(temp))
                {
                    temporaryList = BuyOrders[temp];
                }
                try
                {
                    var order = Utils.ReadFromXmlFile<BuyOrder>(s);
                    if (!order.Enabled) continue;
                    if (order.IndividualRefreshTimer)
                    {
                        order.Path = s;
                    }
                    temporaryList.Add(order);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading buy orders {s} {ex.ToString()}");

                }
                BuyOrders.Remove(temp);
                BuyOrders.Add(temp, temporaryList);
            }
        }

        public void LoadAllSellOffers()
        {
            SellOffers.Clear();
            foreach (var s in Directory.GetFiles(FolderLocation + "//SellOffers//", "*", SearchOption.AllDirectories))
            {
                var temp = new DirectoryInfo(s).Name;
                var temporaryList = new List<SellOffer>();
                if (SellOffers.ContainsKey(temp))
                {
                    temporaryList = SellOffers[temp];
                }
                try
                {
                    var order = Utils.ReadFromXmlFile<SellOffer>(s);
                    if (!order.Enabled) continue;
                    if (order.IndividualRefreshTimer)
                    {
                        order.Path = s;
                    }
                    temporaryList.Add(order);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading sell offers {s} {ex.ToString()}");

                }
                SellOffers.Remove(temp);
                SellOffers.Add(temp, temporaryList);
            }
        }

        public void LoadAllGridSales()
        {
            GridsForSale.Clear();

            foreach (var s2 in Directory.GetFiles($"{FolderLocation}//GridSelling//"))
            {
                try
                {
                    var sale = Utils.ReadFromXmlFile<GridSale>(s2);
                    if (sale.Enabled && !GridsForSale.ContainsKey(sale.ItemSubTypeId))
                    {
                        GridsForSale.Add(sale.ItemSubTypeId, sale);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading grid sales offers {s2} {ex.ToString()}");
                }
            }
        }
    }
}
