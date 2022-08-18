using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;

namespace CrunchEconomy.Storage
{
    public interface IPlayerDataProvider
    {
        string FolderLocation { get; set; }
        void SavePlayerData(PlayerData data);
        void AddContractToBeSaved(Contract Contract, bool Delete = false);
        void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false);
        void SaveContracts();
    }
}