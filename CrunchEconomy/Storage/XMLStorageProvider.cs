using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using NLog;

namespace CrunchEconomy.Storage
{
    public class XmlStorageProvider : IStorageProvider
    {
        public string FolderLocation { get; set; }
        public FileUtils Utils = new FileUtils();
        public Logger Log = LogManager.GetLogger("CrunchEcon-StorageProvider");

        private Dictionary<Guid, Contract> _contractSave = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, SurveyMission> _surveySave = new Dictionary<Guid, SurveyMission>();

        public void SavePlayerData(PlayerData data)
        {
            Utils.WriteToXmlFile<PlayerData>($"{FolderLocation}//PlayerData//Data//{data.steamId}.xml", data);
        }

        public XmlStorageProvider(string StorageFolder)
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
