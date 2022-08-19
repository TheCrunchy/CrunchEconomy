using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using NLog;

namespace CrunchEconomy.Storage
{
    public class XmlPlayerDataProvider : IPlayerDataProvider
    {
        public string FolderLocation { get; set; }
        public FileUtils Utils = new FileUtils();
        public Logger Log = LogManager.GetLogger("CrunchEcon-StorageProvider");

        private Dictionary<Guid, Contract> _contractSave = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, SurveyMission> _surveySave = new Dictionary<Guid, SurveyMission>();
        private Dictionary<ulong, PlayerData> playerData = new Dictionary<ulong, PlayerData>();
        public void SavePlayerData(PlayerData Data)
        {
            Utils.WriteToXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{Data.steamId}.xml", Data);
        }

        public PlayerData GetPlayerData(ulong SteamId, bool login = false)
        {
            if (!login)
            {
                return LoadPlayerData(SteamId);
            }
            return playerData.TryGetValue(SteamId, out var data) ? data : LoadPlayerData(SteamId);
        }

        public PlayerData LoadPlayerData(ulong SteamId)
        {
            var data = Utils.ReadFromXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.xml");
            playerData.Add(SteamId, data);
            if (data != null) return data;

            File.Delete($"{FolderLocation}//PlayerData//Data//{SteamId}.xml");
            if (!playerData.TryGetValue(SteamId, out var previousData)) return new PlayerData()
            {
                steamId = SteamId
            };

            Utils.WriteToXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{SteamId}.xml", previousData);
            playerData.Add(SteamId, previousData);
            Log.Error($"Corrupt Player Data, if they had a previous save before login, that has been restored. {SteamId}");
            return previousData;
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
