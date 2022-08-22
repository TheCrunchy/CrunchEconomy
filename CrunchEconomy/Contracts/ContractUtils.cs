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
        static Random _rand = new Random();
        public static Contract GeneratedToPlayer(GeneratedContract Gen)
        {
            var contract = new Contract();
            contract.Type = Gen.Type;
            var temporary = new List<ContractInfo>();
            foreach (var info in Gen.ItemsToPickFrom)
            {
                var chance = _rand.NextDouble();
                if (chance <= info.Chance)
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
                var temp = _rand.Next(temporary.Count);
                contract.TypeIfHauling = temporary[temp].TypeId;
                contract.SubType = temporary[temp].SubTypeId;
            }
            contract.GenerateAmountToMine(Gen.Minimum, Gen.Maximum);
            contract.ContractPrice = Convert.ToInt64(contract.AmountToMineOrDeliver * Gen.PricePerOre);
            contract.MinedAmount = 0;
            contract.ContractName = Gen.Name;
            contract.SpawnItemsInPlayerInventory = Gen.SpawnItemsInPlayerInvent;
            contract.Reputation = Gen.ReputationGain;
            contract.PlayerLoot = Gen.PlayerLoot;
            contract.PutInStation = Gen.PutInStation;
            contract.CargoName = Gen.StationCargoName;
            contract.PutTheHaulInStation = Gen.PutTheHaulInStation;
            contract.CooldownInSeconds = Gen.CooldownInSeconds;
            return contract;
        }
        public static List<SurveyMission> SurveyMissions = new List<SurveyMission>();

        public static SurveyMission GetNewMission(PlayerData Data)
        {
            SurveyMission chosen = null;
            chosen.Id = Guid.NewGuid();
            var possible = (from mission in SurveyMissions where mission.Enabled where Data.SurveyReputation >= mission.ReputationRequired let rand = new Random() let chance = rand.NextDouble() where chance <= mission.Chance where mission.GetStage(1) != null && mission.GetStage(1).Enabled where Data.SurveyReputation >= mission.GetStage(1).MinimumReputation && Data.SurveyReputation <= mission.GetStage(1).MaximumReputation where GpsHelper.ParseGps(mission.GetStage(1).LocationGps) != null select mission).ToList();
            switch (possible.Count)
            {
                case 0:
                    return null;
                case 1:
                    chosen = possible[0];
                    return chosen;
            }

            var random = new Random();
            var r = random.Next(possible.Count);
            chosen = possible[r];

            return chosen ?? null;
        }

        public static Stations GetDeliveryLocation(Contract Contract)
        {
            var locations = CrunchEconCore.Stations.Where(Station => Station.GetGps() != null && Station.UseAsDeliveryLocationForContracts).ToList();

            var random = new Random();
            if (locations.Count == 1)
            {
                return locations[0];
            }
            var r = random.Next(locations.Count);
            return locations[r];

        }

        public static Dictionary<string, GeneratedContract> NewContracts = new Dictionary<string, GeneratedContract>();
        public static void LoadAllContracts()
        {
            NewContracts.Clear();
            SurveyMissions.Clear();
            foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//ContractConfigs//Mining//"))
            {


                var contract = CrunchEconCore.Utils.ReadFromXmlFile<GeneratedContract>(s);
                if (NewContracts.ContainsKey(contract.Name))
                {
                    CrunchEconCore.Log.Error("This file doesnt have unique contract name " + s);
                    continue;
                }
                if (contract.Enabled)
                {
                    NewContracts.Add(contract.Name, contract);
                }
            }
            foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//ContractConfigs//Hauling//"))
            {


                var contract = CrunchEconCore.Utils.ReadFromXmlFile<GeneratedContract>(s);
                if (NewContracts.ContainsKey(contract.Name))
                {
                    CrunchEconCore.Log.Error("This file doesnt have unique contract name " + s);
                    continue;
                }
                if (contract.Enabled)
                {
                    NewContracts.Add(contract.Name, contract);
                }
            }

            foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//ContractConfigs//Survey//"))
            {


               var mission = CrunchEconCore.Utils.ReadFromXmlFile<SurveyMission>(s);
               if (!mission.Enabled) continue;
                mission.SetupMissionList();
                SurveyMissions.Add(mission);
            }
        }

        public static DateTime Chat = DateTime.Now;
    }
}
