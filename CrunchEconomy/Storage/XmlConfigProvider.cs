using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using Sandbox.Engine.Multiplayer;

namespace CrunchEconomy.Storage
{
    public class XmlConfigProvider : IConfigProvider
    {
        public Logger Log = LogManager.GetLogger("CrunchEcon-ConfigProvider");
        public FileUtils Utils = new FileUtils();
        public string FolderLocation { get; set; }
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private List<Stations> _stations = new List<Stations>();
        public Dictionary<string, List<BuyOrder>> _buyOrders { get; set; } = new Dictionary<string, List<BuyOrder>>();
        public Dictionary<string, List<SellOffer>> _sellOffers { get; set; } = new Dictionary<string, List<SellOffer>>();
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private Dictionary<string, GridSale> _gridsForSale = new Dictionary<string, GridSale>();

        public XmlConfigProvider(string FolderLocation)
        {
            this.FolderLocation = FolderLocation;
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
