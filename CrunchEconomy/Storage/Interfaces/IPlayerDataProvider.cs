using System;
using System.Collections.Generic;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;

namespace CrunchEconomy.Storage.Interfaces
{
    public interface IPlayerDataProvider
    {
        Dictionary<Guid, Contract> ContractSave { get; set; }
        Dictionary<Guid, SurveyMission> SurveySave { get; set; }
        Dictionary<ulong, PlayerData> PlayerData { get; set; }
        void Setup(Config Config);
        void SavePlayerData(PlayerData Data);
        Dictionary<Guid,Contract> LoadMiningContracts(List<Guid> Ids);
        Dictionary<Guid, Contract> LoadHaulingContracts(List<Guid> Ids);
        PlayerData GetPlayerData(ulong SteamId, bool Login = false);
        PlayerData LoadPlayerData(ulong SteamId);
        void AddContractToBeSaved(Contract Contract, bool Delete = false);
        void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false);
        void SaveContracts();
        SurveyMission LoadMission(Guid SurveyMission);
    }
}