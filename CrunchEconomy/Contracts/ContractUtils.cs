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
using VRage.Game;
using VRageMath;

namespace CrunchEconomy.Contracts
{
    public class ContractUtils
    {
        public static Contract GeneratedToPlayer(GeneratedContract gen)
        {
            Contract contract = new Contract();
            contract.type = gen.type;
            contract.TypeIfHauling = gen.TypeIfHauling;
            contract.SubType = gen.SubType;
            contract.GenerateAmountToMine(gen.minimum, gen.maximum);
            contract.contractPrice = contract.amountToMineOrDeliver * gen.PricePerOre;
            contract.minedAmount = 0;
            contract.ContractName = gen.Name;
            contract.SpawnItemsInPlayerInventory = gen.SpawnItemsInPlayerInvent;
            contract.reputation = gen.ReputationGain;
            contract.PlayerLoot = gen.PlayerLoot;
            contract.PutInStation = gen.PutInStation;
            contract.CargoName = gen.StationCargoName;
            return contract;
        }
        public static List<SurveyMission> SurveyMissions = new List<SurveyMission>();

        public static SurveyMission GetNewMission(PlayerData data)
        {
            List<SurveyMission> Possible = new List<SurveyMission>();
            SurveyMission chosen = null;
            foreach (SurveyMission mission in SurveyMissions)
            {
                if (mission.enabled)
                {
                    if (data.SurveyReputation >= mission.ReputationRequired)
                    {
                        Random rand = new Random();
                        double chance = rand.NextDouble();
                        if (chance <= mission.chance)
                        {
                            if (mission.getStage(1) != null && mission.getStage(1).enabled)
                            {
                                if (data.SurveyReputation >= mission.getStage(1).MinimumReputation && data.SurveyReputation <= mission.getStage(1).MaximumReputation)
                                {
                                    if (ScanChat(mission.getStage(1).LocationGPS) != null)
                                    {
                                        Possible.Add(mission);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (Possible.Count == 0)
            {
                return null;
            }
            if (Possible.Count == 1)
            {
                chosen = Possible[0];
            }
            Random random = new Random();
            int r = random.Next(Possible.Count);
            chosen = Possible[r];

            if (chosen != null)
            {
                chosen.id = Guid.NewGuid();
                return chosen;
            }
            
            return null;
        }

        public static Stations GetDeliveryLocation(Contract contract)
        {
            List<Stations> locations = new List<Stations>();
            foreach (Stations station in CrunchEconCore.stations)
            {
                if (station.getGPS() != null && station.UseAsDeliveryLocationForContracts)
                {
                    locations.Add(station);
                }
            }

            Random random = new Random();
            if (locations.Count == 1)
            {
                return locations[0];
            }
            int r = random.Next(locations.Count);
            return locations[r];

        }

        public static GeneratedContract GetRandomPlayerContract(ContractType type)
        {
            GeneratedContract output = null;
            Random random = new Random();
            List<GeneratedContract> temp = new List<GeneratedContract>();
            int count = 0;
            foreach (GeneratedContract contract in newContracts.Values)
            {
                if (contract.type != type)
                {
                    continue;
                }
                count++;
                double chance = random.Next(0, 101);
                if (chance <= contract.chance)
                {
                    temp.Add(contract);
                }
            }
            if (temp.Count == 0)
            {
                return null;
            }
            if (temp.Count == 1)
            {
                output = temp.ElementAt(0);
            }
            else
            {

                int index = random.Next(temp.Count - 1);

                output = temp.ElementAt(index);
            }

            return output;
        }

        public static Dictionary<string, GeneratedContract> newContracts = new Dictionary<string, GeneratedContract>();
        public static void LoadAllContracts()
        {
            newContracts.Clear();
            SurveyMissions.Clear();
            foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Mining//"))
            {


                GeneratedContract contract = CrunchEconCore.utils.ReadFromXmlFile<GeneratedContract>(s);
                //  DateTime now = DateTime.Now;
                //if (now.Minute == 59 || now.Minute == 60)
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                //}
                //else
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                //}     
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
            foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Hauling//"))
            {


                GeneratedContract contract = CrunchEconCore.utils.ReadFromXmlFile<GeneratedContract>(s);
                //  DateTime now = DateTime.Now;
                //if (now.Minute == 59 || now.Minute == 60)
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                //}
                //else
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                //}
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

            foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//ContractConfigs//Survey//"))
            {


               SurveyMission mission = CrunchEconCore.utils.ReadFromXmlFile<SurveyMission>(s);
                //  DateTime now = DateTime.Now;
                //if (now.Minute == 59 || now.Minute == 60)
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour + 1, 0, 0, 0, DateTimeKind.Utc);
                //}
                //else
                //{
                //    koth.nextCaptureInterval = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute + 1, 0, 0, DateTimeKind.Utc);
                //}
                if (mission.enabled)
                {
                    mission.SetupMissionList();
                    SurveyMissions.Add(mission);
                }
            }
        }
        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }



        public static DateTime chat = DateTime.Now;
        //public void GenerateNewMiningContracts(MyPlayer player)
        //{
        //    Contract contract;
        //    Boolean generate = false;
        //    CrunchEconCore.playerData.TryGetValue(player.Id.SteamId, out PlayerData data);
        //    if (data == null)
        //    {
        //        data = new PlayerData();
        //        data.steamId = player.Id.SteamId;
        //    }

        //    if (data.getMiningContracts().Count == 0)
        //    {

        //        contract = new Contract();
        //        generate = true;

        //    }
        //    if (generate)
        //    {
        //        GeneratedContract newContract = GetRandomPlayerContract(ContractType.Mining);

        //        if (newContract == null)
        //        {
        //            CrunchEconCore.SendMessage("Big Boss Dave", "No contract available.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));
        //            return;
        //        }
        //        contract = GeneratedToPlayer(newContract);
        //        contract.PlayerSteamId = player.Id.SteamId;

        //        CrunchEconCore.SendMessage("Big Boss Dave", "New job for you, !contract info", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));
        //        CrunchEconCore.ContractSave.Remove(contract.ContractId);
        //        CrunchEconCore.ContractSave.Add(contract.ContractId, contract);
        //        CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);
        //    }
        //    else
        //    {

        //        CrunchEconCore.SendMessage("Big Boss Dave", "Check contract info with !contract info", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));

        //    }
        //}



    }
}
