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
using CrunchEconomy.Storage;
using CrunchEconomy.Storage.Interfaces;
using static CrunchEconomy.WhitelistFile;
using Sandbox.Definitions;

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

        public static IPlayerDataProvider PlayerStorageProvider { get; set; }
        public static IConfigProvider ConfigProvider { get; set; }

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
                //physical objects are not fun to count
                //else
                //{

                //    if (id.TypeId.ToString().Equals("MyObjectBuilder_PhysicalObject"))
                //    {

                //      //  CrunchEconCore.Log.Info("LOOKING FOR " + id.SubtypeName);
                //        foreach (VRage.Game.ModAPI.IMyInventoryItem item in inv.GetItems())
                //        {
                //            if (!item.Content.TypeId.ToString().Equals("MyObjectBuilder_PhysicalObject"))
                //            {
                //                continue;
                //            }
                //            CrunchEconCore.Log.Info(item.Content.SubtypeName.ToString() + " " + item.Content.TypeId.ToString());

                //            if (item.Content.SubtypeName.Equals(id.SubtypeName))
                //            {
                //                CrunchEconCore.Log.Info("found it");
                //                targetAmount += item.Amount;
                //            }
                //        }
                //    }
                //}
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
                                inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
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
                    foreach (RepItem item in ConfigProvider.RepConfig.RepConfigs)
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

        public static void Login(IPlayer player)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (player == null)
            {
                return;
            }

            DoFactionShit(player);

            var data = PlayerStorageProvider.GetPlayerData(player.SteamId);
            data.GetHaulingContracts();
            data.GetMiningContracts();
            var id = MySession.Static.Players.TryGetIdentityId(player.SteamId);
            if (id == 0)
            {
                return;
            }
            var playerList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(id, playerList);
            foreach (var gps in playerList.Where(Gps => Gps.Description != null && Gps.Description.Contains("Contract Delivery Location.")))
            {
                MyAPIGateway.Session?.GPS.RemoveGps(id, gps);
            }

            foreach (var c in data.GetMiningContracts().Values.Where(c => c.minedAmount >= c.amountToMineOrDeliver))
            {
                c.DoPlayerGps(id);
            }

            foreach (var c in data.GetHaulingContracts().Values)
            {
                c.DoPlayerGps(id);
            }
            var iden = GetIdentityByNameOrId(player.SteamId.ToString());
            if (iden == null) return;

            var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

            //this as linq no work
            foreach (var gps in from stat in stations where stat.GiveGPSOnLogin where stat.getGPS() != null select stat.getGPS())
            {
                var myGps = gps;
                myGps.DiscardAt = new TimeSpan(6000);
                gpscol.SendAddGpsRequest(iden.IdentityId, ref myGps);
            }
        }

        //public static void Logout(IPlayer p)
        //{
        //    if (CrunchEconCore.config != null && !CrunchEconCore.config.PluginEnabled)
        //    {
        //        return;
        //    }
        //    if (p == null)
        //    {
        //        return;
        //    }

        //    if (File.Exists(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json"))
        //    {
        //        PlayerData data = utils.ReadFromJsonFile<PlayerData>(path + "//PlayerData//Data//" + p.SteamId.ToString() + ".json");
        //        playerData.Remove(p.SteamId);
        //        data.getMiningContracts();
        //        data.getHaulingContracts();
        //        playerData.Add(p.SteamId, data);
        //    }
        //}


        public static Boolean AlliancePluginEnabled = false;
        //i should really split this into multiple methods so i dont have one huge method for everything
        public bool HandleDeliver(Contract contract, MyPlayer player, PlayerData data, MyCockpit controller)
        {
            var proceed = false;
            switch (contract.type)
            {
                case ContractType.Mining:
                    {
                        if (config.MiningContractsEnabled)
                        {
                            if (contract.minedAmount >= contract.amountToMineOrDeliver)
                            {
                                proceed = true;
                            }
                        }

                        break;
                    }
                case ContractType.Hauling:
                    {
                        if (config.HaulingContractsEnabled)
                        {
                            proceed = true;
                        }

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!proceed) return false;
            if (contract.DeliveryLocation == null && contract.DeliveryLocation == string.Empty)
            {
                contract.DeliveryLocation = DrillPatch.GenerateDeliveryLocation(player.GetPosition(), contract).ToString();
                PlayerStorageProvider.AddContractToBeSaved(contract);
            }

            Vector3D coords = contract.getCoords();
            var rep = 0;
            var distance = Vector3.Distance(coords, controller.PositionComp.GetPosition());
            if (!(distance <= 500)) return false;

            var itemsToRemove = new Dictionary<MyDefinitionId, int>();
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

            var inventories = GetInventoriesForContract(controller.CubeGrid);

            if (!FacUtils.IsOwnerOrFactionOwned(controller.CubeGrid, player.Identity.IdentityId, true)) return false;
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
                    var MethodInput = new object[] { player.Id.SteamId, contract.contractPrice, "Mining", controller.CubeGrid.PositionComp.GetPosition() };
                    contract.contractPrice = (long)AllianceTaxes?.Invoke(null, MethodInput);

                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            if (contract.DoRareItemReward)
            {
                foreach (var item in contract.PlayerLoot)
                {
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var reward) || !item.Enabled || rep < item.ReputationRequired) continue;
                    //  Log.Info("Tried to do ");
                    var rand = new Random();
                    var amount = rand.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var chance = rand.NextDouble();
                    if (!(chance <= item.chance)) continue;
                    if (!SpawnLoot(controller.CubeGrid, reward, (MyFixedPoint)amount)) continue;
                    contract.GivenItemReward = true;
                    SendMessage("Boss Dave", $"Heres a bonus for a job well done {amount} {reward.ToString().Replace("MyObjectBuilder_", "")}", Color.Gold, (long)player.Id.SteamId);
                }
            }

            //  BoundingSphereD sphere = new BoundingSphereD(coords, 400);
            var grid = MyAPIGateway.Entities.GetEntityById(contract.StationEntityId) as MyCubeGrid;
            if (grid != null)
            {
                foreach (var item in contract.PutInStation)
                {
                    if (!item.Enabled || rep < item.ReputationRequired) continue;
                    var random = new Random();
                    if (!(random.NextDouble() <= item.chance)) continue;
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.TypeId + "/" + item.SubTypeId,
                            out var newid)) continue;
                    var amount = random.Next(item.ItemMinAmount, item.ItemMaxAmount);
                    var station = new Stations
                    {
                        CargoName = contract.CargoName,
                        OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid)),
                        ViewOnlyNamedCargo = true
                    };
                    SpawnItems(grid, newid, amount, station);
                }
                if (contract.PutTheHaulInStation)
                {
                    foreach (var pair in itemsToRemove)
                    {
                        var station = new Stations
                        {
                            CargoName = contract.CargoName,
                            OwnerFactionTag = FacUtils.GetFactionTag(FacUtils.GetOwner(grid)),
                            ViewOnlyNamedCargo = true
                        };
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
            contract.status = ContractStatus.Completed;
            PlayerStorageProvider.AddContractToBeSaved(contract, true);
            PlayerStorageProvider.playerData[player.Id.SteamId] = data;

            return true;

            //SAVE THE PLAYER DATA WITH INCREASED REPUTATION

        }

        public void DoContractDelivery(MyPlayer player, bool DoNewContract)
        {
            if (player.GetPosition() == null) return;

            if (!config.MiningContractsEnabled) return;
            var data = PlayerStorageProvider.GetPlayerData(player.Id.SteamId);
            if (data.MiningContracts.Count <= 0 && data.HaulingContracts.Count <= 0) return;
            var playerOnline = player;
            if (player.Character == null ||
                !(player?.Controller.ControlledEntity is MyCockpit controller)) return;
            var grid = controller.CubeGrid;
            var delete = new List<Contract>();
            delete.AddRange(data.GetMiningContracts().Values.Where(contract => HandleDeliver(contract, player, data, controller)));
            delete.AddRange(data.GetHaulingContracts().Values.Where(contract => HandleDeliver(contract, player, data, controller)));
            foreach (var contract in delete)
            {
                if (contract.type == ContractType.Mining)
                {
                    data.GetMiningContracts().Remove(contract.ContractId);
                    data.MiningContracts.Remove(contract.ContractId);
                }
                else
                {
                    data.GetHaulingContracts().Remove(contract.ContractId);
                    data.MiningContracts.Remove(contract.ContractId);
                }
            }

            PlayerStorageProvider.playerData[player.Id.SteamId] = data;
            try
            {
                PlayerStorageProvider.SavePlayerData(data);
            }
            catch (Exception ex)
            {
                Log.Error("WHY YOU DO THIS?");
            }
        }


        public static Dictionary<ulong, DateTime> playerSurveyTimes = new Dictionary<ulong, DateTime>();
        public static Dictionary<ulong, DateTime> MessageCooldowns = new Dictionary<ulong, DateTime>();
        public void GenerateNewSurveyMission(PlayerData data, MyPlayer player)
        {
            try
            {
                List<IMyGps> playerList;
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
                                                gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
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
                                                gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                                            }
                                        }
                                        else
                                        {
                                            mission.status = ContractStatus.Completed;
                                            data.SetLoadedSurvey(null);
                                            data.surveyMission = Guid.Empty;
                                        }

                                        PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                                        PlayerStorageProvider.SavePlayerData(data);

                                    }
                                    else
                                    {
                                        mission.status = ContractStatus.Completed;
                                        data.SetLoadedSurvey(null);
                                        data.surveyMission = Guid.Empty;
                                        PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                                        PlayerStorageProvider.SavePlayerData(data);
                                    }
                                    playerSurveyTimes.Remove(player.Id.SteamId);
                                    playerList = new List<IMyGps>();
                                    MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                                    foreach (var gps in playerList.Where(gps => gps.Description != null && gps.Description.Contains("SURVEY LOCATION.")))
                                    {
                                        MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
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
                                    PlayerStorageProvider.playerData[player.Id.SteamId] = data;
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
                            PlayerStorageProvider.AddSurveyToBeSaved(mission);
                            PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                            // utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                        }
                    }
                    if (ShouldReturn)
                    {
                        return;
                    }
                }

                if (DateTime.Now < data.NextSurveyMission) return;

                var newSurvey = ContractUtils.GetNewMission(data);
                if (newSurvey == null) return;

                playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                foreach (var gps in playerList.Where(gps => gps.Description != null && gps.Description.Contains("SURVEY LOCATION.")))
                {
                    MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
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
                    gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
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
                    gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                }
                data.SetLoadedSurvey(newSurvey);
                data.NextSurveyMission = DateTime.Now.AddSeconds(config.SecondsBetweenSurveyMissions);
                PlayerStorageProvider.AddSurveyToBeSaved(newSurvey);

                PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                PlayerStorageProvider.SavePlayerData(data);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                data.SetLoadedSurvey(null);
                data.surveyMission = Guid.Empty;
                PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                PlayerStorageProvider.SavePlayerData(data);
                throw;
            }
        }
        public static Dictionary<long, DateTime> individualTimers = new Dictionary<long, DateTime>();

        public static string GetPlayerName(ulong steamId)
        {
            var id = GetIdentityByNameOrId(steamId.ToString());
            if (id != null && id.DisplayName != null)
            {
                return id.DisplayName;
            }

            return steamId.ToString();
        }
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (!ulong.TryParse(playerNameOrSteamId, out ulong steamId)) continue;
                var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                if (id == steamId)
                    return identity;
                if (identity.IdentityId == (long)steamId)
                    return identity;

            }
            return null;
        }
        public static async void DoStationShit()
        {
            Log.Info("Redoing station whitelists");
            await Task.Run(() =>
            {
                foreach (var station in stations)
                {
                    if (!station.WhitelistedSafezones) continue;
                    var sphere = new BoundingSphereD(station.getGPS().Coords, 200);

                    foreach (var zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
                    {
                        zone.Factions.Clear();
                        zone.AccessTypeFactions = station.DoBlacklist ? MySafeZoneAccess.Blacklist : MySafeZoneAccess.Whitelist;

                        foreach (var s in station.Whitelist)
                        {
                            if (s.Contains("LIST:"))
                            {
                                //split the list
                                //     Log.Info("Is list");
                                var temp = s.Split(':')[1];
                                //    Log.Info(temp);
                                foreach (var tag in from list in ConfigProvider.Whitelist.whitelist where list.ListName == temp from tag in list.FactionTags where MySession.Static.Factions.TryGetFactionByTag(tag) != null select tag)
                                {
                                    //        Log.Info("fac isnt null");
                                    zone.Factions.Add(MySession.Static.Factions.TryGetFactionByTag(tag));
                                }
                            }
                            else if (s.Contains("FAC:"))
                            {
                                var temp = s.Split(':')[1];
                                if (MySession.Static.Factions.TryGetFactionByTag(temp) != null)
                                {
                                    zone.Factions.Add(MySession.Static.Factions.TryGetFactionByTag(temp));
                                }
                            }
                        }
                        MySessionComponentSafeZones.RequestUpdateSafeZone((MyObjectBuilder_SafeZone)zone.GetObjectBuilder());
                    }
                }
            });
        }

        public static Random rnd = new Random();
        public override void Update()
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

            if (DateTime.Now >= NextFileRefresh)
            {
                NextFileRefresh = DateTime.Now.AddMinutes(15);
                Log.Info("Loading stuff for CrunchEcon");
                try
                {

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
                    ConfigProvider.LoadAllGridSales();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading grid sales " + ex.ToString());


                }
                try
                {
                    ConfigProvider.LoadAllSellOffers();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Sell offers " + ex.ToString());


                }
                try
                {
                    ConfigProvider.LoadStations();
                    DoStationShit();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Stations " + ex.ToString());


                }
                try
                {
                    ConfigProvider.LoadAllBuyOrders();
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading Buy Orders " + ex.ToString());


                }


            }

            if (ticks % 256 == 0 && TorchState == TorchSessionState.Loaded)
            {

                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
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

                    if (config == null || !config.SurveyContractsEnabled) continue;
                    var data = PlayerStorageProvider.GetPlayerData(player.Id.SteamId);
                    GenerateNewSurveyMission(data, player);
                }

            }

            if (ticks % 64 == 0 && TorchState == TorchSessionState.Loaded)
            {
                PlayerStorageProvider.SaveContracts();
                DateTime now = DateTime.Now;
                foreach (var station in stations)
                {
                    //first check if its any, then we can load the grid to do the editing
                    try
                    {
                        if (now >= station.nextCraftRefresh && station.EnableStationCrafting)
                        {
                            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) != null)
                            {
                                if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is MyCubeGrid grid)
                                {
                                    List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                    foreach (CraftedItem item in station.CraftableItems)
                                    {
                                        double yeet = rnd.NextDouble();
                                        if (yeet <= item.chanceToCraft)
                                        {

                                            var comps = new Dictionary<MyDefinitionId, int>();
                                            inventories.AddRange(GetInventories(grid, station));
                                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.typeid, item.subtypeid, out MyDefinitionId id))
                                            {
                                                foreach (RecipeItem recipe in item.RequriedItems)
                                                {
                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.typeid, recipe.subtypeid, out MyDefinitionId id2))
                                                    {
                                                        comps.Add(id2, recipe.amount);
                                                    }
                                                }
                                                if (ConsumeComponents(inventories, comps, 0l))
                                                {
                                                    SpawnItems(grid, id, item.amountPerCraft, station);
                                                    comps.Clear();
                                                    inventories.Clear();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            station.nextCraftRefresh = DateTime.Now.AddSeconds(station.SecondsBetweenCrafting);
                        }
                        if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh)
                        {
                            MyGps gps = station.getGPS();
                            Boolean AddSellTime = false;
                            Boolean AddBuyTime = false;
                            bool checkLocation = false;
                            if (!station.WorldName.Equals("default"))
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

        public void SaveStation(Stations Station)
        {
            ConfigProvider.SaveStation(Station);
        }

        public void ClearStoreOfPlayersSellingOrders(MyStoreBlock store)
        {
            var yeet = store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Order).ToList();
            foreach (var item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public void ClearStoreOfPlayersBuyingOffers(MyStoreBlock store)
        {

            var yeet = store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Offer).ToList();
            foreach (var item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public static List<Stations> stations = new List<Stations>();

        public static Dictionary<String, List<BuyOrder>> buyOrders = new Dictionary<string, List<BuyOrder>>();
        public static Dictionary<String, List<SellOffer>> sellOffers = new Dictionary<string, List<SellOffer>>();
        public static FileUtils utils = new FileUtils();

        public static MyGps ParseGPS(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            var matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

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


        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            TorchState = state;
            if (!CrunchEconCore.config.PluginEnabled && config != null)
            {
                return;
            }

            if (state != TorchSessionState.Loaded) return;

            if (config.SetMinPricesTo1)
            {
                sessionManager.AddOverrideMod(2825413709);
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    if (def is MyComponentDefinition definition)
                    {
                        definition.MinimalPricePerUnit = 1;
                    }
                    if (def is MyPhysicalItemDefinition itemDefinition)
                    {
                        itemDefinition.MinimalPricePerUnit = 1;
                    }
                }
            }
            session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += Login;

            if (session.Managers.GetManager<PluginManager>().Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"), out ITorchPlugin All))
            {
                var alli = All.GetType().Assembly.GetType("AlliancesPlugin.AlliancePlugin");
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
                ConfigProvider.LoadAllGridSales();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading grid sales " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadAllSellOffers();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Sell offers " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadStations();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Stations " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadAllBuyOrders();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Buy Orders " + ex.ToString());


            }
        }

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
