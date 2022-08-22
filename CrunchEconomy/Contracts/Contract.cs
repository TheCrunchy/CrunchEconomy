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
        public ContractStatus Status = ContractStatus.InProgress;
        public Guid ContractId = Guid.NewGuid();
        public ContractType Type;
        public Boolean DoRareItemReward = false;
        public string TypeIfHauling = "Ingot";
        public string ContractName;
        public ulong PlayerSteamId;
        public Boolean GivenItemReward = false;
        public long DistanceBonus = 0;
        public long ContractPrice = 0;
        public string CargoName = "default";
        
        public long StationEntityId = 0;
        public List<RewardItem> PlayerLoot = new List<RewardItem>();
        public List<RewardItem> PutInStation = new List<RewardItem>();
        public int Reputation = 1;
        public int CooldownInSeconds = 1;
        public string SubType;
        public Boolean PutTheHaulInStation = false;
        public int MinedAmount = 0;
        public Boolean SpawnItemsInPlayerInventory;
        public DateTime TimeCompleted;
        public int AmountToMineOrDeliver = 0;
        public long AmountPaid = 0;
    

        public void DoPlayerGps(long IdentityId)
        {
            var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
            var sb = new StringBuilder();
            sb.AppendLine("Deliver " + AmountToMineOrDeliver + " " + SubType + " Ore. !contract info");
            sb.AppendLine("Contract Delivery Location.");
            if (ScanChat(DeliveryLocation) != null)
            {
                var gpsRef = ScanChat(DeliveryLocation);
                gpsRef.GPSColor = Color.DarkOrange;
                gpsRef.ShowOnHud = true;

                gpsRef.Description = sb.ToString() ;
                gpsRef.DisplayName = "Delivery Location. " + SubType;
                gpsRef.Name = "Delivery Location. " + SubType;
                gpsRef.DiscardAt = new TimeSpan(600);
                gpscol.SendAddGpsRequest(IdentityId, ref gpsRef);
            }
        }
        public Vector3 GetCoords()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Deliver " + AmountToMineOrDeliver + " " + SubType + " Ore. !contract info");
            sb.AppendLine("Contract Delivery Location.");
            if (ScanChat(DeliveryLocation) != null)
            {
                var gpsRef = ScanChat(DeliveryLocation);
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
        public static MyGps ScanChat(string Input, string Desc = null)
        {

            var flag = true;
            var matchCollection = Regex.Matches(Input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            var color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                var str = match.Groups[1].Value;
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
                var gps = new MyGps()
                {
                    Name = str,
                    Description = Desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }

        public void GenerateAmountToMine(int Min, int Max)
        {
            var rnd = new Random();
            AmountToMineOrDeliver = rnd.Next(Min - 1, Max + 1);

        }

        public Boolean AddToContractAmount(int Amount)
        {
            MinedAmount += Amount;
            if (MinedAmount >= AmountToMineOrDeliver)
            {
                return true;
            }
            return false;
        }

        public String DeliveryLocation;

    }
}

