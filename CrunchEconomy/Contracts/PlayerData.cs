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

        private Dictionary<Guid, MiningContract> loadedMining = new Dictionary<Guid, MiningContract>();
        private Dictionary<Guid, HaulingContract> loadedHauling = new Dictionary<Guid, HaulingContract>();

        public void addMining(MiningContract contract)
        {
            if (!loadedMining.ContainsKey(contract.ContractId))
            {
                loadedMining.Add(contract.ContractId, contract);
            }
        }

        public Dictionary<Guid, MiningContract> getMiningContracts()
        {
            if (loadedMining.Count > 0)
            {
                return loadedMining;
            }
            Dictionary<Guid, MiningContract> temporary = new Dictionary<Guid, MiningContract>();
            foreach (Guid id in MiningContracts)
            {
                if (File.Exists(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + id.ToString() + ".xml"))
                {
                    if (!temporary.ContainsKey(id))
                    {
                        MiningContract contract = CrunchEconCore.utils.ReadFromXmlFile<MiningContract>(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + id.ToString() + ".xml");
                        temporary.Add(id, contract);
                    }
                }
            }

            loadedMining = temporary;
            return temporary;
          
        }
        public Dictionary<Guid, HaulingContract> getHaulingContracts()
        {
            if (loadedHauling.Count > 0)
            {
                return loadedHauling;
            }
            Dictionary<Guid, HaulingContract> temporary = new Dictionary<Guid, HaulingContract>();
            foreach (Guid id in HaulingContracts)
            {
                if (File.Exists(CrunchEconCore.path + "//PlayerData//Hauling//InProgress//" + id.ToString() + ".xml"))
                {
                    if (!temporary.ContainsKey(id))
                    {
                        HaulingContract contract = CrunchEconCore.utils.ReadFromXmlFile<HaulingContract>(CrunchEconCore.path + "//PlayerData//Hauling//InProgress//" + id.ToString() + ".xml");
                        temporary.Add(id, contract);
                    }
                }
            }
            loadedHauling = temporary;
            return temporary;
        }


    }
}
