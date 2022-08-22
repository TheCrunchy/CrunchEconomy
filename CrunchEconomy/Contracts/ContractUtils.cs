using CrunchEconomy.SurveyMissions;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CrunchEconomy.Helpers;
using CrunchEconomy.Station_Stuff;
using CrunchEconomy.Station_Stuff.Objects;
using VRage.Game;
using VRageMath;
using static CrunchEconomy.Contracts.GeneratedContract;

namespace CrunchEconomy.Contracts
{
    public class ContractUtils
    {
        static Random rand = new Random();
        public static Contract GeneratedToPlayer(GeneratedContract gen)
        {
            var contract = new Contract();
            contract.type = gen.type;
            var temporary = new List<ContractInfo>();
            foreach (var info in gen.ItemsToPickFrom)
            {
                var chance = rand.NextDouble();
                if (chance <= info.chance)
                {
                    temporary.Add(info);
                }
            }
            if (temporary.Count == 1)
            {
                contract.TypeIfHauling = temporary[0].TypeId;
                contract.SubType = temporary[0].SubTypeId;
            }
            else
            {
                var temp = rand.Next(temporary.Count);
                contract.TypeIfHauling = temporary[temp].TypeId;
                contract.SubType = temporary[temp].SubTypeId;
            }
            contract.GenerateAmountToMine(gen.minimum, gen.maximum);
            contract.contractPrice = Convert.ToInt64(contract.amountToMineOrDeliver * gen.PricePerOre);
            contract.minedAmount = 0;
            contract.ContractName = gen.Name;
            contract.SpawnItemsInPlayerInventory = gen.SpawnItemsInPlayerInvent;
            contract.reputation = gen.ReputationGain;
            contract.PlayerLoot = gen.PlayerLoot;
            contract.PutInStation = gen.PutInStation;
            contract.CargoName = gen.StationCargoName;
            contract.PutTheHaulInStation = gen.PutTheHaulInStation;
            contract.CooldownInSeconds = gen.CooldownInSeconds;
            return contract;
        }
        public static List<SurveyMission> SurveyMissions = new List<SurveyMission>();

        public static SurveyMission GetNewMission(PlayerData data)
        {
            SurveyMission chosen = null;
            chosen.id = Guid.NewGuid();
            var Possible = (from mission in SurveyMissions where mission.enabled where data.SurveyReputation >= mission.ReputationRequired let rand = new Random() let chance = rand.NextDouble() where chance <= mission.chance where mission.getStage(1) != null && mission.getStage(1).enabled where data.SurveyReputation >= mission.getStage(1).MinimumReputation && data.SurveyReputation <= mission.getStage(1).MaximumReputation where GpsHelper.ParseGPS(mission.getStage(1).LocationGPS) != null select mission).ToList();
            switch (Possible.Count)
            {
                case 0:
                    return null;
                case 1:
                    chosen = Possible[0];
                    return chosen;
            }

            var random = new Random();
            var r = random.Next(Possible.Count);
            chosen = Possible[r];

            return chosen ?? null;
        }

        public static Stations GetDeliveryLocation(Contract contract)
        {
            var locations = CrunchEconCore.stations.Where(station => station.getGPS() != null && station.UseAsDeliveryLocationForContracts).ToList();

            var random = new Random();
            if (locations.Count == 1)
            {
                return locations[0];
            }
            var r = random.Next(locations.Count);
            return locations[r];

        }

        public static Dictionary<string, GeneratedContract> newContracts = new Dictionary<string, GeneratedContract>();
        public static void LoadAllContracts()
        {
            newContracts.Clear();
            SurveyMissions.Clear();
            foreach (var s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Mining//"))
            {


                var contract = CrunchEconCore.utils.ReadFromXmlFile<GeneratedContract>(s);
                if (newContracts.ContainsKey(contract.Name))
                {
                    CrunchEconCore.Log.Error("This file doesnt have unique contract name " + s);
                    continue;
                }
                if (contract.Enabled)
                {
                    newContracts.Add(contract.Name, contract);
                }
            }
            foreach (var s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Hauling//"))
            {


                var contract = CrunchEconCore.utils.ReadFromXmlFile<GeneratedContract>(s);
                if (newContracts.ContainsKey(contract.Name))
                {
                    CrunchEconCore.Log.Error("This file doesnt have unique contract name " + s);
                    continue;
                }
                if (contract.Enabled)
                {
                    newContracts.Add(contract.Name, contract);
                }
            }

            foreach (var s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Survey//"))
            {


               var mission = CrunchEconCore.utils.ReadFromXmlFile<SurveyMission>(s);
               if (!mission.enabled) continue;
                mission.SetupMissionList();
                SurveyMissions.Add(mission);
            }
        }

        public static DateTime chat = DateTime.Now;
    }
}
