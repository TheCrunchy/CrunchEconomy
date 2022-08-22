using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Helpers;
using CrunchEconomy.Storage.Interfaces;
using CrunchEconomy.SurveyMissions;
using NLog;

namespace CrunchEconomy.Storage
{
    public class XmlPlayerDataProvider : IPlayerDataProvider
    {
        public string FolderLocation { get; set; }
        public FileUtils Utils { get; set; } = new FileUtils();
        public Logger Log = LogManager.GetLogger("CrunchEcon-StorageProvider");

        public Dictionary<Guid, Contract> ContractSave { get; set; } = new Dictionary<Guid, Contract>();
        public Dictionary<Guid, SurveyMission> SurveySave { get; set; } = new Dictionary<Guid, SurveyMission>();
        public Dictionary<ulong, PlayerData> PlayerData { get; set; } = new Dictionary<ulong, PlayerData>();

        public void Setup(Config Config)
        {
            FolderLocation = CrunchEconCore.Path;
        }

        public void SavePlayerData(PlayerData Data)
        {
            Utils.WriteToXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{Data.SteamId}.xml", Data);
        }

        public SurveyMission LoadMission(Guid SurveyMission)
        {
            var path = $"{CrunchEconCore.Path}//PlayerData//Survey//InProgress//{SurveyMission}.xml";
            if (!File.Exists(path)) return null;
            var mission = CrunchEconCore.Utils.ReadFromXmlFile<SurveyMission>(path);
            if (mission == null) return null;
            mission.SetupMissionList();
            return mission;

        }

        public Dictionary<Guid, Contract> LoadMiningContracts(List<Guid> Ids)
        {
            var temporary = new Dictionary<Guid, Contract>();
            foreach (var id in Ids)
            {
                var path = $"{FolderLocation}//PlayerData//Mining//InProgress//{id}.xml";
                if (!File.Exists(path)) continue;
                if (temporary.ContainsKey(id)) continue;
                var contract = CrunchEconCore.Utils.ReadFromXmlFile<Contract>(path);
                temporary.Add(id, contract);
            }

            return temporary;
        }
        public Dictionary<Guid, Contract> LoadHaulingContracts(List<Guid> Ids)
        {
            var temporary = new Dictionary<Guid, Contract>();
            foreach (var id in Ids)
            {
                var path = $"{FolderLocation}//PlayerData//Hauling//InProgress//{id}.xml";
                if (!File.Exists(path)) continue;
                if (temporary.ContainsKey(id)) continue;
                var contract = CrunchEconCore.Utils.ReadFromXmlFile<Contract>(path);
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
            return PlayerData.TryGetValue(SteamId, out var data) ? data : LoadPlayerData(SteamId);
        }

        public PlayerData LoadPlayerData(ulong SteamId)
        {
            var data = Utils.ReadFromXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.xml");
            PlayerData.Remove(SteamId);
            if (data != null)
            {
                PlayerData.Add(SteamId, data);
                return data;
            }

            File.Delete($"{FolderLocation}//PlayerData//Data//{SteamId}.xml");
            if (PlayerData.TryGetValue(SteamId, out var previousData))
            {
                Utils.WriteToXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.xml", previousData);
                PlayerData.Add(SteamId, previousData);
                Log.Error($"Corrupt Player Data, if they had a previous save before login, that has been restored. {SteamId}");
                return previousData;
            }

            var temp = new PlayerData()
            {
                SteamId = SteamId
            };
            PlayerData.Add(SteamId, temp);
            return temp;
        }

        public XmlPlayerDataProvider(string StorageFolder)
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
                File.Delete($"{FolderLocation}//PlayerData//Mining//InProgress//{Contract.ContractId}.xml");
            }
            ContractSave.Remove(Contract.ContractId);
            ContractSave.Add(Contract.ContractId, Contract);
        }
        public void AddSurveyToBeSaved(SurveyMission Mission, bool Delete = false)
        {
            if (Delete)
            {
                File.Delete($"{FolderLocation}//PlayerData//Survey//InProgress//{Mission.Id}.xml");
            }
            SurveySave.Remove(Mission.Id);
            SurveySave.Add(Mission.Id, Mission);
        }

        public void SaveContracts()
        {
            string type;
            foreach (var keys in ContractSave)
            {
                var contract = keys.Value;
                switch (contract.Type)
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

                switch (contract.Status)
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
            ContractSave.Clear();

            foreach (var keys in SurveySave)
            {
                var mission = keys.Value;
                type = "//Survey";

                switch (mission.Status)
                {
                    case ContractStatus.InProgress:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//InProgress//{mission.Id}.xml", keys.Value);
                        break;
                    case ContractStatus.Completed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Completed//{mission.Id}.xml", keys.Value);
                        break;
                    case ContractStatus.Failed:
                        Utils.WriteToXmlFile($"{FolderLocation}//PlayerData//{type}//Failed//{mission.Id}.xml", keys.Value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            SurveySave.Clear();
        }

    }
}
