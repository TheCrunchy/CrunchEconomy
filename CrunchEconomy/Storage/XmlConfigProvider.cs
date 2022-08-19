﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
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
        private List<Stations> _stations = new List<Stations>();
        public Dictionary<string, List<BuyOrder>> _buyOrders { get; set; } = new Dictionary<string, List<BuyOrder>>();
        public Dictionary<string, List<SellOffer>> _sellOffers { get; set; } = new Dictionary<string, List<SellOffer>>();
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private Dictionary<string, GridSale> _gridsForSale = new Dictionary<string, GridSale>();

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

                Whitelist.whitelist.Add(temp);
                var temp2 = new WhitelistFile.Whitelist();
                temp2.FactionTags.Add("CAR");
                temp2.FactionTags.Add("BOB");
                temp2.ListName = "LIST2";
                Whitelist.whitelist.Add(temp2);
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
                    typeid = "Ore",
                    subtypeid = "Iron",
                    amountPerCraft = 500,
                    chanceToCraft = 1
                };

                var recipe = new Stations.RecipeItem
                {
                    typeid = "Ore",
                    subtypeid = "Stone",
                    amount = 500
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
                example.gpsToPickFrom.Add(gps);
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
                mission.configs.Add(new SurveyStage());
                Directory.CreateDirectory(FolderLocation + "//ContractConfigs//Survey//");
                Utils.WriteToXmlFile<SurveyMission>(FolderLocation + "//ContractConfigs//Survey//Example1.xml", mission);
                mission.configs.Add(new SurveyStage());
                Utils.WriteToXmlFile<SurveyMission>(FolderLocation + "//ContractConfigs//Survey//Example2.xml", mission);
                mission.configs.Add(new SurveyStage());
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
                contract.type = ContractType.Hauling;
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
            return _stations;
        }
        public Dictionary<string, GridSale> GetGridsForSale()
        {
            return _gridsForSale;
        }
        public Dictionary<string, List<BuyOrder>> GetBuyOrders()
        {
            return _buyOrders;
        }
        public Dictionary<string, List<SellOffer>> GetSellOffers()
        {
            return _sellOffers;
        }

        public void SaveStation(Stations station)
        {
            Utils.WriteToXmlFile<Stations>(FolderLocation + "//Stations//" + station.Name + ".xml", station);
        }

        public void LoadStations()
        {
            _stations.Clear();
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
                    _stations.Add(stat);
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                }
            }
        }

        public void LoadAllBuyOrders()
        {
            _buyOrders.Clear();
            foreach (var s in Directory.GetDirectories(FolderLocation + "//BuyOrders//", "*", SearchOption.AllDirectories))
            {
                var temp = new DirectoryInfo(s).Name;
                var temporaryList = new List<BuyOrder>();
                if (_buyOrders.ContainsKey(temp))
                {
                    temporaryList = _buyOrders[temp];
                }
                try
                {
                    var order = Utils.ReadFromXmlFile<BuyOrder>(s);
                    if (!order.Enabled) continue;
                    if (order.IndividualRefreshTimer)
                    {
                        order.path = s;
                    }
                    temporaryList.Add(order);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading buy orders {s} {ex.ToString()}");

                }
                _buyOrders.Remove(temp);
                _buyOrders.Add(temp, temporaryList);
            }
        }

        public void LoadAllSellOffers()
        {
            _sellOffers.Clear();
            foreach (var s in Directory.GetDirectories(FolderLocation + "//SellOffers//", "*", SearchOption.AllDirectories))
            {
                var temp = new DirectoryInfo(s).Name;
                var temporaryList = new List<SellOffer>();
                if (_sellOffers.ContainsKey(temp))
                {
                    temporaryList = _sellOffers[temp];
                }
                try
                {
                    var order = Utils.ReadFromXmlFile<SellOffer>(s);
                    if (!order.Enabled) continue;
                    if (order.IndividualRefreshTimer)
                    {
                        order.path = s;
                    }
                    temporaryList.Add(order);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error loading sell offers {s} {ex.ToString()}");

                }
                _sellOffers.Remove(temp);
                _sellOffers.Add(temp, temporaryList);
            }
        }

        public void LoadAllGridSales()
        {
            _gridsForSale.Clear();

            foreach (var s2 in Directory.GetFiles($"{FolderLocation}//GridSelling//"))
            {
                try
                {
                    var sale = Utils.ReadFromXmlFile<GridSale>(s2);
                    if (sale.Enabled && !_gridsForSale.ContainsKey(sale.ItemSubTypeId))
                    {
                        _gridsForSale.Add(sale.ItemSubTypeId, sale);
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
