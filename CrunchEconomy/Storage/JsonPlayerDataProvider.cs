using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Storage.Interfaces;
using CrunchEconomy.SurveyMissions;
using NLog;

namespace CrunchEconomy.Storage
{
    public class JsonPlayerDataProvider : IPlayerDataProvider
    {
        public string FolderLocation { get; set; }
        public FileUtils Utils { get; set; } = new FileUtils();
        public Logger Log = LogManager.GetLogger("CrunchEcon-StorageProvider");

        public Dictionary<Guid, Contract> _contractSave { get; set; } = new Dictionary<Guid, Contract>();
        public Dictionary<Guid, SurveyMission> _surveySave { get; set; } = new Dictionary<Guid, SurveyMission>();
        public Dictionary<ulong, PlayerData> playerData { get; set; } = new Dictionary<ulong, PlayerData>();

        public void Setup(Config Config)
        {
            FolderLocation = CrunchEconCore.path;
        }

        public void SavePlayerData(PlayerData Data)
        {
            
            Utils.WriteToJsonFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{Data.steamId}.json", Data);
        }

        public SurveyMission LoadMission(Guid SurveyMission)
        {
            var path = $"{CrunchEconCore.path}//PlayerData//Survey//InProgress//{SurveyMission}.json";
            if (!File.Exists(path)) return null;
            var mission = CrunchEconCore.utils.ReadFromJsonFile<SurveyMission>(path);
            if (mission == null) return null;
            mission.SetupMissionList();
            return mission;
        }

        public Dictionary<Guid, Contract> LoadMiningContracts(List<Guid> Ids)
        {
            var temporary = new Dictionary<Guid, Contract>();
            foreach (var id in Ids)
            {
                var path = $"{FolderLocation}//PlayerData//Mining//InProgress//{id}.json";
                if (!File.Exists(path)) continue;
                if (temporary.ContainsKey(id)) continue;
                var contract = CrunchEconCore.utils.ReadFromJsonFile<Contract>(path);
                temporary.Add(id, contract);
            }

            return temporary;
        }
        public Dictionary<Guid, Contract> LoadHaulingContracts(List<Guid> Ids)
        {
            var temporary = new Dictionary<Guid, Contract>();
            foreach (var id in Ids)
            {
                var path = $"{FolderLocation}//PlayerData//Hauling//InProgress//{id}.json";
                if (!File.Exists(path)) continue;
                if (temporary.ContainsKey(id)) continue;
                var contract = CrunchEconCore.utils.ReadFromJsonFile<Contract>(path);
                temporary.Add(id, contract);
            }

            return temporary;
        }

        public PlayerData GetPlayerData(ulong SteamId, bool Login = false)
        {
            if (!Login)
            {
                return LoadPlayerData(SteamId);
            }
            return playerData.TryGetValue(SteamId, out var data) ? data : LoadPlayerData(SteamId);
        }

        public PlayerData LoadPlayerData(ulong SteamId)
        {
            var data = Utils.ReadFromJsonFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.json");
            playerData.Remove(SteamId);
            if (data != null)
            {
                playerData.Add(SteamId, data);
                return data;
            }

            File.Delete($"{FolderLocation}//PlayerData//Data//{SteamId}.json");
            if (playerData.TryGetValue(SteamId, out var previousData))
            {
                Utils.WriteToJsonFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.json", previousData);
                playerData.Add(SteamId, previousData);
                Log.Error($"Corrupt Player Data, if they had a previous save before login, that has been restored. {SteamId}");
                return previousData;
            }

            var temp = new PlayerData()
            {
                steamId = SteamId
            };
            playerData.Add(SteamId, temp);
            return temp;
        }

        public JsonPlayerDataProvider(string StorageFolder)
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

        public void AddContractToBeSaved(Contract Contract, bool Delete = false)
        {
            if (Delete)
            {
                File.Delete($"{FolderLocation}//PlayerData//Mining//InProgress//{Contract.ContractId}.json");
            }
            _contractSave.Remove(Contract.ContractId);
            _contractSave.Add(Contract.ContractId, Contract);
        }
        public void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false)
        {
            if (Delete)
            {
                File.Delete($"{FolderLocation}//PlayerData//Survey//InProgress//{Mission.id}.json");
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
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//InProgress//{contract.ContractId}.json", keys.Value);
                        break;
                    case ContractStatus.Completed:
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//Completed//{contract.ContractId}.json", keys.Value);
                        break;
                    case ContractStatus.Failed:
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//Failed//{contract.ContractId}.json", keys.Value);
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
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//InProgress//{mission.id}.json", keys.Value);
                        break;
                    case ContractStatus.Completed:
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//Completed//{mission.id}.json", keys.Value);
                        break;
                    case ContractStatus.Failed:
                        Utils.WriteToJsonFile($"{FolderLocation}//PlayerData//{type}//Failed//{mission.id}.json", keys.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            _surveySave.Clear();
        }

    }
}
