using System;
using System.Collections.Generic;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;

namespace CrunchEconomy.Storage.Interfaces
{
    public interface IPlayerDataProvider
    {
        Dictionary<Guid, Contract> _contractSave { get; set; }
        Dictionary<Guid, SurveyMission> _surveySave { get; set; }
        Dictionary<ulong, PlayerData> playerData { get; set; }
        void Setup(Config Config);
        void SavePlayerData(PlayerData Data);
        Dictionary<Guid,Contract> LoadMiningContracts(List<Guid> Ids);
        Dictionary<Guid, Contract> LoadHaulingContracts(List<Guid> Ids);
        PlayerData GetPlayerData(ulong SteamId, bool Login = false);
        PlayerData LoadPlayerData(ulong SteamId);
        void AddContractToBeSaved(Contract Contract, bool Delete = false);
        void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false);
        void SaveContracts();
        SurveyMission LoadMission(Guid surveyMission);
    }
}