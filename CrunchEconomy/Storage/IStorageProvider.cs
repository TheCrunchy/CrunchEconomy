using System.Collections.Generic;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;

namespace CrunchEconomy.Storage
{
    public interface IStorageProvider
    {
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
        void AddContractToBeSaved(Contract Contract, bool Delete = false);
        void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false);
        void SaveContracts();
    }
}