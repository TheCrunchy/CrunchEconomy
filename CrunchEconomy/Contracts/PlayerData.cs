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
        public ulong SteamId;
        public Boolean HasHadSurveyBefore = false;
        public int MiningReputation = 0;
        public int HaulingReputation = 0;
        public int SurveyReputation = 0;

        private Dictionary<Guid, Contract> _loadedMining = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, Contract> _loadedHauling = new Dictionary<Guid, Contract>();
        public Guid SurveyMission = Guid.Empty;
        public DateTime NextSurveyMission = DateTime.Now.AddSeconds(10);
        private SurveyMission _loadedMission = null;
        public void SetLoadedSurvey(SurveyMission Mission)
        {
            _loadedMission = Mission;
        }
        public SurveyMission GetLoadedMission()
        {
            return _loadedMission;
        }
        public SurveyMission GetMission()
        {
            if (SurveyMission == Guid.Empty)
            {
                return null;
            }

            var mission = CrunchEconCore.PlayerStorageProvider.LoadMission(SurveyMission);
            _loadedMission = mission;
            return mission;
        }

        public void AddMining(Contract Contract)
        {
            //test
            if (_loadedMining.ContainsKey(Contract.ContractId)) return;
            _loadedMining.Add(Contract.ContractId, Contract);
            MiningContracts.Add(Contract.ContractId);
        }
        public void AddHauling(Contract Contract)
        {
            if (_loadedHauling.ContainsKey(Contract.ContractId)) return;
            _loadedHauling.Add(Contract.ContractId, Contract);
            HaulingContracts.Add(Contract.ContractId);
        }

        public Dictionary<Guid, Contract> GetMiningContracts()
        {
            if (_loadedMining != null && _loadedMining.Count > 0)
            {
                return _loadedMining;
            }


            _loadedMining = CrunchEconCore.PlayerStorageProvider.LoadMiningContracts(MiningContracts);
            return _loadedMining;
        }

        public Dictionary<Guid, Contract> GetHaulingContracts()
        {
            if (_loadedHauling != null && _loadedHauling.Count > 0)
            {
                return _loadedHauling;
            }
            _loadedHauling = CrunchEconCore.PlayerStorageProvider.LoadHaulingContracts(HaulingContracts);
            return _loadedHauling;
        }
    }
}
