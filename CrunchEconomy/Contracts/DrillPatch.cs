﻿using NLog;
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


        public static MyGps GenerateDeliveryLocation(Vector3 location, MiningContract contract)
        {
            float distance = 0;
            MyGps closest = null;
            foreach (MyGps gps in locations)
            {
                if (distance == 0)
                {
                    closest = gps;
                }
                else
                {
                    float d = Vector3.Distance(gps.Coords, location);
                    if (d < distance)
                    {
                        distance = d;
                        closest = gps;
                    }
                }
            }
            closest.Description = "Deliver " + contract.amountToMine + " " + contract.OreSubType + " Ore. !mc info";
            closest.DisplayName = "Ore Delivery Location. " + contract.OreSubType;
            closest.Name = "Ore Delivery Location. " + contract.OreSubType;
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
        public static FileUtils utils = new FileUtils();
        public static void TestPatchMethod(MyDrillBase __instance, MyVoxelMaterialDefinition material,
      VRageMath.Vector3 hitPosition,
      int removedAmount,
      bool onlyCheck)
        {
            if (CrunchEconCore.config == null)
            {
                return;
            }
            if (!CrunchEconCore.config.MiningContractsEnabled)
            {
                return;
            }
            if (__instance.OutputInventory != null && __instance.OutputInventory.Owner != null)
            {
                if (__instance.OutputInventory.Owner.GetBaseEntity() is MyShipDrill shipDrill)
                {
                    if (drill == null)
                    {
                        drill = __instance.GetType();
                    }

                    if (string.IsNullOrEmpty(material.MinedOre))
                        return;

                    if (!onlyCheck)
                    {
                        long playerId = 0;

                        foreach (IMyCockpit cockpit in shipDrill.CubeGrid.GetFatBlocks().OfType<IMyCockpit>())
                        {
                            if (cockpit.Pilot != null)
                            {
                                MyCharacter pilot = cockpit.Pilot as MyCharacter;
                                if (CrunchEconCore.playerData.TryGetValue(MySession.Static.Players.TryGetSteamId(pilot.GetPlayerIdentityId()), out PlayerData data))
                                {

                                    playerId = pilot.GetPlayerIdentityId();
                                    MyObjectBuilder_Ore newObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                                    newObject.MaterialTypeName = new MyStringHash?(material.Id.SubtypeId);
                                    float num = (float)((double)removedAmount / (double)byte.MaxValue * 1.0) * __instance.VoxelHarvestRatio * material.MinedOreRatio;
                                    if (!MySession.Static.AmountMined.ContainsKey(material.MinedOre))
                                        MySession.Static.AmountMined[material.MinedOre] = (MyFixedPoint)0;
                                    MySession.Static.AmountMined[material.MinedOre] += (MyFixedPoint)num;
                                    MyPhysicalItemDefinition physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition((MyObjectBuilder_Base)newObject);
                                    MyFixedPoint amountItems1 = (MyFixedPoint)(num / physicalItemDefinition.Volume);
                                    MyFixedPoint maxAmountPerDrop = (MyFixedPoint)(float)(0.150000005960464 / (double)physicalItemDefinition.Volume);


                                    MyFixedPoint collectionRatio = (MyFixedPoint)drill.GetField("m_inventoryCollectionRatio", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
                                    MyFixedPoint b = amountItems1 * ((MyFixedPoint)1 - collectionRatio);
                                    MyFixedPoint amountItems2 = MyFixedPoint.Min(maxAmountPerDrop * 10 - (MyFixedPoint)0.001, b);
                                    MyFixedPoint totalAmount = amountItems1 * collectionRatio - amountItems2;

                                    //if they can have multiple contracts edit this bit
                                    //if they can have multiple contracts edit this bit
                                    //if they can have multiple contracts edit this bit
                                    foreach (MiningContract contract in data.getMiningContracts().Values)
                                    {

                                        if (contract.minedAmount >= contract.amountToMine)
                                        {
                                            continue;
                                        }
                                        if (contract.OreSubType.Equals(material.MinedOre))
                                        {
                                            
                                            if (contract.AddToContractAmount(totalAmount.ToIntSafe()))
                                            {

                                                if (contract.DeliveryLocation == string.Empty)
                                                {
                                                    MyGps location = GenerateDeliveryLocation(hitPosition, contract);
                                                    if (location != null)
                                                    {
                                                        contract.DeliveryLocation = (location.ToString());
                                                        contract.DoPlayerGps(playerId);
                                                        if (messageCooldown.TryGetValue(MySession.Static.Players.TryGetSteamId(playerId), out DateTime time))
                                                        {
                                                            if (DateTime.Now >= time)
                                                            {
                                                                CrunchEconCore.SendMessage("Big Boss Dave", "Contract Ready to be completed, Deliver " + String.Format("{0:n0}", contract.amountToMine) + " " + contract.OreSubType + " to the delivery GPS.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                                                                messageCooldown[MySession.Static.Players.TryGetSteamId(playerId)] = DateTime.Now.AddSeconds(3);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            CrunchEconCore.SendMessage("Big Boss Dave", "Contract Ready to be completed, Deliver " + String.Format("{0:n0}", contract.amountToMine) + " " + contract.OreSubType + " to the delivery GPS.", Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                                                            messageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(3));
                                                        }
                                                    }
                                                }
                                                //im not sure i need this anymore, since the contract data is saved seperately
                                                data.getMiningContracts()[contract.ContractId] = contract;

                                                //REDO THIS
                                                CrunchEconCore.miningSave.Remove(MySession.Static.Players.TryGetSteamId(playerId));
                                                CrunchEconCore.miningSave.Add(MySession.Static.Players.TryGetSteamId(playerId), contract);
                                                return;
                                            }
                                            else
                                            {
                                                if (messageCooldown.TryGetValue(MySession.Static.Players.TryGetSteamId(playerId), out DateTime time))
                                                {
                                                    if (DateTime.Now >= time)
                                                    {
                                                        CrunchEconCore.SendMessage("Big Boss Dave", "Contract progress : " + material.MinedOre + " " + String.Format("{0:n0}", contract.minedAmount) + " / " + String.Format("{0:n0}", contract.amountToMine), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                                                        messageCooldown[MySession.Static.Players.TryGetSteamId(playerId)] = DateTime.Now.AddSeconds(0.5);
                                                    }
                                                }
                                                else
                                                {
                                                    CrunchEconCore.SendMessage("Big Boss Dave", "Contract progress : " + material.MinedOre + " " + String.Format("{0:n0}", contract.minedAmount) + " / " + String.Format("{0:n0}", contract.amountToMine), Color.Gold, (long)MySession.Static.Players.TryGetSteamId(playerId));
                                                    messageCooldown.Add(MySession.Static.Players.TryGetSteamId(playerId), DateTime.Now.AddSeconds(0.5));
                                                }
                                                //im not sure i need this anymore, since the contract data is saved seperately
                                                data.getMiningContracts()[contract.ContractId] = contract;
                                                //REDO THIS
                                                CrunchEconCore.miningSave.Remove(MySession.Static.Players.TryGetSteamId(playerId));
                                                CrunchEconCore.miningSave.Add(MySession.Static.Players.TryGetSteamId(playerId), contract);
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }

        }
    }
}
