using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using NLog;

namespace CrunchEconomy.Storage
{
    public class XmlStorageProvider : IStorageProvider
    {
        private string FolderLocation { get; set; }
        public FileUtils Utils = new FileUtils();
        public Logger Log = LogManager.GetLogger("CrunchEcon-StorageProvider");

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private List<Stations> _stations = new List<Stations>();
        public Dictionary<string, List<BuyOrder>> _buyOrders { get; set; } = new Dictionary<string, List<BuyOrder>>();
        public Dictionary<string, List<SellOffer>> _sellOffers { get; set; } = new Dictionary<string, List<SellOffer>>();
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private Dictionary<string, GridSale> _gridsForSale = new Dictionary<string, GridSale>();
        private Dictionary<Guid, Contract> _contractSave = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, SurveyMission> _surveySave = new Dictionary<Guid, SurveyMission>();

        public XmlStorageProvider(string StorageFolder)
        {
            FolderLocation = StorageFolder;
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Data//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Mining//Completed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Mining//Failed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Mining//InProgress//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Hauling//Completed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Hauling//Failed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Hauling//InProgress//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Survey//Completed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Survey//Failed//");
            Directory.CreateDirectory(FolderLocation + "//PlayerData//Survey//InProgress//");
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

        public void AddContractToBeSaved(Contract Contract, bool Delete = false)
        {
            if (Delete)
            {
                File.Delete($"{FolderLocation}//PlayerData//Mining//InProgress//{Contract.ContractId}.xml");
            }
            _contractSave.Remove(Contract.ContractId);
            _contractSave.Add(Contract.ContractId, Contract);
        }
        public void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false)
        {
            if (Delete)
            {
                File.Delete($"{FolderLocation}//PlayerData//Survey//InProgress//{Mission.id}.xml");
            }
            _surveySave.Remove(Mission.id);
            _surveySave.Add(Mission.id, Mission);
        }

        public void SaveContracts()
        {
            string type;
            foreach (var keys in _contractSave)
            {
                var contract = keys.Value;
                switch (contract.type)
                {
                    case ContractType.Mining:
                        type = "//Mining";
                        break;
                    case ContractType.Hauling:
                        type = "//Hauling";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (contract.status)
                {
                    case ContractStatus.InProgress:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//InProgress//{contract.ContractId}.xml", keys.Value);
                        break;
                    case ContractStatus.Completed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Completed//{contract.ContractId}.xml", keys.Value);
                        break;
                    case ContractStatus.Failed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Failed//{contract.ContractId}.xml", keys.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            _contractSave.Clear();

            foreach (var keys in _surveySave)
            {
                var mission = keys.Value;
                type = "//Survey";

                switch (mission.status)
                {
                    case ContractStatus.InProgress:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//InProgress//{mission.id}.xml", keys.Value);
                        break;
                    case ContractStatus.Completed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Completed//{mission.id}.xml", keys.Value);
                        break;
                    case ContractStatus.Failed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Failed//{mission.id}.xml", keys.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            _surveySave.Clear();
        }

    }
}
