using NLog;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace CrunchEconomy.Contracts
{

    [PatchShim]
    public static class DrillPatch
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static List<MyGps> locations = new List<MyGps>();


        internal static readonly MethodInfo update =
            typeof(MyDrillBase).GetMethod("TryHarvestOreMaterial", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo updatePatch =
            typeof(DrillPatch).GetMethod(nameof(TestPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");


        public static MyGps GenerateDeliveryLocation(Vector3 location, Contract contract)
        {
            float distance = 0;
            MyGps closest = null;
            locations.Clear();
            foreach (var station in CrunchEconCore.stations.Where(station => station.getGPS() != null))
            {
                locations.Add(station.getGPS());
            }

            foreach (var gps in locations)
            {
                if (distance == 0)
                {
                    closest = gps;
                }
                else
                {
                    var d = Vector3.Distance(gps.Coords, location);
                    if (!(d < distance)) continue;
                    distance = d;
                    closest = gps;
                }
            }
            closest.Description = "Deliver " + contract.amountToMineOrDeliver + " " + contract.SubType + " Ore. !contract info";
            closest.DisplayName = "Ore Delivery Location. " + contract.SubType;
            closest.Name = "Ore Delivery Location. " + contract.SubType;
            closest.DiscardAt = new TimeSpan(600);
            return closest;
        }
        public static Dictionary<ulong, DateTime> messageCooldown = new Dictionary<ulong, DateTime>();

        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Suffixes.Add(updatePatch);

            Log.Info("Patching Successful CrunchDrill!");

        }

        public static Type drill = null;
        public static void TestPatchMethod(MyDrillBase __instance, MyVoxelMaterialDefinition material,
      VRageMath.Vector3 hitPosition,
      int removedAmount,
      bool onlyCheck)
        {
        
            if (CrunchEconCore.config == null)
            {
                return;
            }
            if (!CrunchEconCore.config.PatchesEnabled)
            {
                return;
            }
            if (!CrunchEconCore.config.MiningContractsEnabled)
            {
                return;
            }

            if (__instance.OutputInventory == null || __instance.OutputInventory.Owner == null) return;
            if (!(__instance.OutputInventory.Owner.GetBaseEntity() is MyShipDrill shipDrill)) return;
            if (drill == null)
            {
                drill = __instance.GetType();
            }

            if (string.IsNullOrEmpty(material.MinedOre))
                return;

            if (onlyCheck) return;
            long playerId = 0;

            foreach (var cockpit in shipDrill.CubeGrid.GetFatBlocks().OfType<IMyCockpit>())
            {
                if (cockpit.Pilot == null) continue;
                var pilot = cockpit.Pilot as MyCharacter;
                if (!CrunchEconCore.PlayerStorageProvider.playerData.TryGetValue(
                        MySession.Static.Players.TryGetSteamId(pilot.GetPlayerIdentityId()),
                        out var data)) continue;

                playerId = pilot.GetPlayerIdentityId();
                var newObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                newObject.MaterialTypeName = new MyStringHash?(material.Id.SubtypeId);
                var num = (float)((double)removedAmount / (double)byte.MaxValue * 1.0) * __instance.VoxelHarvestRatio * material.MinedOreRatio;
                if (!MySession.Static.AmountMined.ContainsKey(material.MinedOre))
                    MySession.Static.AmountMined[material.MinedOre] = (MyFixedPoint)0;
                MySession.Static.AmountMined[material.MinedOre] += (MyFixedPoint)num;
                var physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition((MyObjectBuilder_Base)newObject);
                var amountItems1 = (MyFixedPoint)(num / physicalItemDefinition.Volume);
                var maxAmountPerDrop = (MyFixedPoint)(float)(0.150000005960464 / (double)physicalItemDefinition.Volume);


                var collectionRatio = (MyFixedPoint)drill.GetField("m_inventoryCollectionRatio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                var b = amountItems1 * ((MyFixedPoint)1 - collectionRatio);
                var amountItems2 = MyFixedPoint.Min(maxAmountPerDrop * 10 - (MyFixedPoint)0.001, b);
                var totalAmount = amountItems1 * collectionRatio - amountItems2;

                foreach (var contract in data.GetMiningContracts().Values.Where(contract => contract.minedAmount < contract.amountToMineOrDeliver).Where(contract => contract.SubType.Equals(material.MinedOre) && totalAmount > 0))
                {
                    contract.AddToContractAmount(totalAmount.ToIntSafe());
                    if (contract.minedAmount >= contract.amountToMineOrDeliver)
                    {
                        if (string.IsNullOrEmpty(contract.DeliveryLocation))
                        {
                            var location = GenerateDeliveryLocation(hitPosition, contract);
                            if (location != null)
                            {
                                contract.DeliveryLocation = (location.ToString());
                                contract.DoPlayerGps(playerId);
                            }
                        }
                        CrunchEconCore.SendMessage("Boss Dave", "Contract Ready to be completed, Deliver " + String.Format("{0:n0}", contract.amountToMineOrDeliver) + " " + contract.SubType + " to the delivery GPS.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                        contract.DoPlayerGps(pilot.GetPlayerIdentityId());
                        messageCooldown.Remove(MySession.Static.Players.TryGetSteamId(playerId));
                        messageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(0.5));

                        data.GetMiningContracts()[contract.ContractId] = contract;

                        CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract);
                        return;
                    }

                    if (messageCooldown.TryGetValue(MySession.Static.Players.TryGetSteamId(playerId), out var time))
                    {
                        if (DateTime.Now >= time)
                        {
                            CrunchEconCore.SendMessage("Boss Dave", "Progress: " + material.MinedOre + " " + String.Format("{0:n0}", contract.minedAmount) + " / " + String.Format("{0:n0}", contract.amountToMineOrDeliver), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                            messageCooldown[MySession.Static.Players.TryGetSteamId(playerId)] = DateTime.Now.AddSeconds(0.5);
                        }
                    }
                    else
                    {
                        CrunchEconCore.SendMessage("Boss Dave", "Progress: " + material.MinedOre + " " + String.Format("{0:n0}", contract.minedAmount) + " / " + String.Format("{0:n0}", contract.amountToMineOrDeliver), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                        messageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(0.5));
                    }
                    data.GetMiningContracts()[contract.ContractId] = contract;
                    CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract);
                    return;
                }
            }
        }
    }
}
