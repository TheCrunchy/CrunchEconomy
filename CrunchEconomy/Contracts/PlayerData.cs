using CrunchEconomy.SurveyMissions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.Contracts
{
    public class PlayerData
    {
        public List<Guid> MiningContracts = new List<Guid>();
        public List<Guid> HaulingContracts = new List<Guid>();
        public ulong steamId;
        public Boolean HasHadSurveyBefore = false;
        public int MiningReputation = 0;
        public int HaulingReputation = 0;
        public int SurveyReputation = 0;

        private Dictionary<Guid, Contract> loadedMining = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, Contract> loadedHauling = new Dictionary<Guid, Contract>();
        public Guid surveyMission = Guid.Empty;
        public DateTime NextSurveyMission = DateTime.Now.AddSeconds(10);
        private SurveyMission loadedMission = null;
        public void SetLoadedSurvey(SurveyMission mission)
        {
            loadedMission = mission;
        }
        public SurveyMission GetLoadedMission()
        {
            return loadedMission;
        }
        public SurveyMission getMission()
        {
            if (surveyMission == Guid.Empty)
            {
                return null;
            }

            if (File.Exists(CrunchEconCore.path + "//PlayerData//Survey//InProgress//" + surveyMission.ToString() + ".xml"))
            {
             
                    SurveyMission mission = CrunchEconCore.utils.ReadFromXmlFile<SurveyMission>(CrunchEconCore.path + "//PlayerData//Survey//InProgress//" + surveyMission.ToString() + ".xml");
                mission.SetupMissionList();
                loadedMission = mission;
            }

            return loadedMission;
        }

        public void addMining(Contract contract)
        {
            //test
            if (loadedMining.ContainsKey(contract.ContractId)) return;
            loadedMining.Add(contract.ContractId, contract);
            MiningContracts.Add(contract.ContractId);
        }
        public void addHauling(Contract contract)
        {
            if (loadedHauling.ContainsKey(contract.ContractId)) return;
            loadedHauling.Add(contract.ContractId, contract);
            HaulingContracts.Add(contract.ContractId);
        }

        public Dictionary<Guid, Contract> GetMiningContracts()
        {
            if (loadedMining != null && loadedMining.Count > 0)
            {
                return loadedMining;
            }


            loadedMining = CrunchEconCore.PlayerStorageProvider.LoadMiningContracts(MiningContracts);
            return loadedMining;
        }

        public Dictionary<Guid, Contract> GetHaulingContracts()
        {
            if (loadedHauling != null && loadedHauling.Count > 0)
            {
                return loadedHauling;
            }
            loadedHauling = CrunchEconCore.PlayerStorageProvider.LoadHaulingContracts(HaulingContracts);
            return loadedHauling;
        }
    }
}
