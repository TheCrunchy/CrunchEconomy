using System.Collections.Generic;
using CrunchEconomy.Station_Stuff;
using CrunchEconomy.Station_Stuff.Objects;

namespace CrunchEconomy.Storage.Interfaces
{
    public interface IConfigProvider
    {
        RepConfig RepConfig { get; set; }
        WhitelistFile Whitelist { get; set; }
        string FolderLocation { get; set; }
        List<Stations> Stations { get; set; }
        Dictionary<string, List<BuyOrder>> BuyOrders { get; set; }
        Dictionary<string, List<SellOffer>> SellOffers { get; set; }
        Dictionary<string, GridSale> GridsForSale { get; set; }
        void LoadRepConfig();
        void LoadWhitelist();
        List<Stations> GetStations();
        Dictionary<string, GridSale> GetGridsForSale();
        Dictionary<string, List<BuyOrder>> GetBuyOrders();
        Dictionary<string, List<SellOffer>> GetSellOffers();
        void SaveStation(Stations station);
        void LoadStations();
        void LoadAllBuyOrders();
        void LoadAllSellOffers();
        void LoadAllGridSales();
    }
}