using System.Collections.Generic;

namespace CrunchEconomy.Storage
{
    public interface IConfigProvider
    {
        string FolderLocation { get; set; }
        Dictionary<string, List<BuyOrder>> _buyOrders { get; set; }
        Dictionary<string, List<SellOffer>> _sellOffers { get; set; }
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