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
using CrunchEconomy.Helpers;
using VRage.Game;
using VRageMath;

namespace CrunchEconomy.Contracts
{
    public class ShipContract
    {
        public string Name = "Example";
        public ContractStatus status = ContractStatus.InProgress;
        public Guid ContractId = Guid.Empty;
        public long BasePrice = 50000;
        public long BonusIfBelowStock = 50000;
        public string LinkedToStore = "Put a store name here that sells ships";
        public string LinkedTypeId = "Type id";
        public string LinkedSubTypeId = "Subtype id";
        public int LinkedBonusUntilStockLevel = 5;
        public Boolean PickRandomFromList = false;

        public List<RewardItem> PlayerLoot = new List<RewardItem>();
        public List<RewardItem> PutInStation = new List<RewardItem>();
        public long StationEntityId = 0;
        public List<ShipItem> blocksRequired = new List<ShipItem>();

        public String DeliveryLocation;
        public void DoPlayerGps(long identityId)
        {
            var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
            var sb = new StringBuilder();
            sb.AppendLine("Deliver grid !contract info");
            sb.AppendLine("Contract Delivery Location.");
            if (GpsHelper.ParseGPS(DeliveryLocation) == null) return;
            var gpsRef = GpsHelper.ParseGPS(DeliveryLocation);
            gpsRef.GPSColor = Color.DarkOrange;
            gpsRef.ShowOnHud = true;
            gpsRef.Description = sb.ToString();
            gpsRef.DisplayName = "Grid Delivery Location. ";
            gpsRef.Name = "Grid Delivery Location. ";
            gpsRef.DiscardAt = new TimeSpan(600);
            gpscol.SendAddGpsRequest(identityId, ref gpsRef);
        }
        public Vector3 getCoords()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Deliver grid !contract info");
            sb.AppendLine("Contract Delivery Location.");
            if (GpsHelper.ParseGPS(DeliveryLocation) == null) return new Vector3(0, 0, 0);
            var gpsRef = GpsHelper.ParseGPS(DeliveryLocation);
            gpsRef.GPSColor = Color.DarkOrange;
            gpsRef.ShowOnHud = true;
            gpsRef.Description = sb.ToString();
            gpsRef.DisplayName = "Grid Delivery Location. ";
            gpsRef.Name = "Grid Delivery Location. ";
            gpsRef.DiscardAt = new TimeSpan(600);


            return gpsRef.Coords;

        }
        public class ShipItem
        {
            public int Minimum = 0;
            public int Maximum = 1;
            public string ObjectBuilder = "You can create a ship contract by looking at a grid and using !shipcontract create <name>";
            public int DeliverAmount = 1;
            public long PricePerBlockIfRandom = 500;
        }
    }
}
