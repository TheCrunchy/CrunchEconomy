﻿using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts.Internal;
using System.Linq;
using System.Reflection;
using System.Text;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using Torch.API.Managers;
using System.IO;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Torch.Mod.Messages;
using Torch.Mod;
using Torch.Managers.ChatManager;
using Torch.Managers;
using Torch.API.Plugins;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;
using Sandbox.Game.Screens.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using NLog;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using ShipMarket;
using static CrunchEconomy.Stations;
using static CrunchEconomy.Contracts.GeneratedContract;
using static CrunchEconomy.RepConfig;
using System.Threading.Tasks;
using NLog.Fluent;
using static CrunchEconomy.WhitelistFile;
using Sandbox.Definitions;
using Torch.Utils.SteamWorkshopTools;
using VRage.ObjectBuilders.Private;

namespace CrunchEconomy
{
    public class CrunchEconCore : TorchPluginBase
    {

        public static TorchSessionState TorchState;
        public static ITorchBase TorchBase;

        public static Logger Log = LogManager.GetLogger("CrunchEcon");

        public static Config config;

        private TorchSessionManager sessionManager;
        public static string path;
        public static string basePath;
        public static DateTime NextFileRefresh = DateTime.Now.AddMinutes(1);

        public static bool paused = false;

        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id)
        {
            MyFixedPoint targetAmount = 0;
            foreach (VRage.Game.ModAPI.IMyInventory inv in inventories)
            {

                VRage.Game.ModAPI.IMyInventoryItem invItem = inv.FindItem(id);

                if (invItem != null)
                {
                    targetAmount += invItem.Amount;
                }
            }

           
            return targetAmount;
        }
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventoriesForContract(MyCubeGrid grid)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                if (block is MyReactor reactor)
                {
                    continue;
                }

