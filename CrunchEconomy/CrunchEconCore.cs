using Sandbox.Game.World;
using System;
using System.Collections.Generic;
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
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.Entities.Blocks;
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
using static CrunchEconomy.WhitelistFile;

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
                    if (!(block is MyStoreBlock store))
                    {
                        if (block.DisplayNameText != null && !block.DisplayNameText.Equals(station.CargoName))
                        {
                            continue;
                        }
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

            foreach (var block in grid.GetFatBlocks())
            {
                if (!block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                {
                    continue;
                }
                if (station.ViewOnlyNamedCargo)
                {
                    if (block.DisplayNameText != null && !block.DisplayNameText.Equals(station.CargoName) && !(block is MyStoreBlock store))
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
                            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
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
            if (grid != null)
            {


                foreach (var block in grid.GetFatBlocks())
                {

                    if (!block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                    {
                        continue;
                    }
                    if (block.DisplayNameText != null && !block.DisplayNameText.Equals(station.CargoName))
                    {
                        continue;
                    }
                    for (int i = 0; i < block.InventoryCount; i++)
                    {

                        VRage.Game.ModAPI.IMyInventory inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);

                        MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                        if (inv.CanItemsBeAdded(amount, itemType))
                        {
                            inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
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
                    SendMessage("[Shipyard]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)steamid);

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

                                MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(iden.IdentityId, target.FactionId, item.PlayerToFactionRep);
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
                                gpscol.SendAddGps(iden.IdentityId, ref gps);

                            }

                        }
                    }
                }
            }
        }
        public static void Logout(IPlayer p)
        {
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
                    contract.DeliveryLocation = DrillPatch.GenerateDeliveryLocation(player.GetPosition(), contract).ToString();
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
                        parseThis = "MyObjectBuilder_" + contract.type + "/" + contract.SubType;
                        rep = data.HaulingReputation;
                    }
                    if (MyDefinitionId.TryParse(parseThis, out MyDefinitionId id))
                    {
                        itemsToRemove.Add(id, contract.amountToMineOrDeliver);

                    }

                    List<VRage.Game.ModAPI.IMyInventory> inventories = GetInventoriesForContract(controller.CubeGrid);

                    if (FacUtils.IsOwnerOrFactionOwned(controller.CubeGrid, player.Identity.IdentityId, true))
                    {
                        if (ConsumeComponents(inventories, itemsToRemove, player.Id.SteamId))
                        {

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
                            if (data.MiningReputation >= 5000)
                            {
                                data.MiningReputation = 5000;
                            }
                            if (data.HaulingReputation >= 5000)
                            {
                                data.HaulingReputation = 5000;
                            }
                            if (data.MiningReputation >= 100)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 250)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 500)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 750)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 1000)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 2000)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.025f);
                            }
                            if (data.MiningReputation >= 3000)
                            {
                                contract.contractPrice += Convert.ToInt64(contract.contractPrice * 0.05f);
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
                                    object[] MethodInput = new object[] { player.Id.SteamId, contract.contractPrice, "Mining", controller.CubeGrid.PositionComp.GetPosition() };
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
                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out MyDefinitionId reward) && item.Enabled && rep >= item.ReputationRequired)
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
                                                SendMessage("Boss Dave", "Heres a bonus for a job well done " + amount + " " + reward.ToString().Replace("MyObjectBuilder_", ""), Color.Gold, (long)player.Id.SteamId);
                                            }
                                        }
                                    }
                                }
                            }

                            //  BoundingSphereD sphere = new BoundingSphereD(coords, 400);
                            MyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(contract.StationEntityId) as MyCubeGrid;
                            if (grid != null)
                            {

                                foreach (RewardItem item in contract.PutInStation)
                                {
                                    if (item.Enabled && rep >= item.ReputationRequired)
                                    {
                                        Random random = new Random();
                                        if (random.NextDouble() <= item.chance)
                                        {
                                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId, out MyDefinitionId newid))
                                            {
                                                int amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                                                Stations station = new Stations();
                                                station.CargoName = contract.CargoName;
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
                                        station.ViewOnlyNamedCargo = true;
                                        SpawnItems(grid, pair.Key, pair.Value, station);
                                    }
                                }

                            }
                            else
                            {
                                Log.Error("Couldnt find station to put items in! Did it get cut and pasted? at " + coords.ToString());
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
                            //if (data.getMiningContracts().Count == 0)
                            //{
                            //    GeneratedContract con = ContractUtils.GetRandomPlayerContract(ContractType.Mining);
                            //    if (con != null)
                            //    {
                            //        data.addMining(ContractUtils.GeneratedToPlayer(con));
                            //        playerData[player.Id.SteamId] = data;
                            //        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);

                            //    }

                            //}
                            SendMessage("Boss Dave", "Check contracts with !contract info", Color.Gold, (long)player.Id.SteamId);
                        }
                        //else
                        //{
                        //    PlayerData newdata = new PlayerData();
                        //    newdata.steamId = player.Id.SteamId;
                        //    GeneratedContract con = ContractUtils.GetRandomPlayerContract(ContractType.Mining);
                        //    if (con != null)
                        //    {
                        //        newdata.addMining(ContractUtils.GeneratedToPlayer(con));
                        //        playerData.Add(player.Id.SteamId, newdata);
                        //        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + newdata.steamId + ".json", newdata);
                        //        SendMessage("Boss Dave", "Heres a new job !contract info", Color.Gold, (long)player.Id.SteamId);
                        //    }

                        //}
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
        public void GenerateNewSurveyMission(PlayerData data, MyPlayer player)
        {
            try
            {
                if (data.surveyMission != Guid.Empty)
                {
                    //   Log.Info("Has survey");
                    bool ShouldReturn = false;
                    SurveyMission mission = data.GetLoadedMission();
                    if (mission != null)
                    {
                        //   Log.Info("not null");
                        float distance = Vector3.Distance(new Vector3(mission.CurrentPosX, mission.CurrentPosY, mission.CurrentPosZ), player.GetPosition());
                        if (distance <= mission.getStage(mission.CurrentStage).RadiusNearLocationToBeInside)
                        {
                            // Log.Info("within distance");
                            if (playerSurveyTimes.TryGetValue(player.Id.SteamId, out DateTime time))
                            {
                                var seconds = DateTime.Now.Subtract(time);

                                mission.getStage(mission.CurrentStage).Progress += Convert.ToInt32(seconds.TotalSeconds);
                                //  Log.Info("progress " + mission.getStage(mission.CurrentStage).Progress);
                                if (mission.getStage(mission.CurrentStage).Progress >= mission.getStage(mission.CurrentStage).SecondsToStayInArea)
                                {
                                    // Log.Info("Completed");
                                    mission.getStage(mission.CurrentStage).Completed = true;
                                    //do rewards
                                    long money = mission.getStage(mission.CurrentStage).CreditReward;
                                    if (AlliancePluginEnabled)
                                    {
                                        //patch into alliances and process the payment there
                                        //contract.AmountPaid = contract.contractPrice;
                                        try
                                        {
                                            object[] MethodInput = new object[] { player.Id.SteamId, money, "Survey", player.Character.PositionComp.GetPosition() };
                                            money = (long)AllianceTaxes?.Invoke(null, MethodInput);

                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex);
                                        }
                                    }
                                    if (mission.getStage(mission.CurrentStage).DoRareItemReward && data.SurveyReputation >= mission.getStage(mission.CurrentStage).MinimumRepRequiredForItem)
                                    {
                                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + mission.getStage(mission.CurrentStage).RewardItemType, mission.getStage(mission.CurrentStage).RewardItemSubType, out MyDefinitionId reward))
                                        {

                                            Random rand = new Random();
                                            double chance = rand.NextDouble();
                                            if (chance <= mission.getStage(mission.CurrentStage).ItemRewardChance)
                                            {

                                                MyItemType itemType = new MyInventoryItemFilter(reward.TypeId + "/" + reward.SubtypeName).ItemType;
                                                if (player.Character.GetInventory() != null && player.Character.GetInventory().CanItemsBeAdded((MyFixedPoint)mission.getStage(mission.CurrentStage).ItemRewardAmount, itemType))
                                                {
                                                    player.Character.GetInventory().AddItems((MyFixedPoint)mission.getStage(mission.CurrentStage).ItemRewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(reward));
                                                    SendMessage("Survey", "Bonus item reward in character inventory.", Color.Gold, (long)player.Id.SteamId);
                                                }
                                            }

                                        }
                                    }
                                    EconUtils.addMoney(player.Identity.IdentityId, money);

                                    SendMessage("Survey", mission.getStage(mission.CurrentStage).CompletionMessage, Color.Gold, (long)player.Id.SteamId);
                                    data.SurveyReputation += mission.getStage(mission.CurrentStage).ReputationGain;
                                    if (mission.getStage(mission.CurrentStage + 1) != null)
                                    {

                                        mission.CurrentStage += 1;
                                        if (data.SurveyReputation >= mission.getStage(mission.CurrentStage).MinimumReputation && data.SurveyReputation <= mission.getStage(mission.CurrentStage).MaximumReputation)
                                        {


                                            data.NextSurveyMission = data.NextSurveyMission.AddSeconds(60);


                                            if (mission.getStage(mission.CurrentStage).FindRandomPositionAroundLocation)

                                            {
                                                MyGps gps = ContractUtils.ScanChat(mission.getStage(1).LocationGPS);

                                                if (mission.getStage(mission.CurrentStage).FindRandomPositionAroundLocation)
                                                {
                                                    int negative = System.Math.Abs(mission.getStage(mission.CurrentStage).RadiusToPickRandom) * (-1);
                                                    int positive = mission.getStage(mission.CurrentStage).RadiusToPickRandom;

                                                    Random rand = new Random();
                                                    int x = rand.Next(negative, positive);
                                                    int y = rand.Next(negative, positive);
                                                    int z = rand.Next(negative, positive);
                                                    Vector3 offset = new Vector3(x, y, z);
                                                    gps.Coords += offset;
                                                }

                                                mission.CurrentPosX = gps.Coords.X;
                                                mission.CurrentPosY = gps.Coords.Y;
                                                mission.CurrentPosZ = gps.Coords.Z;
                                                StringBuilder sb = new StringBuilder();
                                                sb.AppendLine(mission.getStage(mission.CurrentStage).GPSDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.getStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.getStage(mission.CurrentStage).GPSName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGps(player.Identity.IdentityId, ref gps);
                                            }
                                            else
                                            {
                                                MyGps gps = ContractUtils.ScanChat(mission.getStage(mission.CurrentStage).LocationGPS);
                                                mission.CurrentPosX = gps.Coords.X;
                                                mission.CurrentPosY = gps.Coords.Y;
                                                mission.CurrentPosZ = gps.Coords.Z;
                                                StringBuilder sb = new StringBuilder();
                                                sb.AppendLine(mission.getStage(mission.CurrentStage).GPSDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.getStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.getStage(mission.CurrentStage).GPSName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGps(player.Identity.IdentityId, ref gps);
                                            }
                                        }
                                        else
                                        {
                                            mission.status = ContractStatus.Completed;
                                            File.Delete(path + "//PlayerData//Survey//InProgress" + mission.id + ".xml");
                                            data.SetLoadedSurvey(null);
                                            data.surveyMission = Guid.Empty;
                                        }
                                        CrunchEconCore.SurveySave.Remove(mission.id);
                                        CrunchEconCore.SurveySave.Add(mission.id, mission);
                                        playerData[player.Id.SteamId] = data;
                                        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                                    }
                                    else
                                    {
                                        mission.status = ContractStatus.Completed;
                                        data.SetLoadedSurvey(null);
                                        data.surveyMission = Guid.Empty;
                                        File.Delete(path + "//PlayerData//Survey//InProgress" + mission.id + ".xml");
                                        CrunchEconCore.SurveySave.Remove(mission.id);
                                        CrunchEconCore.SurveySave.Add(mission.id, mission);
                                        playerData[player.Id.SteamId] = data;
                                        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                                    }
                                    playerSurveyTimes.Remove(player.Id.SteamId);
                                    List<IMyGps> playerList = new List<IMyGps>();
                                    MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                                    foreach (IMyGps gps in playerList)
                                    {
                                        if (gps.Description != null && gps.Description.Contains("SURVEY LOCATION."))
                                        {
                                            MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
                                        }
                                    }


                                    return;
                                }
                                else
                                {
                                    if (MessageCooldowns.TryGetValue(player.Id.SteamId, out DateTime time2))
                                    {
                                        if (DateTime.Now >= time2)
                                        {
                                            NotificationMessage message2 = new NotificationMessage();

                                            message2 = new NotificationMessage("Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                            //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                            ModCommunication.SendMessageTo(message2, player.Id.SteamId);


                                            // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                            MessageCooldowns[player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                        }
                                    }
                                    else
                                    {
                                        NotificationMessage message2 = new NotificationMessage();

                                        message2 = new NotificationMessage("Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);


                                        // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                        MessageCooldowns[player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                    }
                                    data.NextSurveyMission = data.NextSurveyMission.AddSeconds(60);
                                    data.SetLoadedSurvey(mission);
                                    playerData[player.Id.SteamId] = data;
                                    playerSurveyTimes[player.Id.SteamId] = DateTime.Now;
                                    ShouldReturn = true;
                                }

                            }
                            else
                            {
                                playerSurveyTimes.Add(player.Id.SteamId, DateTime.Now);
                                ShouldReturn = true;
                            }
                            data.SetLoadedSurvey(mission);
                            data.NextSurveyMission = DateTime.Now.AddSeconds(60);
                            CrunchEconCore.SurveySave.Remove(mission.id);
                            CrunchEconCore.SurveySave.Add(mission.id, mission);
                            playerData[player.Id.SteamId] = data;
                            // utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                        }
                        else
                        {
                            //  Log.Info("not within distance");
                            ShouldReturn = false;
                        }
                    }
                    else
                    {
                        ShouldReturn = false;
                    }
                    if (ShouldReturn)
                    {
                        return;
                    }
                }
                if (DateTime.Now >= data.NextSurveyMission)
                {

                    SurveyMission newSurvey = ContractUtils.GetNewMission(data);
                    if (newSurvey != null)
                    {
                        List<IMyGps> playerList = new List<IMyGps>();
                        MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                        foreach (IMyGps gps in playerList)
                        {
                            if (gps.Description != null && gps.Description.Contains("SURVEY LOCATION."))
                            {
                                MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
                            }
                        }

                        data.surveyMission = newSurvey.id;
                        if (newSurvey.getStage(1).FindRandomPositionAroundLocation)

                        {
                            MyGps gps = ContractUtils.ScanChat(newSurvey.getStage(1).LocationGPS);

                            if (newSurvey.getStage(1).FindRandomPositionAroundLocation)
                            {
                                int negative = System.Math.Abs(newSurvey.getStage(1).RadiusToPickRandom) * (-1);
                                int positive = newSurvey.getStage(1).RadiusToPickRandom;

                                Random rand = new Random();
                                int x = rand.Next(negative, positive);
                                int y = rand.Next(negative, positive);
                                int z = rand.Next(negative, positive);
                                Vector3 offset = new Vector3(x, y, z);
                                gps.Coords += offset;
                            }

                            newSurvey.CurrentPosX = gps.Coords.X;
                            newSurvey.CurrentPosY = gps.Coords.Y;
                            newSurvey.CurrentPosZ = gps.Coords.Z;
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(newSurvey.getStage(1).GPSDescription);
                            sb.AppendLine("");
                            sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.getStage(1).CreditReward) + " SC.");
                            sb.AppendLine("");
                            sb.AppendLine("SURVEY LOCATION.");
                            gps.Description = sb.ToString();
                            gps.GPSColor = Color.Gold;
                            gps.Name = newSurvey.getStage(1).GPSName;
                            gps.ShowOnHud = true;
                            gps.DiscardAt = new TimeSpan(6000);

                            MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                            gpscol.SendAddGps(player.Identity.IdentityId, ref gps);
                        }
                        else
                        {
                            MyGps gps = ContractUtils.ScanChat(newSurvey.getStage(1).LocationGPS);
                            newSurvey.CurrentPosX = gps.Coords.X;
                            newSurvey.CurrentPosY = gps.Coords.Y;
                            newSurvey.CurrentPosZ = gps.Coords.Z;
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(newSurvey.getStage(1).GPSDescription);
                            sb.AppendLine("");
                            sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.getStage(1).CreditReward) + " SC.");
                            sb.AppendLine("");
                            sb.AppendLine("SURVEY LOCATION.");
                            gps.Description = sb.ToString();
                            gps.GPSColor = Color.Gold;
                            gps.Name = newSurvey.getStage(1).GPSName;
                            gps.ShowOnHud = true;
                            gps.DiscardAt = new TimeSpan(6000);

                            MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                            gpscol.SendAddGps(player.Identity.IdentityId, ref gps);
                        }
                        data.SetLoadedSurvey(newSurvey);
                        data.NextSurveyMission = DateTime.Now.AddSeconds(config.SecondsBetweenSurveyMissions);
                        CrunchEconCore.SurveySave.Remove(newSurvey.id);
                        CrunchEconCore.SurveySave.Add(newSurvey.id, newSurvey);

                        playerData[player.Id.SteamId] = data;
                        utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                    }
                    else
                    {
                        //    Log.Info("null");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                data.SetLoadedSurvey(null);
                data.surveyMission = Guid.Empty;
                playerData[player.Id.SteamId] = data;
                utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                throw;
            }
        }
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
                        if (!station.WorldName.Equals("default"))
                        {
                            if (station.WorldName.Equals(MyMultiplayer.Static.HostName))
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
                    }
                }
            });
        }
        public static Random rnd = new Random();
        public override  void Update()
        {
            ticks++;
            if (paused)
            {
                return;
            }


            if (DateTime.Now >= NextFileRefresh)
            {
                NextFileRefresh = DateTime.Now.AddMinutes(15);
                Log.Info("Loading stuff for CrunchEcon");
                try
                {
                    repConfig = utils.ReadFromXmlFile<RepConfig>(path + "\\ReputationConfig.xml");
                    whitelist = utils.ReadFromXmlFile<WhitelistFile>(path + "\\Whitelist.xml");
                    //MarketCommands.list.RefreshList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                try
                {
                    ContractUtils.LoadAllContracts();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading mining contract stuff " + ex.ToString());
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
                    DoStationShit();
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
                    if (config != null && config.SurveyContractsEnabled)
                    {
                        if (playerData.TryGetValue(player.Id.SteamId, out PlayerData data))
                        {


                            GenerateNewSurveyMission(data, player);

                        }
                        else
                        {

                            data = new PlayerData();
                            data.steamId = player.Id.SteamId;
                            GenerateNewSurveyMission(data, player);
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
                ContractSave.Clear();

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
                SurveySave.Clear();
                DateTime now = DateTime.Now;
                foreach (Stations station in stations)
                {
                    //first check if its any, then we can load the grid to do the editing
                    try
                    {

                        if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh)
                        {
                            MyGps gps = station.getGPS();
                            Boolean AddSellTime = false;
                            Boolean AddBuyTime = false;
                            bool checkLocation = false;
                            if (!station.WorldName.Equals("default"))
                            {
                                if (station.WorldName.Equals(MyMultiplayer.Static.HostName))
                                {
                                    if (station.StationEntityId > 0)
                                    {
                                        if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) != null)
                                        {
                                            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is MyCubeGrid grid)
                                            {
                                                if (station.DoPeriodGridClearing && now >= station.nextGridInventoryClear)
                                                {
                                                    station.nextGridInventoryClear = now.AddSeconds(station.SecondsBetweenClearEntireInventory);
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
                                                        if (now >= station.nextSellRefresh && station.DoSellOffers)
                                                        {
                                                            //    Log.Info(store.DisplayNameText);
                                                            //    Log.Info("its past the timer");
                                                            AddSellTime = true;

                                                            if (sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                                            {
                                                                //     Log.Info("it found the store files");

                                                                ClearStoreOfPlayersBuyingOffers(store);
                                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                                inventories.AddRange(GetInventories(grid, station));

                                                                //  Log.Info("now its checking offers");

                                                                foreach (SellOffer offer in offers)
                                                                {
                                                                    try
                                                                    {

                                                                        //Log.Info("this is an offer");
                                                                        double chance = rnd.NextDouble();

                                                                        //   Log.Info("Should be adding something to sell?");

                                                                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + offer.typeId, offer.subtypeId, out MyDefinitionId id))
                                                                        {

                                                                            int hasAmount = CountComponents(inventories, id).ToIntSafe();
                                                                            //  Log.Info("AMOUNT " + hasAmount + " " + id.ToString() + " " + station.Name);
                                                                            if (hasAmount > 0)
                                                                            {
                                                                                if (hasAmount < offer.SpawnIfCargoLessThan && offer.SpawnItemsIfNeeded)
                                                                                {
                                                                                    int amountSpawned = 0;

                                                                                    amountSpawned = rnd.Next(offer.minAmountToSpawn, offer.maxAmountToSpawn);
                                                                                    if (offer.IndividualRefreshTimer)
                                                                                    {
                                                                                        if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                                        {
                                                                                            if (now >= refresh)
                                                                                            {
                                                                                                individualTimers[store.EntityId] = DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh);


                                                                                                if (chance <= offer.chance)
                                                                                                {
                                                                                                    SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
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
                                                                                            individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));



                                                                                            if (chance <= offer.chance)
                                                                                            {
                                                                                                SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                                hasAmount += amountSpawned;
                                                                                            }
                                                                                        }


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        //spawn items
                                                                                        if (chance <= offer.chance)
                                                                                        {
                                                                                            SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                            hasAmount += amountSpawned;
                                                                                        }
                                                                                    }

                                                                                }

                                                                                SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);



                                                                                int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                                                                                price = Convert.ToInt32(price * station.GetModifier(offer.StationModifierItemName));
                                                                                MyStoreItemData item = new MyStoreItemData(itemId, hasAmount, price, null, null);
                                                                                //       Log.Info("if it got here its creating the offer");
                                                                                MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);

                                                                                if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                                {
                                                                                    Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (offer.SpawnItemsIfNeeded)
                                                                                {
                                                                                    int amountSpawned = 0;

                                                                                    amountSpawned = rnd.Next(offer.minAmountToSpawn, offer.maxAmountToSpawn);
                                                                                    if (offer.IndividualRefreshTimer)
                                                                                    {
                                                                                        if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                                        {
                                                                                            if (now >= refresh)
                                                                                            {
                                                                                                individualTimers[store.EntityId] = DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh);


                                                                                                if (chance <= offer.chance)
                                                                                                {
                                                                                                    SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
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
                                                                                            individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));



                                                                                            if (chance <= offer.chance)
                                                                                            {
                                                                                                SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                                hasAmount += amountSpawned;
                                                                                            }
                                                                                        }

                                                                                    }
                                                                                    else
                                                                                    {


                                                                                        //spawn items
                                                                                        SpawnItems(grid, id, (MyFixedPoint)offer.SpawnIfCargoLessThan, station);
                                                                                    }


                                                                                    SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);




                                                                                    int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                                                                                    price = Convert.ToInt32(price * station.GetModifier(offer.StationModifierItemName));
                                                                                    MyStoreItemData item = new MyStoreItemData(itemId, offer.SpawnIfCargoLessThan, price, null, null);
                                                                                    //    Log.Info("if it got here its creating the offer");
                                                                                    MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);
                                                                                    if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                                    {
                                                                                        Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                                    }
                                                                                }
                                                                            }

                                                                        }
                                                                    }



                                                                    catch (Exception ex)
                                                                    {
                                                                        Log.Error("Error on this sell offer " + offer.typeId + " " + offer.subtypeId);
                                                                        Log.Error("for this station " + station.Name);
                                                                        station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
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


                                                            if (buyOrders.TryGetValue(store.DisplayNameText, out List<BuyOrder> orders))
                                                            {

                                                                ClearStoreOfPlayersSellingOrders(store);
                                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                                inventories.AddRange(GetInventories(grid, station));

                                                                foreach (BuyOrder order in orders)
                                                                {

                                                                    try
                                                                    {
                                                                        if (order.IndividualRefreshTimer)
                                                                        {
                                                                            if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                            {
                                                                                if (now < refresh)
                                                                                {
                                                                                    continue;
                                                                                }
                                                                                else
                                                                                {
                                                                                    individualTimers[store.EntityId] = DateTime.Now.AddSeconds(order.SecondsBetweenRefresh);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(order.SecondsBetweenRefresh));
                                                                            }
                                                                        }
                                                                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + order.typeId, order.subtypeId, out MyDefinitionId id))
                                                                        {

                                                                            SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, order.subtypeId);


                                                                            double chance = rnd.NextDouble();
                                                                            if (chance <= order.chance)
                                                                            {
                                                                                int price = rnd.Next((int)order.minPrice, (int)order.maxPrice);
                                                                                price = Convert.ToInt32(price * station.GetModifier(order.StationModifierItemName));
                                                                                int amount = rnd.Next((int)order.minAmount, (int)order.maxAmount);
                                                                                MyStoreItemData item = new MyStoreItemData(itemId, amount, price, null, null);
                                                                                MyStoreInsertResults result = store.InsertOrder(item, out long notUsingThis);
                                                                                if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                                {
                                                                                    Log.Error("Unable to insert this order into store " + order.typeId + " " + order.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                                }
                                                                            }
                                                                        }
                                                                    }


                                                                    catch (Exception ex)
                                                                    {

                                                                        Log.Error("Error on this buy order " + order.typeId + " " + order.subtypeId);
                                                                        Log.Error("for this station " + station.Name);
                                                                        station.nextBuyRefresh = now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);
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
                            }
                            else
                            {
                                checkLocation = true;
                            }

                            if (checkLocation)
                            {
                                BoundingSphereD sphere = new BoundingSphereD(gps.Coords, 200);

                                foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                                {
                                    if (station.DoPeriodGridClearing && now >= station.nextGridInventoryClear)
                                    {
                                        station.nextGridInventoryClear = now.AddSeconds(station.SecondsBetweenClearEntireInventory);
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

                                                if (sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                                {
                                                    //     Log.Info("it found the store files");

                                                    ClearStoreOfPlayersBuyingOffers(store);
                                                    List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                    inventories.AddRange(GetInventories(grid, station));

                                                    //  Log.Info("now its checking offers");

                                                    foreach (SellOffer offer in offers)
                                                    {
                                                        try
                                                        {

                                                            //Log.Info("this is an offer");
                                                            double chance = rnd.NextDouble();

                                                            //   Log.Info("Should be adding something to sell?");

                                                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + offer.typeId, offer.subtypeId, out MyDefinitionId id))
                                                            {

                                                                int hasAmount = CountComponents(inventories, id).ToIntSafe();
                                                                //  Log.Info("AMOUNT " + hasAmount + " " + id.ToString() + " " + station.Name);
                                                                if (hasAmount > 0)
                                                                {
                                                                    if (hasAmount < offer.SpawnIfCargoLessThan && offer.SpawnItemsIfNeeded)
                                                                    {
                                                                        int amountSpawned = 0;

                                                                        amountSpawned = rnd.Next(offer.minAmountToSpawn, offer.maxAmountToSpawn);
                                                                        if (offer.IndividualRefreshTimer)
                                                                        {
                                                                            if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                            {
                                                                                if (now >= refresh)
                                                                                {
                                                                                    individualTimers[store.EntityId] = DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh);


                                                                                    if (chance <= offer.chance)
                                                                                    {
                                                                                        SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
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
                                                                                individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));



                                                                                if (chance <= offer.chance)
                                                                                {
                                                                                    SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                    hasAmount += amountSpawned;
                                                                                }
                                                                            }


                                                                        }
                                                                        else
                                                                        {
                                                                            //spawn items
                                                                            if (chance <= offer.chance)
                                                                            {
                                                                                SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                hasAmount += amountSpawned;
                                                                            }
                                                                        }

                                                                    }

                                                                    SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);



                                                                    int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                                                                    price = Convert.ToInt32(price * station.GetModifier(offer.StationModifierItemName));
                                                                    MyStoreItemData item = new MyStoreItemData(itemId, hasAmount, price, null, null);
                                                                    //       Log.Info("if it got here its creating the offer");
                                                                    MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);

                                                                    if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                    {
                                                                        Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (offer.SpawnItemsIfNeeded)
                                                                    {
                                                                        int amountSpawned = 0;

                                                                        amountSpawned = rnd.Next(offer.minAmountToSpawn, offer.maxAmountToSpawn);
                                                                        if (offer.IndividualRefreshTimer)
                                                                        {
                                                                            if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                            {
                                                                                if (now >= refresh)
                                                                                {
                                                                                    individualTimers[store.EntityId] = DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh);


                                                                                    if (chance <= offer.chance)
                                                                                    {
                                                                                        SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
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
                                                                                individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));



                                                                                if (chance <= offer.chance)
                                                                                {
                                                                                    SpawnItems(grid, id, (MyFixedPoint)amountSpawned, station);
                                                                                    hasAmount += amountSpawned;
                                                                                }
                                                                            }

                                                                        }
                                                                        else
                                                                        {


                                                                            //spawn items
                                                                            SpawnItems(grid, id, (MyFixedPoint)offer.SpawnIfCargoLessThan, station);
                                                                        }


                                                                        SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);




                                                                        int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                                                                        price = Convert.ToInt32(price * station.GetModifier(offer.StationModifierItemName));
                                                                        MyStoreItemData item = new MyStoreItemData(itemId, offer.SpawnIfCargoLessThan, price, null, null);
                                                                        //    Log.Info("if it got here its creating the offer");
                                                                        MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);
                                                                        if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                        {
                                                                            Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                        }
                                                                    }
                                                                }

                                                            }
                                                        }



                                                        catch (Exception ex)
                                                        {
                                                            Log.Error("Error on this sell offer " + offer.typeId + " " + offer.subtypeId);
                                                            Log.Error("for this station " + station.Name);
                                                            station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
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


                                                if (buyOrders.TryGetValue(store.DisplayNameText, out List<BuyOrder> orders))
                                                {

                                                    ClearStoreOfPlayersSellingOrders(store);
                                                    List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                    inventories.AddRange(GetInventories(grid, station));

                                                    foreach (BuyOrder order in orders)
                                                    {

                                                        try
                                                        {
                                                            if (order.IndividualRefreshTimer)
                                                            {
                                                                if (individualTimers.TryGetValue(store.EntityId, out DateTime refresh))
                                                                {
                                                                    if (now < refresh)
                                                                    {
                                                                        continue;
                                                                    }
                                                                    else
                                                                    {
                                                                        individualTimers[store.EntityId] = DateTime.Now.AddSeconds(order.SecondsBetweenRefresh);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    individualTimers.Add(store.EntityId, DateTime.Now.AddSeconds(order.SecondsBetweenRefresh));
                                                                }
                                                            }
                                                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + order.typeId, order.subtypeId, out MyDefinitionId id))
                                                            {

                                                                SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, order.subtypeId);


                                                                double chance = rnd.NextDouble();
                                                                if (chance <= order.chance)
                                                                {
                                                                    int price = rnd.Next((int)order.minPrice, (int)order.maxPrice);
                                                                    price = Convert.ToInt32(price * station.GetModifier(order.StationModifierItemName));
                                                                    int amount = rnd.Next((int)order.minAmount, (int)order.maxAmount);
                                                                    MyStoreItemData item = new MyStoreItemData(itemId, amount, price, null, null);
                                                                    MyStoreInsertResults result = store.InsertOrder(item, out long notUsingThis);
                                                                    if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                    {
                                                                        Log.Error("Unable to insert this order into store " + order.typeId + " " + order.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                    }
                                                                }
                                                            }
                                                        }


                                                        catch (Exception ex)
                                                        {

                                                            Log.Error("Error on this buy order " + order.typeId + " " + order.subtypeId);
                                                            Log.Error("for this station " + station.Name);
                                                            station.nextBuyRefresh = now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);
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
                                station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
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
                        stations.Add(stat);

                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                }

            }
        }
        public void LoadAllBuyOrders()
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

        public void LoadAllGridSales()
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

        public void LoadAllSellOffers()
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
        public static Dictionary<String, GridSale> gridsForSale = new Dictionary<string, GridSale>();

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            TorchState = state;
            if (state == TorchSessionState.Unloading)
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
