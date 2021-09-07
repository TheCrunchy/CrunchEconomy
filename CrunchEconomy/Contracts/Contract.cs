using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace CrunchEconomy.Contracts
{
    public class Contract
    {
        public ContractStatus status = ContractStatus.InProgress;
        public Guid ContractId = Guid.NewGuid();
        public ContractType type;
        public Boolean DoRareItemReward = false;
        public string TypeIfHauling = "Ingot";
        public string ContractName;
        public ulong PlayerSteamId;
        public Boolean GivenItemReward = false;
        public long DistanceBonus = 0;
        public long contractPrice = 0;
        public string CargoName = "default";
        public long StationEntityId = 0;
        public List<RewardItem> PlayerLoot = new List<RewardItem>();
        public List<RewardItem> PutInStation = new List<RewardItem>();
        public int reputation = 1;

        public string SubType;

        public int minedAmount = 0;

        public int amountToMineOrDeliver = 0;
        public long AmountPaid = 0;
        public void DoPlayerGps(long identityId)
        {
            MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Deliver " + amountToMineOrDeliver + " " + SubType + " Ore. !mc info");
            sb.AppendLine("Contract Delivery Location.");
            if (ScanChat(DeliveryLocation) != null)
            {
                MyGps gpsRef = ScanChat(DeliveryLocation);
                gpsRef.GPSColor = Color.DarkOrange;
                gpsRef.ShowOnHud = true;

                gpsRef.Description = sb.ToString() ;
                gpsRef.DisplayName = "Delivery Location. " + SubType;
                gpsRef.Name = "Delivery Location. " + SubType;
                gpsRef.DiscardAt = new TimeSpan(600);
                gpscol.SendAddGps(identityId, ref gpsRef);
            }
        }
        public Vector3 getCoords()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Deliver " + amountToMineOrDeliver + " " + SubType + " Ore. !mc info");
            sb.AppendLine("Contract Delivery Location.");
            if (ScanChat(DeliveryLocation) != null)
            {
                MyGps gpsRef = ScanChat(DeliveryLocation);
                gpsRef.GPSColor = Color.DarkOrange;
                gpsRef.ShowOnHud = true;
                gpsRef.Description = sb.ToString();
                gpsRef.DisplayName = "Delivery Location. " + SubType;
                gpsRef.Name = "Delivery Location. " + SubType;
                gpsRef.DiscardAt = new TimeSpan(600);


                return gpsRef.Coords;
            }

            return new Vector3(0, 0, 0);
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

        public void GenerateAmountToMine(int min, int max)
        {
            Random rnd = new Random();
            amountToMineOrDeliver = rnd.Next(min - 1, max + 1);

        }

        public Boolean AddToContractAmount(int amount)
        {
            minedAmount += amount;
            if (minedAmount >= amountToMineOrDeliver)
            {
                return true;
            }
            return false;
        }

        public String DeliveryLocation;

    }
}

