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
        public int MiningReputation = 0;
        public int HaulingReputation = 0;

        private Dictionary<Guid, Contract> loadedMining = new Dictionary<Guid, Contract>();
        private Dictionary<Guid, Contract> loadedHauling = new Dictionary<Guid, Contract>();

        public void addMining(Contract contract)
        {
            if (!loadedMining.ContainsKey(contract.ContractId))
            {
                loadedMining.Add(contract.ContractId, contract);
                MiningContracts.Add(contract.ContractId);
            }
        }
        public void addHauling(Contract contract)
        {
            if (!loadedHauling.ContainsKey(contract.ContractId))
            {
                loadedHauling.Add(contract.ContractId, contract);
                HaulingContracts.Add(contract.ContractId);
            }
        }
        public Dictionary<Guid, Contract> getMiningContracts()
        {
            if (loadedMining.Count > 0)
            {
                return loadedMining;
            }
            Dictionary<Guid, Contract> temporary = new Dictionary<Guid, Contract>();
            foreach (Guid id in MiningContracts)
            {
                if (File.Exists(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + id.ToString() + ".xml"))
                {
                    if (!temporary.ContainsKey(id))
                    {
                       Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + id.ToString() + ".xml");
                        temporary.Add(id, contract);
                    }
                }
            }

            loadedMining = temporary;
            return temporary;
          
        }
        public Dictionary<Guid, Contract> getHaulingContracts()
        {
            if (loadedHauling.Count > 0)
            {
                return loadedHauling;
            }
            Dictionary<Guid, Contract> temporary = new Dictionary<Guid, Contract>();
            foreach (Guid id in HaulingContracts)
            {
                if (File.Exists(CrunchEconCore.path + "//PlayerData//Hauling//InProgress//" + id.ToString() + ".xml"))
                {
                    if (!temporary.ContainsKey(id))
                    {
                        Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(CrunchEconCore.path + "//PlayerData//Hauling//InProgress//" + id.ToString() + ".xml");
                        temporary.Add(id, contract);
                    }
                }
            }
            loadedHauling = temporary;
            return temporary;
        }


    }
}
