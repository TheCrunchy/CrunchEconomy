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
        public static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static List<MyGps> Locations = new List<MyGps>();


        internal static readonly MethodInfo update =
            typeof(MyDrillBase).GetMethod("TryHarvestOreMaterial", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo updatePatch =
            typeof(DrillPatch).GetMethod(nameof(TestPatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");


        public static MyGps GenerateDeliveryLocation(Vector3 Location, Contract Contract)
        {
            float distance = 0;
            MyGps closest = null;
            Locations.Clear();
            foreach (var station in CrunchEconCore.Stations.Where(Station => Station.GetGps() != null))
            {
                Locations.Add(station.GetGps());
            }

            foreach (var gps in Locations)
            {
                if (distance == 0)
                {
                    closest = gps;
                }
                else
                {
                    var d = Vector3.Distance(gps.Coords, Location);
                    if (!(d < distance)) continue;
                    distance = d;
                    closest = gps;
                }
            }
            closest.Description = "Deliver " + Contract.AmountToMineOrDeliver + " " + Contract.SubType + " Ore. !contract info";
            closest.DisplayName = "Ore Delivery Location. " + Contract.SubType;
            closest.Name = "Ore Delivery Location. " + Contract.SubType;
            closest.DiscardAt = new TimeSpan(600);
            return closest;
        }
        public static Dictionary<ulong, DateTime> MessageCooldown = new Dictionary<ulong, DateTime>();

        public static void Patch(PatchContext Ctx)
        {

            Ctx.GetPattern(update).Suffixes.Add(updatePatch);

            log.Info("Patching Successful CrunchDrill!");

        }

        public static Type Drill = null;
        public static void TestPatchMethod(MyDrillBase Instance, MyVoxelMaterialDefinition Material,
      VRageMath.Vector3 HitPosition,
      int RemovedAmount,
      bool OnlyCheck)
        {
        
            if (CrunchEconCore.Config == null)
            {
                return;
            }
            if (!CrunchEconCore.Config.PatchesEnabled)
            {
                return;
            }
            if (!CrunchEconCore.Config.MiningContractsEnabled)
            {
                return;
            }

            if (Instance.OutputInventory == null || Instance.OutputInventory.Owner == null) return;
            if (!(Instance.OutputInventory.Owner.GetBaseEntity() is MyShipDrill shipDrill)) return;
            if (Drill == null)
            {
                Drill = Instance.GetType();
            }

            if (string.IsNullOrEmpty(Material.MinedOre))
                return;

            if (OnlyCheck) return;
            long playerId = 0;

            foreach (var cockpit in shipDrill.CubeGrid.GetFatBlocks().OfType<IMyCockpit>())
            {
                if (cockpit.Pilot == null) continue;
                var pilot = cockpit.Pilot as MyCharacter;
                if (!CrunchEconCore.PlayerStorageProvider.PlayerData.TryGetValue(
                        MySession.Static.Players.TryGetSteamId(pilot.GetPlayerIdentityId()),
                        out var data)) continue;

                playerId = pilot.GetPlayerIdentityId();
                var newObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(Material.MinedOre);
                newObject.MaterialTypeName = new MyStringHash?(Material.Id.SubtypeId);
                var num = (float)((double)RemovedAmount / (double)byte.MaxValue * 1.0) * Instance.VoxelHarvestRatio * Material.MinedOreRatio;
                if (!MySession.Static.AmountMined.ContainsKey(Material.MinedOre))
                    MySession.Static.AmountMined[Material.MinedOre] = (MyFixedPoint)0;
                MySession.Static.AmountMined[Material.MinedOre] += (MyFixedPoint)num;
                var physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition((MyObjectBuilder_Base)newObject);
                var amountItems1 = (MyFixedPoint)(num / physicalItemDefinition.Volume);
                var maxAmountPerDrop = (MyFixedPoint)(float)(0.150000005960464 / (double)physicalItemDefinition.Volume);


                var collectionRatio = (MyFixedPoint)Drill.GetField("m_inventoryCollectionRatio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Instance);
                var b = amountItems1 * ((MyFixedPoint)1 - collectionRatio);
                var amountItems2 = MyFixedPoint.Min(maxAmountPerDrop * 10 - (MyFixedPoint)0.001, b);
                var totalAmount = amountItems1 * collectionRatio - amountItems2;

                foreach (var contract in data.GetMiningContracts().Values.Where(Contract => Contract.MinedAmount < Contract.AmountToMineOrDeliver).Where(Contract => Contract.SubType.Equals(Material.MinedOre) && totalAmount > 0))
                {
                    contract.AddToContractAmount(totalAmount.ToIntSafe());
                    if (contract.MinedAmount >= contract.AmountToMineOrDeliver)
                    {
                        if (string.IsNullOrEmpty(contract.DeliveryLocation))
                        {
                            var location = GenerateDeliveryLocation(HitPosition, contract);
                            if (location != null)
                            {
                                contract.DeliveryLocation = (location.ToString());
                                contract.DoPlayerGps(playerId);
                            }
                        }
                        CrunchEconCore.SendMessage("Boss Dave", "Contract Ready to be completed, Deliver " + String.Format("{0:n0}", contract.AmountToMineOrDeliver) + " " + contract.SubType + " to the delivery GPS.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                        contract.DoPlayerGps(pilot.GetPlayerIdentityId());
                        MessageCooldown.Remove(MySession.Static.Players.TryGetSteamId(playerId));
                        MessageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(0.5));

                        data.GetMiningContracts()[contract.ContractId] = contract;

                        CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract);
                        return;
                    }

                    if (MessageCooldown.TryGetValue(MySession.Static.Players.TryGetSteamId(playerId), out var time))
                    {
                        if (DateTime.Now >= time)
                        {
                            CrunchEconCore.SendMessage("Boss Dave", "Progress: " + Material.MinedOre + " " + String.Format("{0:n0}", contract.MinedAmount) + " / " + String.Format("{0:n0}", contract.AmountToMineOrDeliver), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                            MessageCooldown[MySession.Static.Players.TryGetSteamId(playerId)] = DateTime.Now.AddSeconds(0.5);
                        }
                    }
                    else
                    {
                        CrunchEconCore.SendMessage("Boss Dave", "Progress: " + Material.MinedOre + " " + String.Format("{0:n0}", contract.MinedAmount) + " / " + String.Format("{0:n0}", contract.AmountToMineOrDeliver), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                        MessageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(0.5));
                    }
                    data.GetMiningContracts()[contract.ContractId] = contract;
                    CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(contract);
                    return;
                }
            }
        }
    }
}
