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
        public static MiningContract GeneratedToPlayer(GeneratedContract gen)
        {
            MiningContract contract = new MiningContract();
            contract.OreSubType = gen.OreSubType;
            contract.GenerateAmountToMine(gen.minimumToMine, gen.maximumToMine);
            contract.contractPrice = contract.amountToMine * gen.PricePerOre;
            contract.minedAmount = 0;

            return contract;
        }
        public static GeneratedContract GetRandomPlayerContract()
        {
            GeneratedContract output = null;
            Random random = new Random();
            List<GeneratedContract> temp = new List<GeneratedContract>();
            int count = 0;
            foreach (GeneratedContract contract in newContracts.Values)
            {
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
                if (contract.Enabled && !newContracts.ContainsKey(contract.Name))
                {
                    newContracts.Add(contract.Name, contract);
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

        public static void LoadDeliveryLocations()
        {
            DrillPatch.locations.Clear();
            String[] line;
            if (System.IO.File.Exists(CrunchEconCore.path + "//ContractConfigs//MiningDeliveryLocations.txt"))
            {
                line = File.ReadAllLines(CrunchEconCore.path + "//ContractConfigs//MiningDeliveryLocations.txt");
                for (int i = 0; i < line.Length; i++)
                {
                    if (ScanChat(line[i]) != null)
                    {
                        MyGps gpsRef = ScanChat(line[i]);
                        gpsRef.GPSColor = Color.DarkOrange;
                        gpsRef.ShowOnHud = true;
                        DrillPatch.locations.Add(gpsRef);
                    }
                }
            }

            //HaulingCore.DeliveryLocations.Clear();
            //line = File.ReadAllLines(path + "//HaulingStuff//deliveryLocations.txt");
            //for (int i = 0; i < line.Length; i++)
            //{
            //    if (ScanChat(line[i]) != null)
            //    {
            //        MyGps gpsRef = ScanChat(line[i]);
            //        HaulingCore.DeliveryLocations.Add(gpsRef);
            //    }
            //}
        }

        public static DateTime chat = DateTime.Now;
        public void GenerateNewMiningContracts(MyPlayer player)
        {
            MiningContract contract;
            Boolean generate = false;
            CrunchEconCore.playerData.TryGetValue(player.Id.SteamId, out PlayerData data);
            if (data == null)
            {
                data = new PlayerData();
                data.steamId = player.Id.SteamId;
            }

            if (data.getMiningContracts().Count == 0)
            {

                contract = new MiningContract();
                generate = true;

            }
            if (generate)
            {
                GeneratedContract newContract = GetRandomPlayerContract();

                if (newContract == null)
                {
                    CrunchEconCore.SendMessage("Big Boss Dave", "No contract available.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));
                    return;
                }
                contract = GeneratedToPlayer(newContract);
                contract.PlayerSteamId = player.Id.SteamId;

                CrunchEconCore.SendMessage("Big Boss Dave", "New job for you, !mc info or !mc quit", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));
                CrunchEconCore.miningSave.Remove(player.Id.SteamId);
                CrunchEconCore.miningSave.Add(player.Id.SteamId, contract);
                CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);
            }
            else
            {

                    CrunchEconCore.SendMessage("Big Boss Dave", "Check contract info with !contract info", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(player.Identity.IdentityId));
                
            }
        }



    }
}