                for (int i = 0; i < block.InventoryCount; i++)
                {

                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }

        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid, Stations station)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                if (!block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                {
                    continue;
                }
                if (block is MyReactor reactor)
                {
                    continue;
                }
                if (station.ViewOnlyNamedCargo)
                {
                    var temp = station.CargoName.Split(',').ToList();
                    var cargos = temp.Select(outer => outer.Trim()).ToList();
                    if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText))
                    {
                        continue;
                    }
                }
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }

        public static List<VRage.Game.ModAPI.IMyInventory> ClearInventories(MyCubeGrid grid, Stations station)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var temp = station.CargoName.Split(',').ToList();
            var cargos = temp.Select(outer => outer.Trim()).ToList();
            foreach (var block in grid.GetFatBlocks())
            {
                if (!block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                {
                    continue;
                }
                if (station.ViewOnlyNamedCargo)
                {
                    if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText))
                    {
                        continue;
                    }
                }
                for (int i = 0; i < block.InventoryCount; i++)
                {

                    VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inv.Clear();
                }

            }
            return inventories;
        }

        public bool SpawnLoot(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount)
        {
            if (grid != null)
            {


                foreach (var block in grid.GetFatBlocks())
                {


                    for (int i = 0; i < block.InventoryCount; i++)
                    {

                        VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);

                        MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                        if (inv.CanItemsBeAdded(amount, itemType))
                        {
                            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializerKeen.CreateNewObject(id));
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                //   Log.Info("Should spawn item");


            }
            else
            {

            }
            return false;
        }

        public bool SpawnItems(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount, Stations station)
        {
            //  CrunchEconCore.Log.Info("SPAWNING 1 " + amount);
            if (grid != null)
            {
                bool found = false;
                var temp = station.CargoName.Split(',').ToList();
                var cargos = temp.Select(outer => outer.Trim()).ToList();
                //   CrunchEconCore.Log.Info("GRID NO NULL?");
                foreach (var block in grid.GetFatBlocks())
                {

                    if (!block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                    {

                        continue;
                    }

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        //    CrunchEconCfore.Log.Info("SPAWNING 2 " + amount);

                        VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                        if (station.ViewOnlyNamedCargo)
                        {
                            //     CrunchEconCore.Log.Info("NOT SPAWNING " + block.DisplayNameText);
                            if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText) && !found)
                            {
                                continue;
                            }
                            else
                            {
                                found = true;
                            }
                        }
                        else
                        {
                            found = true;
                        }
                        if (found)
                        {
                            MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                            if (inv.CanItemsBeAdded(amount, itemType))
                            {
                                inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializerKeen.CreateNewObject(id));
                                //      CrunchEconCore.Log.Info("SPAWNING 3 " + amount);
                                return true;
                            }
                        }
                    }
                }
                //   Log.Info("Should spawn item");


            }
            else
            {

            }
            return false;
        }
        int ticks = 0;

        public static void SendMessage(string author, string message, Color color, long steamID)
        {


            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }


        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>> toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponentsTwo(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    if (steamid != 0l)
                    {
                        SendMessage("[Econ]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)steamid);
                    }
                    return false;
                }
            }


            foreach (MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint> item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }
        public static MyFixedPoint CountComponentsTwo(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (VRage.Game.ModAPI.IMyInventory inv in inventories)
            {
                VRage.Game.ModAPI.IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    if (invItem.Amount >= targetAmount)
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                        targetAmount = 0;
                        break;
                    }
                    else
                    {
                        items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                        targetAmount -= invItem.Amount;
                    }
                }
            }
            return targetAmount;
        }

        //got bored, this being async probably doesnt matter at all 
        public static async void DoFactionShit(IPlayer p)
        {

            MyIdentity iden = GetIdentityByNameOrId(p.SteamId.ToString());
            if (iden != null)
            {
                MyFaction player = MySession.Static.Factions.TryGetPlayerFaction(iden.IdentityId) as MyFaction;
                await Task.Run(() =>
                {
                    foreach (RepItem item in repConfig.RepConfigs)
                    {
                        if (item.Enabled)
                        {
                            MyFaction target = MySession.Static.Factions.TryGetFactionByTag(item.FactionTag);
                            if (target != null)
                            {

                                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(iden.IdentityId, target.FactionId, item.PlayerToFactionRep, ReputationChangeReason.Admin);
                                if (player != null)
                                {
                                    MySession.Static.Factions.SetReputationBetweenFactions(player.FactionId, target.FactionId, item.FactionToFactionRep);
                                }
                            }
                        }
                    }
                });

            }

            return;

        }

        public static void Login(IPlayer p)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (p == null)
            {
                return;
            }

            DoFactionShit(p);

            if (File.Exists(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json"))
            {
                PlayerData data = utils.ReadFromJsonFile<PlayerData>(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json");
                if (data == null)
                {

                    File.Delete(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json");
                    if (playerData.TryGetValue(p.SteamId, out PlayerData reee))
                    {
                        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json", reee);
                    }
                    CrunchEconCore.Log.Error("Corrupt Player Data, if they had a previous save before login, that has been restored. " + p.SteamId);

                    return;
                }
                playerData.Remove(p.SteamId);

                data.getMiningContracts();
                data.getHaulingContracts();
                playerData.Add(p.SteamId, data);
                long id = MySession.Static.Players.TryGetIdentityId(p.SteamId);
                if (id == 0)
                {
                    return;
                }
                List<IMyGps> playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(id, playerList);
                foreach (IMyGps gps in playerList)
                {
                    if (gps.Description != null && gps.Description.Contains("Contract Delivery Location."))
                    {
                        MyAPIGateway.Session?.GPS.RemoveGps(id, gps);
                    }
                }

                foreach (Contract c in data.getMiningContracts().Values)
                {

                    if (c.minedAmount >= c.amountToMineOrDeliver)
                    {
                        c.DoPlayerGps(id);
                    }

                }

                foreach (Contract c in data.getHaulingContracts().Values)
                {

                    c.DoPlayerGps(id);

                }
                MyIdentity iden = GetIdentityByNameOrId(p.SteamId.ToString());
                if (iden != null)
                {
                    MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                    foreach (Stations stat in stations)
                    {
                        if (stat.GiveGPSOnLogin)
                        {
                            if (stat.getGPS() != null)
                            {
                                MyGps gps = stat.getGPS();
                                gps.DiscardAt = new TimeSpan(6000);
                                gpscol.SendAddGpsRequest(iden.IdentityId, ref gps);

                            }

                        }
                    }
                }
            }
        }
        public static void Logout(IPlayer p)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (p == null)
            {
                return;
            }

            if (File.Exists(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json"))
            {
                PlayerData data = utils.ReadFromJsonFile<PlayerData>(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json");
                playerData.Remove(p.SteamId);
                data.getMiningContracts();
                data.getHaulingContracts();
                playerData.Add(p.SteamId, data);
            }
        }

        public static Dictionary<ulong, PlayerData> playerData = new Dictionary<ulong, PlayerData>();
        public static Boolean AlliancePluginEnabled = false;
        //i should really split this into multiple methods so i dont have one huge method for everything
        public static Dictionary<Guid, Contract> ContractSave = new Dictionary<Guid, Contract>();
        public static Dictionary<Guid, SurveyMission> SurveySave = new Dictionary<Guid, SurveyMission>();

        public bool HandleDeliver(Contract contract, MyPlayer player, PlayerData data, MyCockpit controller)
        {

            bool proceed = false;

            if (contract.type == ContractType.Mining)
            {
                if (config.MiningContractsEnabled)
                {
                    if (contract.minedAmount >= contract.amountToMineOrDeliver)
                    {
                        proceed = true;
                    }
                }
            }
            else
            {
                if (contract.type == ContractType.Hauling)
                {
                    if (config.HaulingContractsEnabled)
                    {
                        proceed = true;
                    }
                }
            }

            if (proceed)
            {
                if (contract.DeliveryLocation == null && contract.DeliveryLocation == string.Empty)
                {
                    contract.DeliveryLocation =
                        DrillPatch.GenerateDeliveryLocation(player.GetPosition(), contract).ToString();
                    CrunchEconCore.ContractSave.Remove(contract.ContractId);
                    CrunchEconCore.ContractSave.Add(contract.ContractId, contract);
                }

                Vector3D coords = contract.getCoords();
                int rep = 0;
                float distance = Vector3.Distance(coords, controller.PositionComp.GetPosition());
                if (distance <= 500)
                {
                    Dictionary<MyDefinitionId, int> itemsToRemove = new Dictionary<MyDefinitionId, int>();
                    string parseThis;

                    if (contract.type == ContractType.Mining)
                    {
                        parseThis = "MyObjectBuilder_Ore/" + contract.SubType;
                        rep = data.MiningReputation;
                    }
                    else
                    {
                        parseThis = "MyObjectBuilder_" + contract.TypeIfHauling + "/" + contract.SubType;
                        rep = data.HaulingReputation;
                    }

                    if (MyDefinitionId.TryParse(parseThis, out MyDefinitionId id))
                    {
                        itemsToRemove.Add(id, contract.amountToMineOrDeliver);

                    }

                    List<VRage.Game.ModAPI.IMyInventory> inventories = GetInventoriesForContract(controller.CubeGrid);

                    if (FacUtils.IsOwnerOrFactionOwned(controller.CubeGrid, player.Identity.IdentityId, true))
                    {
                        if (!ConsumeComponents(inventories, itemsToRemove, player.Id.SteamId)) return false;
                        if (contract.type == ContractType.Mining)
                        {

                            data.MiningReputation += contract.reputation;
                            data.MiningContracts.Remove(contract.ContractId);
                        }
                        else
                        {
                            data.HaulingReputation += contract.reputation;
                            data.HaulingContracts.Remove(contract.ContractId);
                        }

                        switch (data.MiningReputation)
                        {
                            case >= 5000:
                                data.MiningReputation = 5000;
                                break;
                        }

                        switch (data.HaulingReputation)
                        {
                            case >= 5000:
                                data.HaulingReputation = 5000;
                                break;
                        }

                        switch (data.MiningReputation)
                        {
                            case >= 3000:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.05f);
                                break;
                            case >= 2000:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;
                            case >= 1000:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;
                            case >= 750:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;
                            case >= 500:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;
                            case >= 250:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;
                            case >= 100:
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                                break;

                        }

                        if (contract.DistanceBonus > 0)
                        {
                            contract.contractPrice += contract.DistanceBonus;
                        }

                        if (AlliancePluginEnabled)
                        {
                            //patch into alliances and process the payment there
                            //contract.AmountPaid = contract.contractPrice;
                            try
                            {
                                object[] MethodInput = new object[]
                                {
                                    player.Id.SteamId, contract.contractPrice, "Mining",
                                    controller.CubeGrid.PositionComp.GetPosition()
                                };
                                contract.contractPrice = (long)AllianceTaxes?.Invoke(null, MethodInput);

                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }

                        if (contract.DoRareItemReward)
                        {
                            foreach (RewardItem item in contract.PlayerLoot)
                            {
                                if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                                        out MyDefinitionId reward) && item.Enabled &&
                                    rep >= item.ReputationRequired)
                                {
                                    //  Log.Info("Tried to do ");
                                    Random rand = new Random();
                                    int amount = rand.Next(item.ItemMinAmount, item.ItemMaxAmount);
                                    double chance = rand.NextDouble();
                                    if (chance <= item.chance)
                                    {
                                        if (SpawnLoot(controller.CubeGrid, reward, (MyFixedPoint)amount))
                                        {
                                            contract.GivenItemReward = true;
                                            SendMessage("Boss Dave",
                                                "Heres a bonus for a job well done " + amount + " " +
                                                reward.ToString().Replace("MyObjectBuilder_", ""), Color.Gold,
                                                (long)player.Id.SteamId);
                                        }
                                    }
                                }
                            }
                        }

                        //  BoundingSphereD sphere = new BoundingSphereD(coords, 400);
                        MyCubeGrid grid =
                            MyAPIGateway.Entities.GetEntityById(contract.StationEntityId) as MyCubeGrid;
                        if (grid != null)
                        {

                            foreach (RewardItem item in contract.PutInStation)
                            {
                                if (item.Enabled && rep >= item.ReputationRequired)
                                {
                                    Random random = new Random();
                                    if (random.NextDouble() <= item.chance)
                                    {
                                        if (MyDefinitionId.TryParse(
                                                "MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                                                out MyDefinitionId newid))
                                        {
                                            int amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                                            Stations station = new Stations();
                                            station.CargoName = contract.CargoName;
                                            station.OwnerFactionTag =
                                                FacUtils.GetFactionTag(FacUtils.GetOwner(grid));
                                            station.ViewOnlyNamedCargo = true;
                                            SpawnItems(grid, newid, amount, station);
                                        }
                                    }
                                }
                            }

                            if (contract.PutTheHaulInStation)
                            {
                                foreach (KeyValuePair<MyDefinitionId, int> pair in itemsToRemove)
                                {
                                    Stations station = new Stations();
                                    station.CargoName = contract.CargoName;
                                    station.OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid));
                                    station.ViewOnlyNamedCargo = true;
                                    SpawnItems(grid, pair.Key, pair.Value, station);
                                }
                            }

                        }
                        else
                        {
                            Log.Error("Couldnt find station to put items in! Did it get cut and pasted? at " +
                                      coords.ToString());
                        }

                        contract.AmountPaid = contract.contractPrice;
                        contract.TimeCompleted = DateTime.Now;
                        EconUtils.addMoney(player.Identity.IdentityId, contract.contractPrice);


                        contract.PlayerSteamId = player.Id.SteamId;



                        FileUtils utils = new FileUtils();
                        contract.status = ContractStatus.Completed;
                        File.Delete(path + "//PlayerData//Mining//InProgress//" + contract.ContractId + ".xml");
                        CrunchEconCore.ContractSave.Remove(contract.ContractId);
                        CrunchEconCore.ContractSave.Add(contract.ContractId, contract);

                        playerData[player.Id.SteamId] = data;

                        return true;

                        //SAVE THE PLAYER DATA WITH INCREASED REPUTATION
                    }
                }
            }

            return false;

        }


        public void DoContractDelivery(MyPlayer player, bool DoNewContract)
        {
            if (config.MiningContractsEnabled)
            {
                if (DoNewContract)
                {

                    try
                    {

                        if (playerData.TryGetValue(player.Id.SteamId, out PlayerData data))
                        {

                            if (DateTime.Now >= data.NextDaveMessage)
                            {
                                SendMessage("Boss Dave", "Check contracts with !contract info", Color.Gold, (long)player.Id.SteamId);
                                data.NextDaveMessage = DateTime.Now.AddMinutes(config.MinutesBetweenDave);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error("Probably a null contract " + ex.ToString());


                    }

                }
            }
            if (player.GetPosition() != null)
            {

                // GenerateNewMiningContracts(player);

                try
                {
                    if (config.MiningContractsEnabled)
                    {
                        if (playerData.TryGetValue(player.Id.SteamId, out PlayerData data))
                        {
                            MyPlayer playerOnline = player;
                            if (player.Character != null && player?.Controller.ControlledEntity is MyCockpit controller)
                            {
                                MyCubeGrid grid = controller.CubeGrid;
                                List<Contract> delete = new List<Contract>();

                                foreach (Contract contract in data.getMiningContracts().Values)
                                {
                                    try
                                    {
                                        if (HandleDeliver(contract, player, data, controller))
                                        {
                                            delete.Add(contract);

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.ToString());
                                        delete.Add(contract);
                                    }
                                }
                                foreach (Contract contract in data.getHaulingContracts().Values)
                                {
                                    try
                                    {
                                        if (HandleDeliver(contract, player, data, controller))
                                        {
                                            delete.Add(contract);

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.ToString());
                                        delete.Add(contract);
                                    }
                                }
                                foreach (Contract contract in delete)
                                {
                                    if (contract.type == ContractType.Mining)
                                    {
                                        data.getMiningContracts().Remove(contract.ContractId);
                                        data.MiningContracts.Remove(contract.ContractId);
                                    }
                                    else
                                    {
                                        data.getHaulingContracts().Remove(contract.ContractId);
                                        data.MiningContracts.Remove(contract.ContractId);
                                    }

                                }
                                playerData[player.Id.SteamId] = data;
                                try
                                {
                                    utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("WHY YOU DO THIS?");

                                }
                            }
                        }
                    }
                }

                catch (Exception ex)
                {

                    Log.Error(ex);
                }
            }
        }


        public static Dictionary<ulong, DateTime> playerSurveyTimes = new Dictionary<ulong, DateTime>();
        public static Dictionary<ulong, DateTime> MessageCooldowns = new Dictionary<ulong, DateTime>();

        public static Dictionary<long, DateTime> individualTimers = new Dictionary<long, DateTime>();

        public static string GetPlayerName(ulong steamId)
        {
            MyIdentity id = GetIdentityByNameOrId(steamId.ToString());
            if (id != null && id.DisplayName != null)
            {
                return id.DisplayName;
            }
            else
            {
                return steamId.ToString();
            }
        }
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
        public static async void DoStationShit()
        {
            Log.Info("Redoing station whitelists");
            await Task.Run(() =>
            {
                foreach (Stations station in stations)
                {

                    if (station.WhitelistedSafezones)
                    {

                        BoundingSphereD sphere = new BoundingSphereD(station.getGPS().Coords, 200);

                        foreach (MySafeZone zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                        {
                            zone.Factions.Clear();
                            if (station.DoBlacklist)
                            {
                                zone.AccessTypeFactions = MySafeZoneAccess.Blacklist;
                            }
                            else
                            {
                                zone.AccessTypeFactions = MySafeZoneAccess.Whitelist;
                            }

                            foreach (String s in station.Whitelist)
                            {
                                if (s.Contains("LIST:"))
                                {
                                    //split the list
                                    //     Log.Info("Is list");
                                    String temp = s.Split(':')[1];
                                    //    Log.Info(temp);
                                    foreach (Whitelist list in whitelist.whitelist)
                                    {
                                        //        Log.Info("Checking the lists");
                                        //       Log.Info(list.ListName);
                                        if (list.ListName == temp)
                                        {
                                            //       Log.Info("its the right name");
                                            foreach (String tag in list.FactionTags)
                                            {
                                                //     Log.Info("looping through the tags " + tag);
                                                if (MySession.Static.Factions.TryGetFactionByTag(tag) != null)
                                                {
                                                    //        Log.Info("fac isnt null");
                                                    zone.Factions.Add(MySession.Static.Factions.TryGetFactionByTag(tag));
                                                }
                                            }
                                        }

                                    }
                                }
                                else if (s.Contains("FAC:"))
                                {
                                    String temp = s.Split(':')[1];
                                    if (MySession.Static.Factions.TryGetFactionByTag(temp) != null)
                                    {
                                        zone.Factions.Add(MySession.Static.Factions.TryGetFactionByTag(temp));
                                    }

                                }
                            }
                            MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                        }

                    }
                }
            });
        }
        public static Random rnd = new Random();

        public static DateTime NextBalanceUpdate = DateTime.Now;

        public override async void Update()
        {
            if (ticks == 0)
            {
             //   UIHandler.SetupContext(config.DBConnectionString);

            }
            try
            {
                ticks++;
                if (paused)
                {
                    return;
                }

                if (CrunchEconCore.config == null)
                {
                    return;
                }

                if (!CrunchEconCore.config.PluginEnabled)
                {
                    return;
                }
                if (config.DoWebUI)
                {
                    if (DateTime.Now >= NextBalanceUpdate)
                    {
                        NextBalanceUpdate = DateTime.Now.AddSeconds(config.SecondsBetweenEventChecks);
                        //try
                        //{
                        // //   Task.Run(async () => { UIHandler.Handle(); });
                        //}
                        //catch (Exception e)
                        //{
                        //    Log.Error("Error in UI handler", e.ToString());
                        //}
                    }
                }
                if (DateTime.Now >= NextFileRefresh)
                {
                    NextFileRefresh = DateTime.Now.AddMinutes(15);
                    Log.Info("Loading stuff for CrunchEcon");
                    try
                    {
                        DoStationShit();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading Stations " + ex.ToString());
                    }

                }

                if (ticks % 256 == 0 && TorchState == TorchSessionState.Loaded)
                {

                    foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
                    {
                        if (config.MiningContractsEnabled || config.HaulingContractsEnabled)
                        {
                            if (DateTime.Now >= ContractUtils.chat)
                            {
                                ContractUtils.chat = DateTime.Now.AddMinutes(10);
                                DoContractDelivery(player, true);
                            }
                            else
                            {
                                DoContractDelivery(player, false);
                            }
                        }
                    }

                }

                if (ticks % 64 == 0 && TorchState == TorchSessionState.Loaded)
                {

                    string type = "//Mining";
                    foreach (KeyValuePair<Guid, Contract> keys in ContractSave)
                    {
                        //  utils.SaveMiningData(AlliancePlugin.path + "//MiningStuff//PlayerData//" + keys.Key + ".xml", keys.Value);i
                        Contract contract = keys.Value;
                        if (contract.type == ContractType.Mining)
                        {
                            type = "//Mining";
                        }

                        if (contract.type == ContractType.Hauling)
                        {
                            type = "//Hauling";
                        }

                        switch (contract.status)
                        {
                            case ContractStatus.InProgress:
                                utils.WriteToXmlFile<Contract>(
                                    path + "//PlayerData//" + type + "//InProgress//" + contract.ContractId + ".xml",
                                    keys.Value);
                                break;
                            case ContractStatus.Completed:
                                utils.WriteToXmlFile<Contract>(
                                    path + "//PlayerData//" + type + "//Completed//" + contract.ContractId + ".xml",
                                    keys.Value);
                                break;
                            case ContractStatus.Failed:
                                utils.WriteToXmlFile<Contract>(
                                    path + "//PlayerData//" + type + "//Failed//" + contract.ContractId + ".xml",
                                    keys.Value);
                                break;
                        }

                    }

                    ContractSave.Clear();

                    foreach (KeyValuePair<Guid, SurveyMission> keys in SurveySave)
                    {
                        //  utils.SaveMiningData(AlliancePlugin.path + "//MiningStuff//PlayerData//" + keys.Key + ".xml", keys.Value);i
                        SurveyMission contract = keys.Value;
                        type = "//Survey";

                        switch (contract.status)
                        {
                            case ContractStatus.InProgress:
                                utils.WriteToXmlFile<SurveyMission>(
                                    path + "//PlayerData//" + type + "//InProgress//" + contract.id + ".xml",
                                    keys.Value);
                                break;
                            case ContractStatus.Completed:
                                utils.WriteToXmlFile<SurveyMission>(
                                    path + "//PlayerData//" + type + "//Completed//" + contract.id + ".xml",
                                    keys.Value);
                                break;
                            case ContractStatus.Failed:
                                utils.WriteToXmlFile<SurveyMission>(
                                    path + "//PlayerData//" + type + "//Failed//" + contract.id + ".xml", keys.Value);
                                break;
                        }

                    }

                    SurveySave.Clear();
                    DateTime now = DateTime.Now;
                    foreach (Stations station in stations)
                    {
                        bool doneMoney = false;
                        //first check if its any, then we can load the grid to do the editing
                        try
                        {
                            if (now >= station.nextCraftRefresh && station.EnableStationCrafting)
                            {
                                if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) != null)
                                {
                                    if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is MyCubeGrid grid)
                                    {
                                        List<VRage.Game.ModAPI.IMyInventory> inventories =
                                            new List<VRage.Game.ModAPI.IMyInventory>();
                                        foreach (CraftedItem item in station.CraftableItems.Where(x => x.Enabled))
                                        {
                                            bool skip = false;
                                            if (item.OnlyCraftIfStationSellsThisItem)
                                            {
                                                foreach (MyStoreBlock store in grid.GetFatBlocks()
                                                             .OfType<MyStoreBlock>())
                                                {
                                                    if (!store.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                                                        continue;
                                                    if (!sellOffers.TryGetValue(store.DisplayNameText,
                                                            out List<SellOffer> offers)) continue;
                                                    if (offers.All(x =>
                                                            x.typeId != item.typeid &&
                                                            x.subtypeId != item.subtypeid))
                                                    {
                                                        skip = true;
                                                    }
                                                }
                                            }

                                            if (skip) continue;
                                            double yeet = rnd.NextDouble();
                                            if (!(yeet <= item.chanceToCraft)) continue;
                                            var comps = new Dictionary<MyDefinitionId, int>();
                                            inventories.AddRange(GetInventories(grid, station));
                                            if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.typeid,
                                                    item.subtypeid,
                                                    out MyDefinitionId id)) continue;
                                            foreach (RecipeItem recipe in item.RequriedItems)
                                            {
                                                if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.typeid,
                                                        recipe.subtypeid, out MyDefinitionId id2))
                                                {
                                                    comps.Add(id2, recipe.amount);
                                                }
                                            }

                                            if (!ConsumeComponents(inventories, comps, 0l)) continue;
                                            SpawnItems(grid, id, item.amountPerCraft, station);
                                            comps.Clear();
                                            inventories.Clear();
                                        }
                                    }
                                }

                                station.nextCraftRefresh = DateTime.Now.AddSeconds(station.SecondsBetweenCrafting);
                            }

                            if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh)
                            {
                                MyGps gps = station.getGPS();
                                if (gps == null)
                                {
                                    continue;
                                }

                                Boolean AddSellTime = false;
                                Boolean AddBuyTime = false;
                                bool checkLocation = false;
                                if (!station.WorldName.Equals("default"))
                                {
                                    if (station.StationEntityId > 0)
                                    {
                                        if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) != null)
                                        {
                                            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is
                                                MyCubeGrid grid)
                                            {
                                                if (station.DoPeriodGridClearing &&
                                                    now >= station.nextGridInventoryClear)
                                                {
                                                    station.nextGridInventoryClear =
                                                        now.AddSeconds(station.SecondsBetweenClearEntireInventory);
                                                    ClearInventories(grid, station);
                                                    SaveStation(station);
                                                }

                                                if (station.Modifiers != null)
                                                {
                                                    foreach (var modifier in station.Modifiers.Where(x =>
                                                                 x.Enabled && now >= x.NextRefresh))
                                                    {
                                                        modifier.NextRefresh =
                                                            now.AddSeconds(modifier.SecondsBetweenRefreshes);
                                                        modifier.Modifier =
                                                            (float)Math.Round(
                                                                (rnd.NextDouble() * (modifier.Max - modifier.Min) +
                                                                 modifier.Min), 2);
                                                    }
                                                }

                                                foreach (MyStoreBlock store in grid.GetFatBlocks()
                                                             .OfType<MyStoreBlock>())
                                                {
                                                    //  Log.Info(store.DisplayNameText);
                                                    if (store.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                                                    {
                                                        if (station.MaintainNPCBalance && !doneMoney)
                                                        {
                                                            var balance = EconUtils.getBalance(store.OwnerId);
                                                            if (balance < station.MoneyToHave)
                                                            {
                                                                EconUtils.addMoney(store.OwnerId,
                                                                    station.MoneyToHave - balance);
                                                                doneMoney = true;
                                                            }
                                                            else
                                                            {
                                                                doneMoney = true;
                                                            }
                                                        }

                                                        //   Log.Info("1");
                                                        station.StationEntityId = store.CubeGrid.EntityId;
                                                        if (now >= station.nextSellRefresh && station.DoSellOffers)
                                                        {
                                                            //    Log.Info(store.DisplayNameText);
                                                            //    Log.Info("its past the timer");
                                                            AddSellTime = true;

                                                            if (sellOffers.TryGetValue(store.DisplayNameText,
                                                                    out List<SellOffer> offers))
                                                            {
                                                                //     Log.Info("it found the store files");

                                                                ClearStoreOfPlayersBuyingOffers(store);
                                                                List<VRage.Game.ModAPI.IMyInventory> inventories =
                                                                    new List<VRage.Game.ModAPI.IMyInventory>();
                                                                inventories.AddRange(GetInventories(grid, station));

                                                                //  Log.Info("now its checking offers");
                                                                foreach (SellOffer offer in offers)
                                                                {
                                                                    if (offer.OnlySellIfStationCraftsThis &&
                                                                        station.EnableStationCrafting)
                                                                    {
                                                                        if (station.CraftableItems.Where(x => x.Enabled)
                                                                            .All(x =>
                                                                                x.typeid != offer.typeId &&
                                                                                x.subtypeid != offer.subtypeId))
                                                                        {
                                                                            continue;
                                                                        }
                                                                    }

                                                                    try
                                                                    {
                                                                        double chance = rnd.NextDouble();

                                                                        //   Log.Info("Should be adding something to sell?");

                                                                        if (MyDefinitionId.TryParse(
                                                                                "MyObjectBuilder_" + offer.typeId,
                                                                                offer.subtypeId, out MyDefinitionId id))
                                                                        {

                                                                            int hasAmount =
                                                                                CountComponents(inventories, id)
                                                                                    .ToIntSafe();
                                                                            //  Log.Info("AMOUNT " + hasAmount + " " + id.ToString() + " " + station.Name);
                                                                            if (hasAmount > 0)
                                                                            {
                                                                                if (hasAmount <
                                                                                 offer.SpawnIfCargoLessThan &&
                                                                                 offer.SpawnItemsIfNeeded)
                                                                                {
                                                                                    int amountSpawned = 0;

                                                                                    amountSpawned =
                                                                                        rnd.Next(offer.minAmountToSpawn,
                                                                                            offer.maxAmountToSpawn);
                                                                                    if (offer.IndividualRefreshTimer)
                                                                                    {
                                                                                        if (individualTimers
                                                                                         .TryGetValue(
                                                                                             store.EntityId,
                                                                                             out DateTime refresh))
                                                                                        {
                                                                                            if (now >= refresh)
                                                                                            {
                                                                                                individualTimers[
                                                                                                        store
                                                                                                            .EntityId] =
                                                                                                    DateTime.Now
                                                                                                        .AddSeconds(
                                                                                                            offer
                                                                                                                .SecondsBetweenRefresh);


                                                                                                if (chance <= offer
                                                                                                    .chance)
                                                                                                {
                                                                                                    SpawnItems(grid, id,
                                                                                                        (MyFixedPoint)
                                                                                                        amountSpawned,
                                                                                                        station);
                                                                                                    hasAmount +=
                                                                                                        amountSpawned;
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                continue;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            individualTimers.Add(
                                                                                                store.EntityId,
                                                                                                DateTime.Now.AddSeconds(
                                                                                                    offer
                                                                                                        .SecondsBetweenRefresh));



                                                                                            if (chance <= offer.chance)
                                                                                            {
                                                                                                SpawnItems(grid, id,
                                                                                                    (MyFixedPoint)
                                                                                                    amountSpawned,
                                                                                                    station);
                                                                                                hasAmount +=
                                                                                                    amountSpawned;
                                                                                            }
                                                                                        }


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        //spawn items
                                                                                        if (chance <= offer.chance)
                                                                                        {
                                                                                            SpawnItems(grid, id,
                                                                                                (MyFixedPoint)
                                                                                                amountSpawned, station);
                                                                                            hasAmount += amountSpawned;

                                                                                        }
                                                                                    }

                                                                                }

                                                                                SerializableDefinitionId itemId =
                                                                                    new SerializableDefinitionId(
                                                                                        id.TypeId, offer.subtypeId);



                                                                                int price = rnd.Next(
                                                                                    (int)offer.minPrice,
                                                                                    (int)offer.maxPrice);
                                                                                price = Convert.ToInt32(price *
                                                                                    station.GetModifier(offer
                                                                                        .StationModifierItemName));
                                                                                MyStoreItemData item =
                                                                                    new MyStoreItemData(itemId,
                                                                                        hasAmount, price, null, null);
                                                                                //       Log.Info("if it got here its creating the offer");
                                                                                MyStoreInsertResults result =
                                                                                    store.InsertOffer(item,
                                                                                        out long notUsingThis);



                                                                                if (result == MyStoreInsertResults
                                                                                     .Fail_PricePerUnitIsLessThanMinimum ||
                                                                                 result == MyStoreInsertResults
                                                                                     .Fail_StoreLimitReached ||
                                                                                 result == MyStoreInsertResults
                                                                                     .Error)
                                                                                {
                                                                                    Log.Error(
                                                                                        "Unable to insert this offer into store " +
                                                                                        offer.typeId + " " +
                                                                                        offer.subtypeId +
                                                                                        " at station " + station.Name +
                                                                                        " " + result.ToString());
                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                if (offer.SpawnItemsIfNeeded)
                                                                                {
                                                                                    int amountSpawned = 0;

                                                                                    amountSpawned =
                                                                                        rnd.Next(offer.minAmountToSpawn,
                                                                                            offer.maxAmountToSpawn);
                                                                                    if (offer.IndividualRefreshTimer)
                                                                                    {
                                                                                        if (individualTimers
                                                                                         .TryGetValue(
                                                                                             store.EntityId,
                                                                                             out DateTime refresh))
                                                                                        {
                                                                                            if (now >= refresh)
                                                                                            {
                                                                                                individualTimers[
                                                                                                        store
                                                                                                            .EntityId] =
                                                                                                    DateTime.Now
                                                                                                        .AddSeconds(
                                                                                                            offer
                                                                                                                .SecondsBetweenRefresh);


                                                                                                if (chance <= offer
                                                                                                    .chance)
                                                                                                {
                                                                                                    SpawnItems(grid, id,
                                                                                                        (MyFixedPoint)
                                                                                                        amountSpawned,
                                                                                                        station);
                                                                                                    hasAmount +=
                                                                                                        amountSpawned;
                                                                                                }
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                continue;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            individualTimers.Add(
                                                                                                store.EntityId,
                                                                                                DateTime.Now.AddSeconds(
                                                                                                    offer
                                                                                                        .SecondsBetweenRefresh));



                                                                                            if (chance <= offer.chance)
                                                                                            {
                                                                                                SpawnItems(grid, id,
                                                                                                    (MyFixedPoint)
                                                                                                    amountSpawned,
                                                                                                    station);
                                                                                                hasAmount +=
                                                                                                    amountSpawned;
                                                                                            }
                                                                                        }

                                                                                    }
                                                                                    else
                                                                                    {


                                                                                        //spawn items
                                                                                        SpawnItems(grid, id,
                                                                                            (MyFixedPoint)offer
                                                                                                .SpawnIfCargoLessThan,
                                                                                            station);
                                                                                    }


                                                                                    SerializableDefinitionId itemId =
                                                                                        new SerializableDefinitionId(
                                                                                            id.TypeId, offer.subtypeId);




                                                                                    int price = rnd.Next(
                                                                                        (int)offer.minPrice,
                                                                                        (int)offer.maxPrice);
                                                                                    price = Convert.ToInt32(price *
                                                                                        station.GetModifier(offer
                                                                                            .StationModifierItemName));
                                                                                    MyStoreItemData item =
                                                                                        new MyStoreItemData(itemId,
                                                                                            offer.SpawnIfCargoLessThan,
                                                                                            price, null, null);
                                                                                    //    Log.Info("if it got here its creating the offer");
                                                                                    MyStoreInsertResults result =
                                                                                        store.InsertOffer(item,
                                                                                            out long notUsingThis);
                                                                                    if (result == MyStoreInsertResults
                                                                                         .Fail_PricePerUnitIsLessThanMinimum ||
                                                                                     result == MyStoreInsertResults
                                                                                         .Fail_StoreLimitReached ||
                                                                                     result == MyStoreInsertResults
                                                                                         .Error)
                                                                                    {
                                                                                        Log.Error(
                                                                                            "Unable to insert this offer into store " +
                                                                                            offer.typeId + " " +
                                                                                            offer.subtypeId +
                                                                                            " at station " +
                                                                                            station.Name + " " +
                                                                                            result.ToString());
                                                                                    }
                                                                                }
                                                                            }

                                                                        }
                                                                    }



                                                                    catch (Exception ex)
                                                                    {
                                                                        Log.Error("Error on this sell offer " +
                                                                            offer.typeId + " " + offer.subtypeId);
                                                                        Log.Error("for this station " + station.Name);
                                                                        station.nextSellRefresh =
                                                                            now.AddSeconds(station
                                                                                .SecondsBetweenRefreshForSellOffers);
                                                                        Log.Error(ex.ToString());
                                                                        SaveStation(station);
                                                                    }
                                                                }
                                                            }


                                                        }
                                                        else
                                                        {
                                                            //Log.Info("Not past the time for sell offers.");
                                                        }

                                                        if (now >= station.nextBuyRefresh && station.DoBuyOrders)
                                                        {
                                                            AddBuyTime = true;


                                                            if (buyOrders.TryGetValue(store.DisplayNameText,
                                                                    out List<BuyOrder> orders))
                                                            {

                                                                ClearStoreOfPlayersSellingOrders(store);
                                                                List<VRage.Game.ModAPI.IMyInventory> inventories =
                                                                    new List<VRage.Game.ModAPI.IMyInventory>();
                                                                inventories.AddRange(GetInventories(grid, station));

                                                                foreach (BuyOrder order in orders)
                                                                {

                                                                    try
                                                                    {
                                                                        if (order.IndividualRefreshTimer)
                                                                        {
                                                                            if (individualTimers.TryGetValue(
                                                                                    store.EntityId,
                                                                                    out DateTime refresh))
                                                                            {
                                                                                if (now < refresh)
                                                                                {
                                                                                    continue;
                                                                                }
                                                                                else
                                                                                {
                                                                                    individualTimers[store.EntityId] =
                                                                                        DateTime.Now.AddSeconds(
                                                                                            order
                                                                                                .SecondsBetweenRefresh);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                individualTimers.Add(store.EntityId,
                                                                                    DateTime.Now.AddSeconds(order
                                                                                        .SecondsBetweenRefresh));
                                                                            }
                                                                        }

                                                                        if (order.OnlyBuyIfStationDoesNotCraft &&
                                                                            station.EnableStationCrafting)
                                                                        {
                                                                            if (station.CraftableItems
                                                                                .Where(x => x.Enabled).Any(x =>
                                                                                    x.typeid == order.typeId &&
                                                                                    x.subtypeid == order.subtypeId))
                                                                            {
                                                                                continue;
                                                                            }
                                                                        }

                                                                        if (MyDefinitionId.TryParse(
                                                                                "MyObjectBuilder_" + order.typeId,
                                                                                order.subtypeId, out MyDefinitionId id))
                                                                        {

                                                                            SerializableDefinitionId itemId =
                                                                                new SerializableDefinitionId(id.TypeId,
                                                                                    order.subtypeId);

                                                                            MyStoreItemDataSimple simple =
                                                                                new MyStoreItemDataSimple();

                                                                            double chance = rnd.NextDouble();
                                                                            if (chance <= order.chance)
                                                                            {
                                                                                int price = rnd.Next(
                                                                                    (int)order.minPrice,
                                                                                    (int)order.maxPrice);
                                                                                price = Convert.ToInt32(price *
                                                                                    station.GetModifier(order
                                                                                        .StationModifierItemName));
                                                                                int amount =
                                                                                    rnd.Next((int)order.minAmount,
                                                                                        (int)order.maxAmount);
                                                                                MyStoreItemData item =
                                                                                    new MyStoreItemData(itemId, amount,
                                                                                        price, null, null);
                                                                                MyStoreInsertResults result =
                                                                                    store.InsertOrder(item,
                                                                                        out long notUsingThis);
                                                                                store.InsertOrder(simple,
                                                                                    out long dfdfd);
                                                                                if (result == MyStoreInsertResults
                                                                                     .Fail_PricePerUnitIsLessThanMinimum ||
                                                                                 result == MyStoreInsertResults
                                                                                     .Fail_StoreLimitReached ||
                                                                                 result == MyStoreInsertResults
                                                                                     .Error)
                                                                                {
                                                                                    Log.Error(
                                                                                        "Unable to insert this order into store " +
                                                                                        order.typeId + " " +
                                                                                        order.subtypeId +
                                                                                        " at station " + station.Name +
                                                                                        " " + result.ToString());
                                                                                }
                                                                            }
                                                                        }
                                                                    }


                                                                    catch (Exception ex)
                                                                    {

                                                                        Log.Error("Error on this buy order " +
                                                                            order.typeId + " " + order.subtypeId);
                                                                        Log.Error("for this station " + station.Name);
                                                                        station.nextBuyRefresh =
                                                                            now.AddSeconds(station
                                                                                .SecondsBetweenRefreshForBuyOrders);
                                                                        Log.Error(ex.ToString());
                                                                        SaveStation(station);
                                                                    }

                                                                }
                                                            }

                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                checkLocation = true;
                                            }
                                        }
                                        else
                                        {
                                            checkLocation = true;
                                        }
                                    }
                                    else
                                    {
                                        checkLocation = true;
                                    }
                                }
                                else
                                {
                                    checkLocation = true;
                                }

                                if (checkLocation)
                                {
                                    BoundingSphereD sphere = new BoundingSphereD(gps.Coords, 200);

                                    foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere)
                                                 .OfType<MyCubeGrid>())
                                    {
                                        if (station.DoPeriodGridClearing && now >= station.nextGridInventoryClear)
                                        {
                                            station.nextGridInventoryClear =
                                                now.AddSeconds(station.SecondsBetweenClearEntireInventory);
                                            ClearInventories(grid, station);
                                            SaveStation(station);
                                        }

                                        //   foreach (MyProgrammableBlock pb in grid.GetFatBlocks().OfType<MyProgrammableBlock>())
                                        //  {
                                        //    if (!pb.Enabled)
                                        //    {
                                        //        pb.Enabled = true;
                                        //        pb.SendRecompile();
                                        //     }
                                        //  }


                                        foreach (MyStoreBlock store in grid.GetFatBlocks().OfType<MyStoreBlock>())
                                        {
                                            //  Log.Info(store.DisplayNameText);
                                            if (store.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                                            {
                                                //   Log.Info("1");
                                                station.StationEntityId = store.CubeGrid.EntityId;
                                                station.WorldName = MyMultiplayer.Static.HostName;
                                                if (now >= station.nextSellRefresh && station.DoSellOffers)
                                                {
                                                    //    Log.Info(store.DisplayNameText);
                                                    //    Log.Info("its past the timer");
                                                    AddSellTime = true;

                                                    if (sellOffers.TryGetValue(store.DisplayNameText,
                                                            out List<SellOffer> offers))
                                                    {
                                                        //     Log.Info("it found the store files");

                                                        ClearStoreOfPlayersBuyingOffers(store);
                                                        List<VRage.Game.ModAPI.IMyInventory> inventories =
                                                            new List<VRage.Game.ModAPI.IMyInventory>();
                                                        inventories.AddRange(GetInventories(grid, station));

                                                        //  Log.Info("now its checking offers");

                                                        foreach (SellOffer offer in offers)
                                                        {
                                                            try
                                                            {

                                                                //   Log.Info("this is an offer");
                                                                double chance = rnd.NextDouble();
                                                                //    Log.Info("this is fucked");
                                                                //   Log.Info("Should be adding something to sell?");

                                                                if (MyDefinitionId.TryParse(
                                                                        "MyObjectBuilder_" + offer.typeId,
                                                                        offer.subtypeId, out MyDefinitionId id))
                                                                {

                                                                    int hasAmount = CountComponents(inventories, id)
                                                                        .ToIntSafe();
                                                                    //  Log.Info("AMOUNT " + hasAmount + " " + id.ToString() + " " + station.Name);
                                                                    if (hasAmount > 0)
                                                                    {
                                                                        if (hasAmount < offer.SpawnIfCargoLessThan &&
                                                                            offer.SpawnItemsIfNeeded)
                                                                        {
                                                                            int amountSpawned = 0;

                                                                            amountSpawned =
                                                                                rnd.Next(offer.minAmountToSpawn,
                                                                                    offer.maxAmountToSpawn);
                                                                            if (offer.IndividualRefreshTimer)
                                                                            {
                                                                                if (individualTimers.TryGetValue(
                                                                                    store.EntityId,
                                                                                    out DateTime refresh))
                                                                                {
                                                                                    if (now >= refresh)
                                                                                    {
                                                                                        individualTimers
                                                                                                [store.EntityId] =
                                                                                            DateTime.Now.AddSeconds(
                                                                                                offer
                                                                                                    .SecondsBetweenRefresh);


                                                                                        if (chance <= offer.chance)
                                                                                        {
                                                                                            SpawnItems(grid, id,
                                                                                                (MyFixedPoint)
                                                                                                amountSpawned, station);
                                                                                            hasAmount += amountSpawned;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        continue;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    individualTimers.Add(store.EntityId,
                                                                                        DateTime.Now.AddSeconds(
                                                                                            offer
                                                                                                .SecondsBetweenRefresh));



                                                                                    if (chance <= offer.chance)
                                                                                    {
                                                                                        SpawnItems(grid, id,
                                                                                            (MyFixedPoint)amountSpawned,
                                                                                            station);
                                                                                        hasAmount += amountSpawned;
                                                                                    }
                                                                                }


                                                                            }
                                                                            else
                                                                            {
                                                                                //spawn items
                                                                                if (chance <= offer.chance)
                                                                                {
                                                                                    SpawnItems(grid, id,
                                                                                        (MyFixedPoint)amountSpawned,
                                                                                        station);
                                                                                    hasAmount += amountSpawned;
                                                                                }
                                                                            }

                                                                        }

                                                                        SerializableDefinitionId itemId =
                                                                            new SerializableDefinitionId(id.TypeId,
                                                                                offer.subtypeId);



                                                                        int price = rnd.Next((int)offer.minPrice,
                                                                            (int)offer.maxPrice);
                                                                        price = Convert.ToInt32(price *
                                                                            station.GetModifier(
                                                                                offer.StationModifierItemName));
                                                                        MyStoreItemData item =
                                                                            new MyStoreItemData(itemId, hasAmount,
                                                                                price, null, null);
                                                                        //       Log.Info("if it got here its creating the offer");
                                                                        MyStoreInsertResults result =
                                                                            store.InsertOffer(item,
                                                                                out long notUsingThis);

                                                                        if (result == MyStoreInsertResults
                                                                                .Fail_PricePerUnitIsLessThanMinimum ||
                                                                            result == MyStoreInsertResults
                                                                                .Fail_StoreLimitReached ||
                                                                            result == MyStoreInsertResults.Error)
                                                                        {
                                                                            Log.Error(
                                                                                "Unable to insert this offer into store " +
                                                                                offer.typeId + " " + offer.subtypeId +
                                                                                " at station " + station.Name + " " +
                                                                                result.ToString());
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (offer.SpawnItemsIfNeeded)
                                                                        {
                                                                            int amountSpawned = 0;

                                                                            amountSpawned =
                                                                                rnd.Next(offer.minAmountToSpawn,
                                                                                    offer.maxAmountToSpawn);
                                                                            if (offer.IndividualRefreshTimer)
                                                                            {
                                                                                if (individualTimers.TryGetValue(
                                                                                    store.EntityId,
                                                                                    out DateTime refresh))
                                                                                {
                                                                                    if (now >= refresh)
                                                                                    {
                                                                                        individualTimers
                                                                                                [store.EntityId] =
                                                                                            DateTime.Now.AddSeconds(
                                                                                                offer
                                                                                                    .SecondsBetweenRefresh);


                                                                                        if (chance <= offer.chance)
                                                                                        {
                                                                                            SpawnItems(grid, id,
                                                                                                (MyFixedPoint)
                                                                                                amountSpawned, station);
                                                                                            hasAmount += amountSpawned;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        continue;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    individualTimers.Add(store.EntityId,
                                                                                        DateTime.Now.AddSeconds(
                                                                                            offer
                                                                                                .SecondsBetweenRefresh));



                                                                                    if (chance <= offer.chance)
                                                                                    {
                                                                                        SpawnItems(grid, id,
                                                                                            (MyFixedPoint)amountSpawned,
                                                                                            station);
                                                                                        hasAmount += amountSpawned;
                                                                                    }
                                                                                }

                                                                            }
                                                                            else
                                                                            {


                                                                                //spawn items
                                                                                SpawnItems(grid, id,
                                                                                    (MyFixedPoint)offer
                                                                                        .SpawnIfCargoLessThan, station);
                                                                            }


                                                                            SerializableDefinitionId itemId =
                                                                                new SerializableDefinitionId(id.TypeId,
                                                                                    offer.subtypeId);




                                                                            int price = rnd.Next((int)offer.minPrice,
                                                                                (int)offer.maxPrice);
                                                                            price = Convert.ToInt32(price *
                                                                                station.GetModifier(offer
                                                                                    .StationModifierItemName));
                                                                            MyStoreItemData item =
                                                                                new MyStoreItemData(itemId,
                                                                                    offer.SpawnIfCargoLessThan, price,
                                                                                    null, null);
                                                                            //    Log.Info("if it got here its creating the offer");
                                                                            MyStoreInsertResults result =
                                                                                store.InsertOffer(item,
                                                                                    out long notUsingThis);
                                                                            if (result == MyStoreInsertResults
                                                                                    .Fail_PricePerUnitIsLessThanMinimum ||
                                                                                result == MyStoreInsertResults
                                                                                    .Fail_StoreLimitReached ||
                                                                                result == MyStoreInsertResults.Error)
                                                                            {
                                                                                Log.Error(
                                                                                    "Unable to insert this offer into store " +
                                                                                    offer.typeId + " " +
                                                                                    offer.subtypeId + " at station " +
                                                                                    station.Name + " " +
                                                                                    result.ToString());
                                                                            }
                                                                        }
                                                                    }

                                                                }
                                                            }



                                                            catch (Exception ex)
                                                            {
                                                                Log.Error("Error on this sell offer " + offer.typeId +
                                                                          " " + offer.subtypeId);
                                                                Log.Error("for this station " + station.Name);
                                                                station.nextSellRefresh =
                                                                    now.AddSeconds(station
                                                                        .SecondsBetweenRefreshForSellOffers);
                                                                Log.Error(ex.ToString());
                                                                SaveStation(station);
                                                            }
                                                        }
                                                    }


                                                }
                                                else
                                                {
                                                    //Log.Info("Not past the time for sell offers.");
                                                }

                                                if (now >= station.nextBuyRefresh && station.DoBuyOrders)
                                                {
                                                    AddBuyTime = true;


                                                    if (buyOrders.TryGetValue(store.DisplayNameText,
                                                            out List<BuyOrder> orders))
                                                    {

                                                        ClearStoreOfPlayersSellingOrders(store);
                                                        List<VRage.Game.ModAPI.IMyInventory> inventories =
                                                            new List<VRage.Game.ModAPI.IMyInventory>();
                                                        inventories.AddRange(GetInventories(grid, station));

                                                        foreach (BuyOrder order in orders)
                                                        {

                                                            try
                                                            {
                                                                if (order.IndividualRefreshTimer)
                                                                {
                                                                    if (individualTimers.TryGetValue(store.EntityId,
                                                                            out DateTime refresh))
                                                                    {
                                                                        if (now < refresh)
                                                                        {
                                                                            continue;
                                                                        }
                                                                        else
                                                                        {
                                                                            individualTimers[store.EntityId] =
                                                                                DateTime.Now.AddSeconds(
                                                                                    order.SecondsBetweenRefresh);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        individualTimers.Add(store.EntityId,
                                                                            DateTime.Now.AddSeconds(
                                                                                order.SecondsBetweenRefresh));
                                                                    }
                                                                }

                                                                if (MyDefinitionId.TryParse(
                                                                        "MyObjectBuilder_" + order.typeId,
                                                                        order.subtypeId, out MyDefinitionId id))
                                                                {

                                                                    SerializableDefinitionId itemId =
                                                                        new SerializableDefinitionId(id.TypeId,
                                                                            order.subtypeId);


                                                                    double chance = rnd.NextDouble();
                                                                    if (chance <= order.chance)
                                                                    {
                                                                        int price = rnd.Next((int)order.minPrice,
                                                                            (int)order.maxPrice);
                                                                        price = Convert.ToInt32(price *
                                                                            station.GetModifier(
                                                                                order.StationModifierItemName));
                                                                        int amount = rnd.Next((int)order.minAmount,
                                                                            (int)order.maxAmount);
                                                                        MyStoreItemData item =
                                                                            new MyStoreItemData(itemId, amount, price,
                                                                                null, null);
                                                                        MyStoreInsertResults result =
                                                                            store.InsertOrder(item,
                                                                                out long notUsingThis);
                                                                        if (result == MyStoreInsertResults
                                                                                .Fail_PricePerUnitIsLessThanMinimum ||
                                                                            result == MyStoreInsertResults
                                                                                .Fail_StoreLimitReached ||
                                                                            result == MyStoreInsertResults.Error)
                                                                        {
                                                                            Log.Error(
                                                                                "Unable to insert this order into store " +
                                                                                order.typeId + " " + order.subtypeId +
                                                                                " at station " + station.Name + " " +
                                                                                result.ToString());
                                                                        }
                                                                    }
                                                                }
                                                            }


                                                            catch (Exception ex)
                                                            {

                                                                Log.Error("Error on this buy order " + order.typeId +
                                                                          " " + order.subtypeId);
                                                                Log.Error("for this station " + station.Name);
                                                                station.nextBuyRefresh =
                                                                    now.AddSeconds(station
                                                                        .SecondsBetweenRefreshForBuyOrders);
                                                                Log.Error(ex.ToString());
                                                                SaveStation(station);
                                                            }

                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }

                                bool save = false;
                                if (AddSellTime)
                                {
                                    station.nextSellRefresh =
                                        now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                                    save = true;
                                }

                                if (AddBuyTime)
                                {
                                    station.nextBuyRefresh = now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);
                                    save = true;
                                }

                                if (save)
                                {
                                    SaveStation(station);
                                }


                            }
                        }
                        catch (Exception ex)
                        {
                            SaveStation(station);
                            Log.Error("Error on this station " + station.Name);
                            Log.Error(ex.ToString());
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public void SaveStation(Stations station)
        {
            utils.WriteToXmlFile<Stations>(path + "//Stations//" + station.Name + ".xml", station);


        }
        public void ClearStoreOfPlayersSellingOrders(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Order)
                {

                    yeet.Add(item);
                }
            }
            foreach (MyStoreItem item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public void ClearStoreOfPlayersBuyingOffers(MyStoreBlock store)
        {

            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Offer)
                {
                    yeet.Add(item);
                }
            }
            foreach (MyStoreItem item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public static List<Stations> stations = new List<Stations>();

        public static Dictionary<String, List<BuyOrder>> buyOrders = new Dictionary<string, List<BuyOrder>>();
        public static Dictionary<String, List<SellOffer>> sellOffers = new Dictionary<string, List<SellOffer>>();
        public static FileUtils utils = new FileUtils();
        public static void LoadAllStations()
        {
            stations.Clear();
            foreach (String s in Directory.GetFiles(path + "//Stations//"))
            {


                try
                {
                    Stations stat = utils.ReadFromXmlFile<Stations>(s);
                    if (stat.Enabled)
                    {
                        stat.SetupModifiers();
                        if (!stat.WorldName.Equals("default"))
                        {
                            if (stat.WorldName.Equals(MyMultiplayer.Static.HostName))
                            {
                                stations.Add(stat);
                            }
                        }
                        else
                        {
                            stations.Add(stat);
                        }


                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                }

            }
        }
        public static void LoadAllBuyOrders()
        {
            buyOrders.Clear();
            foreach (String s in Directory.GetDirectories(path + "//BuyOrders//"))
            {
                String temp = new DirectoryInfo(s).Name;

                List<BuyOrder> temporaryList = new List<BuyOrder>();
                foreach (String s2 in Directory.GetFiles(s))
                {
                    try
                    {
                        BuyOrder order = utils.ReadFromXmlFile<BuyOrder>(s2);
                        if (order.Enabled)
                        {
                            if (order.IndividualRefreshTimer)
                            {
                                order.path = s2;
                            }
                            temporaryList.Add(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading buy orders " + s2 + " " + ex.ToString());

                    }
                }
                buyOrders.Add(temp, temporaryList);
            }

        }

        public static void LoadAllGridSales()
        {
            gridsForSale.Clear();

            foreach (String s2 in Directory.GetFiles(path + "//GridSelling//"))
            {
                try
                {
                    GridSale sale = utils.ReadFromXmlFile<GridSale>(s2);
                    if (sale.Enabled && !gridsForSale.ContainsKey(sale.ItemSubTypeId))
                    {
                        gridsForSale.Add(sale.ItemSubTypeId, sale);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading sell offers " + s2 + " " + ex.ToString());

                }
            }
        }

        public static void LoadAllSellOffers()
        {
            sellOffers.Clear();
            foreach (String s in Directory.GetDirectories(path + "//SellOffers//"))
            {
                String temp = new DirectoryInfo(s).Name;
                List<SellOffer> temporaryList = new List<SellOffer>();
                foreach (String s2 in Directory.GetFiles(s))
                {
                    try
                    {
                        SellOffer order = utils.ReadFromXmlFile<SellOffer>(s2);
                        if (order.Enabled)
                        {
                            if (order.IndividualRefreshTimer)
                            {
                                order.path = s2;
                            }
                            temporaryList.Add(order);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading sell offers " + s2 + " " + ex.ToString());

                    }
                }
                sellOffers.Add(temp, temporaryList);
            }

        }

        public static MyGps ParseGPS(string input, string desc = null)
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
                    ShowOnHud = true
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }
        public static Dictionary<String, GridSale> gridsForSale = new Dictionary<string, GridSale>();


        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            TorchState = state;
            if (!CrunchEconCore.config.PluginEnabled && config != null)
            {
                return;
            }
            if (state == TorchSessionState.Loaded)
            {

                //
                string type = "//Mining";
                foreach (KeyValuePair<Guid, Contract> keys in ContractSave)
                {
                    //  utils.SaveMiningData(AlliancePlugin.path + "//MiningStuff//PlayerData//" + keys.Key + ".xml", keys.Value);i
                    Contract contract = keys.Value;
                    if (contract.type == ContractType.Mining)
                    {
                        type = "//Mining";
                    }
                    if (contract.type == ContractType.Hauling)
                    {
                        type = "//Hauling";
                    }
                    switch (contract.status)
                    {
                        case ContractStatus.InProgress:
                            utils.WriteToXmlFile<Contract>(path + "//PlayerData//" + type + "//InProgress//" + contract.ContractId + ".xml", keys.Value);
                            break;
                        case ContractStatus.Completed:
                            utils.WriteToXmlFile<Contract>(path + "//PlayerData//" + type + "//Completed//" + contract.ContractId + ".xml", keys.Value);
                            break;
                        case ContractStatus.Failed:
                            utils.WriteToXmlFile<Contract>(path + "//PlayerData//" + type + "//Failed//" + contract.ContractId + ".xml", keys.Value);
                            break;
                    }

                }
                foreach (KeyValuePair<Guid, SurveyMission> keys in SurveySave)
                {
                    //  utils.SaveMiningData(AlliancePlugin.path + "//MiningStuff//PlayerData//" + keys.Key + ".xml", keys.Value);i
                    SurveyMission contract = keys.Value;
                    type = "//Survey";

                    switch (contract.status)
                    {
                        case ContractStatus.InProgress:
                            utils.WriteToXmlFile<SurveyMission>(path + "//PlayerData//" + type + "//InProgress//" + contract.id + ".xml", keys.Value);
                            break;
                        case ContractStatus.Completed:
                            utils.WriteToXmlFile<SurveyMission>(path + "//PlayerData//" + type + "//Completed//" + contract.id + ".xml", keys.Value);
                            break;
                        case ContractStatus.Failed:
                            utils.WriteToXmlFile<SurveyMission>(path + "//PlayerData//" + type + "//Failed//" + contract.id + ".xml", keys.Value);
                            break;
                    }

                }
            }


            if (state == TorchSessionState.Loaded)
            {
                if (config.SetMinPricesTo1)
                {
                    sessionManager.AddOverrideMod(2825413709);
                    foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
                    {

                        if ((def as MyComponentDefinition) != null)
                        {
                            (def as MyComponentDefinition).MinimalPricePerUnit = 1;
                        }
                        if ((def as MyPhysicalItemDefinition) != null)
                        {
                            (def as MyPhysicalItemDefinition).MinimalPricePerUnit = 1;
                        }
                    }
                }
                session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += Login;

                if (session.Managers.GetManager<PluginManager>().Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"), out ITorchPlugin All))
                {
                    Type alli = All.GetType().Assembly.GetType("AlliancesPlugin.AlliancePlugin");
                    try
                    {
                        AllianceTaxes = All.GetType().GetMethod("AddToTaxes", BindingFlags.Public | BindingFlags.Static, null, new Type[4] { typeof(ulong), typeof(long), typeof(string), typeof(Vector3D) }, null);
                        //    BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Shits fucked");

                    }
                    Alliance = All;
                    AlliancePluginEnabled = true;
                }

                try
                {
                    ContractUtils.LoadAllContracts();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                try
                {
                    LoadAllGridSales();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading grid sales " + ex.ToString());


                }
                try
                {
                    LoadAllSellOffers();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Sell offers " + ex.ToString());


                }
                try
                {
                    LoadAllStations();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Stations " + ex.ToString());


                }
                try
                {
                    LoadAllBuyOrders();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Buy Orders " + ex.ToString());


                }
            }
        }
        public static RepConfig repConfig;
        public static WhitelistFile whitelist;
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            if (!CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (!Directory.Exists(path + "//Logs//"))
            {
                Directory.CreateDirectory(path + "//Logs//");
            }
            if (File.Exists(path + "\\Whitelist.xml"))
            {
                whitelist = utils.ReadFromXmlFile<WhitelistFile>(path + "\\Whitelist.xml");
                utils.WriteToXmlFile<WhitelistFile>(path + "\\Whitelist.xml", whitelist, false);
            }
            else
            {
                whitelist = new WhitelistFile();
                Whitelist temp = new Whitelist();
                temp.FactionTags.Add("BOB");
                temp.ListName = "LIST1";

                whitelist.whitelist.Add(temp);
                Whitelist temp2 = new Whitelist();
                temp2.FactionTags.Add("CAR");
                temp2.FactionTags.Add("BOB");
                temp2.ListName = "LIST2";
                whitelist.whitelist.Add(temp2);
                utils.WriteToXmlFile<WhitelistFile>(path + "\\Whitelist.xml", whitelist, false);
            }
            if (File.Exists(path + "\\ReputationConfig.xml"))
            {
                repConfig = utils.ReadFromXmlFile<RepConfig>(path + "\\ReputationConfig.xml");
                utils.WriteToXmlFile<RepConfig>(path + "\\ReputationConfig.xml", repConfig, false);
            }
            else
            {
                repConfig = new RepConfig();
                RepItem item = new RepItem();

                repConfig.RepConfigs.Add(item);
                utils.WriteToXmlFile<RepConfig>(path + "\\ReputationConfig.xml", repConfig, false);
            }
            if (!Directory.Exists(path + "//Stations//"))
            {
                Directory.CreateDirectory(path + "//Stations//");
                Stations station = new Stations();
                station.Enabled = false;
                PriceModifier modifier = new PriceModifier();
                station.Modifiers.Add(modifier);
                station.Whitelist.Add("FAC:BOB");
                station.Whitelist.Add("LIST:LIST1");
                CraftedItem item = new CraftedItem();
                item.typeid = "Ore";
                item.subtypeid = "Iron";
                item.amountPerCraft = 500;
                item.chanceToCraft = 1;

                RecipeItem recipe = new RecipeItem();
                recipe.typeid = "Ore";
                recipe.subtypeid = "Stone";
                recipe.amount = 500;

                item.RequriedItems.Add(recipe);
                station.CraftableItems.Add(item);
                utils.WriteToXmlFile<Stations>(path + "//Stations//Example.xml", station);
            }
            if (!Directory.Exists(path + "//BuyOrders//Example//"))
            {
                Directory.CreateDirectory(path + "//BuyOrders//Example//");
                BuyOrder example = new BuyOrder();
                utils.WriteToXmlFile<BuyOrder>(path + "//BuyOrders//Example//Example.xml", example);

            }
            if (!Directory.Exists(path + "//SellOffers//Example//"))
            {
                Directory.CreateDirectory(path + "//SellOffers//Example//");
                SellOffer example = new SellOffer();
                var gps = "put a gps string here";
                example.gpsToPickFrom.Add(gps);
                utils.WriteToXmlFile<SellOffer>(path + "//SellOffers//Example//Example.xml", example);
            }
            if (!Directory.Exists(path + "//GridSelling//"))
            {
                GridSale gridSale = new GridSale();

                Directory.CreateDirectory(path + "//GridSelling//");
                utils.WriteToXmlFile<GridSale>(path + "//GridSelling//ExampleSale.xml", gridSale);
            }
            if (!Directory.Exists(path + "//GridSelling//Grids//"))
            {
                Directory.CreateDirectory(path + "//GridSelling//Grids//");
            }

            if (!Directory.Exists(path + "//ContractConfigs//Survey//"))
            {
                SurveyMission mission = new SurveyMission();
                mission.configs.Add(new SurveyStage());
                Directory.CreateDirectory(path + "//ContractConfigs//Survey//");
                utils.WriteToXmlFile<SurveyMission>(path + "//ContractConfigs//Survey//Example1.xml", mission);
                mission.configs.Add(new SurveyStage());
                utils.WriteToXmlFile<SurveyMission>(path + "//ContractConfigs//Survey//Example2.xml", mission);
                mission.configs.Add(new SurveyStage());
                utils.WriteToXmlFile<SurveyMission>(path + "//ContractConfigs//Survey//Example3.xml", mission);
            }
            if (!Directory.Exists(path + "//ContractConfigs//Mining//"))
            {
                GeneratedContract contract = new GeneratedContract();

                Directory.CreateDirectory(path + "//ContractConfigs//Mining//");
                contract.PlayerLoot.Add(new RewardItem());
                contract.PutInStation.Add(new RewardItem());
                contract.ItemsToPickFrom.Add(new ContractInfo());
                contract.ItemsToPickFrom.Add(new ContractInfo());
                contract.StationsToDeliverTo.Add(new StationDelivery());
                utils.WriteToXmlFile<GeneratedContract>(path + "//ContractConfigs//Mining//Example.xml", contract);
            }
            if (!Directory.Exists(path + "//ContractConfigs//Hauling//"))
            {
                GeneratedContract contract = new GeneratedContract();
                Directory.CreateDirectory(path + "//ContractConfigs//Hauling//");
                contract.type = ContractType.Hauling;
                contract.PlayerLoot.Add(new RewardItem());
                contract.PutInStation.Add(new RewardItem());
                contract.ItemsToPickFrom.Add(new ContractInfo());
                contract.ItemsToPickFrom.Add(new ContractInfo());
                contract.StationsToDeliverTo.Add(new StationDelivery());
                utils.WriteToXmlFile<GeneratedContract>(path + "//ContractConfigs//Hauling//Example.xml", contract);
            }
            if (!Directory.Exists(path + "//PlayerData//Data//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Data//");
            }
            if (!Directory.Exists(path + "//PlayerData//Mining//Completed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Mining//Completed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Mining//Failed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Mining//Failed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Mining//InProgress//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Mining//InProgress//");
            }
            if (!Directory.Exists(path + "//PlayerData//Hauling//Completed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Hauling//Completed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Hauling//Failed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Hauling//Failed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Hauling//InProgress//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Hauling//InProgress//");
            }
            if (!Directory.Exists(path + "//PlayerData//Survey//Completed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Survey//Completed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Survey//Failed//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Survey//Failed//");
            }
            if (!Directory.Exists(path + "//PlayerData//Survey//InProgress//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//Survey//InProgress//");
            }

            TorchBase = Torch;
        }

        public static MethodInfo AllianceTaxes;

        public static ITorchPlugin Alliance;

        public void SetupConfig()
        {

            path = StoragePath;

            if (File.Exists(StoragePath + "\\CrunchEconomy.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder = config.StoragePath;
            }
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder2 = config.StoragePath + "//CrunchEcon//";
            }

            Directory.CreateDirectory(folder2);


            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            FileUtils utils = new FileUtils();

            config = utils.ReadFromXmlFile<Config>(basePath + "\\CrunchEconomy.xml");


            return config;
        }
        public static void saveConfig()
        {
            FileUtils utils = new FileUtils();

            utils.WriteToXmlFile<Config>(basePath + "\\CrunchEconomy.xml", config);

            return;
        }
    }
}
