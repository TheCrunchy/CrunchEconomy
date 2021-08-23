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
        public bool SpawnItems(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount, Stations station)
        {
            if (grid != null)
            {


                foreach (var block in grid.GetFatBlocks())
                {


                    if (block.DisplayNameText != null && !block.DisplayNameText.Equals(station.CargoName) && block.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
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

    

        public static Dictionary<ulong, PlayerData> playerData = new Dictionary<ulong, PlayerData>();
        public static Boolean AlliancePluginEnabled = false;
        //i should really split this into multiple methods so i dont have one huge method for everything
        public static Dictionary<ulong, MiningContract> miningSave = new Dictionary<ulong, MiningContract>();
        public void DoMiningContractDelivery(MyPlayer player)
        {
            if (player.GetPosition() != null)
            {

                // GenerateNewMiningContracts(player);

                try
                {
                    if (config.MiningContractsEnabled)
                    {
                        MyPlayer playerOnline = player;
                        if (player.Character != null && player?.Controller.ControlledEntity is MyCockpit controller)
                        {
                            MyCubeGrid grid = controller.CubeGrid;
                            if (playerData.TryGetValue(player.Id.SteamId, out PlayerData data))
                            {

                                foreach (MiningContract contract in data.getMiningContracts().Values)
                                {
                                    if (!String.IsNullOrEmpty(contract.OreSubType) && contract.minedAmount >= contract.amountToMine)
                                    {
                                        Vector3D coords = contract.getCoords();
                                        float distance = Vector3.Distance(coords, controller.PositionComp.GetPosition());
                                        if (distance <= 500)
                                        {
                                            Dictionary<MyDefinitionId, int> itemsToRemove = new Dictionary<MyDefinitionId, int>();

                                            if (MyDefinitionId.TryParse("MyObjectBuilder_Ore/" + contract.OreSubType, out MyDefinitionId id))
                                            {
                                                itemsToRemove.Add(id, contract.amountToMine);

                                            }

                                            List<VRage.Game.ModAPI.IMyInventory> inventories = GetInventoriesForContract(grid);

                                            if (FacUtils.IsOwnerOrFactionOwned(grid, player.Identity.IdentityId, true))
                                            {
                                                if (ConsumeComponents(inventories, itemsToRemove, player.Id.SteamId))
                                                {
                                                

                                                    data.MiningReputation += contract.reputation;
                                                    data.MiningContracts.Remove(contract.ContractId);
                                                    if (data.MiningReputation >= 100)
                                                    {
                                                        contract.contractPrice += long.Parse((contract.contractPrice * 0.05f).ToString());
                                                    }
                                                    if (data.MiningReputation >= 200)
                                                    {
                                                        contract.contractPrice += long.Parse((contract.contractPrice * 0.05f).ToString());
                                                    }
                                                    if (data.MiningReputation >= 300)
                                                    {
                                                        contract.contractPrice += long.Parse((contract.contractPrice * 0.05f).ToString());
                                                    }
                                                    if (data.MiningReputation >= 400)
                                                    {
                                                        contract.contractPrice += long.Parse((contract.contractPrice * 0.05f).ToString());
                                                    }
                                                    if (data.MiningReputation >= 500)
                                                    {
                                                        contract.contractPrice += long.Parse((contract.contractPrice * 0.05f).ToString());
                                                    }
                                                    if (AlliancePluginEnabled)
                                                    {
                                                        //patch into alliances and process the payment there
                                                    }

                                                    //   EconUtils.addMoney(player.Identity.IdentityId, contract.contractPrice);
                                                    SendMessage("Big Boss Dave", "Good job, heres the money", Color.Gold, (long)player.Id.SteamId);

                                                    contract.PlayerSteamId = player.Id.SteamId;
                                                    contract.AmountPaid = contract.contractPrice;

                                                    FileUtils utils = new FileUtils();
                                                    contract.status = ContractStatus.Completed;
                                                    File.Delete(path + "//PlayerData//Mining//InProgress//" + contract.ContractId + ".xml");
                                                    CrunchEconCore.miningSave.Remove(player.Id.SteamId);
                                                    CrunchEconCore.miningSave.Add(player.Id.SteamId, contract);

                                                    utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                                                    //SAVE THE PLAYER DATA WITH INCREASED REPUTATION

                                                }
                                            }
                                        }
                                    }
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
        //public void DoHaulingContractDelivery(MyPlayer player)
        //{
        //    if (config.HaulingContractsEnabled)
        //    {
        //        try
        //        {
        //            //do if config enabled
        //            if (HaulingCore.activeContracts.TryGetValue(player.Id.SteamId, out HaulingContract haulContract))
        //            {
        //                MyPlayer playerOnline = player;
        //                if (player.Character != null && player?.Controller.ControlledEntity is MyCockpit controller)
        //                {
        //                    MyCubeGrid grid = controller.CubeGrid;
        //                    Vector3D coords = haulContract.GetDeliveryLocation().Coords;
        //                    float distance = Vector3.Distance(coords, controller.PositionComp.GetPosition());
        //                    if (distance <= 500)
        //                    {
        //                        Dictionary<MyDefinitionId, int> itemsToRemove = new Dictionary<MyDefinitionId, int>();

        //                        int pay = 0;
        //                        //calculate the pay since we only show the player the minimum they can get, this could be removed if the pay is made part of the contract
        //                        //when its generated and stored in the db, reputation when completed could give a bonus percent
        //                        foreach (ContractItems item in haulContract.getItemsInContract())
        //                        {
        //                            if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.ItemType, item.SubType, out MyDefinitionId id))
        //                            {
        //                                itemsToRemove.Add(id, item.AmountToDeliver);
        //                                pay += item.AmountToDeliver * item.GetPrice();
        //                            }
        //                        }

        //                        List<VRage.Game.ModAPI.IMyInventory> inventories = ShipyardCommands.GetInventories(grid);

        //                        if (FacUtils.IsOwnerOrFactionOwned(grid, player.Identity.IdentityId, true))
        //                        {
        //                            if (ShipyardCommands.ConsumeComponents(inventories, itemsToRemove, player.Id.SteamId))
        //                            {
        //                                if (AlliancePlugin.TaxesToBeProcessed.ContainsKey(player.Identity.IdentityId))
        //                                {

        //                                    AlliancePlugin.TaxesToBeProcessed[player.Identity.IdentityId] += pay;
        //                                }
        //                                else
        //                                {
        //                                    AlliancePlugin.TaxesToBeProcessed.Add(player.Identity.IdentityId, pay);

        //                                }
        //                                EconUtils.addMoney(player.Identity.IdentityId, pay);
        //                                ShipyardCommands.SendMessage("Big Boss Dave", "Good job, heres the money", Color.Gold, (long)player.Id.SteamId);
        //                                HaulingCore.RemoveContract(player.Id.SteamId, player.Identity.IdentityId);
        //                                File.Delete(AlliancePlugin.path + "//HaulingStuff//PlayerData//" + player.Id.SteamId + ".json");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error(ex);
        //        }
        //    }
        //}

        public override void Update()
        {
            ticks++;
            if (paused)
            {
                return;
            }

            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
            }
            if (DateTime.Now >= NextFileRefresh)
            {
                NextFileRefresh = DateTime.Now.AddMinutes(1);
                Log.Info("Loading stuff for CrunchEcon");
                try
                {
                    ContractUtils.LoadAllContracts();
                    ContractUtils.LoadDeliveryLocations();
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



            if (ticks % 32 == 0 && TorchState == TorchSessionState.Loaded)
            {
                foreach (KeyValuePair<ulong, MiningContract> keys in miningSave)
                {
                    //  utils.SaveMiningData(AlliancePlugin.path + "//MiningStuff//PlayerData//" + keys.Key + ".xml", keys.Value);
                    MiningContract contract = keys.Value;
                    switch (contract.status)
                    {
                        case ContractStatus.InProgress:
                            utils.WriteToXmlFile<MiningContract>(path + "//PlayerData//Mining//InProgress//" + contract.ContractId + ".xml", keys.Value);
                            break;
                        case ContractStatus.Completed:
                            utils.WriteToXmlFile<MiningContract>(path + "//PlayerData//Mining//Completed//" + contract.ContractId + ".xml", keys.Value);
                            break;
                        case ContractStatus.Failed:
                            utils.WriteToXmlFile<MiningContract>(path + "//PlayerData//Mining//Failed//" + contract.ContractId + ".xml", keys.Value);
                            break;
                    }

                }
                miningSave.Clear();
                DateTime now = DateTime.Now;
                foreach (Stations station in stations)
                {
                    //first check if its any, then we can load the grid to do the editing
                    try
                    {

                        if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh || now >= station.nextGridInventoryClear)
                        {
                            MyGps gps = station.getGPS();
                            BoundingSphereD sphere = new BoundingSphereD(gps.Coords, 200);

                            foreach (MyCubeGrid grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>())
                            {
                                if (station.DoPeriodGridClearing && now >= station.nextGridInventoryClear)
                                {
                                    station.nextGridInventoryClear = now.AddSeconds(station.SecondsBetweenClearEntireInventory);
                                    ClearInventories(grid, station);
                                    SaveStation(station);
                                }
                                foreach (MyStoreBlock store in grid.GetFatBlocks().OfType<MyStoreBlock>())
                                {
                                    if (store.GetOwnerFactionTag().Equals(station.OwnerFactionTag))
                                    {

                                        if (now >= station.nextSellRefresh && station.DoSellOffers)
                                        {
                                            station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                                            if (sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                            {


                                                ClearStoreOfPlayersBuyingOffers(store);
                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                inventories.AddRange(GetInventories(grid, station));
                                                Random rnd = new Random();

                                                foreach (SellOffer offer in offers)
                                                {
                                                    double chance = rnd.NextDouble();
                                                    if (chance > offer.chance)
                                                    {
                                                        continue;
                                                    }
                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + offer.typeId, offer.subtypeId, out MyDefinitionId id))
                                                    {

                                                        int hasAmount = CountComponents(inventories, id).ToIntSafe();
                                                        if (hasAmount > 0)
                                                        {
                                                            if (hasAmount < offer.SpawnIfCargoLessThan && offer.SpawnItemsIfNeeded)
                                                            {
                                                                int amountToSpawn = offer.SpawnIfCargoLessThan - hasAmount;

                                                                //spawn items
                                                                SpawnItems(grid, id, (MyFixedPoint)amountToSpawn, station);
                                                                hasAmount += amountToSpawn;
                                                            }

                                                            SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);


                                                            rnd = new Random();

                                                            int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);

                                                            MyStoreItemData item = new MyStoreItemData(itemId, hasAmount, price, null, null);
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
                                                                SpawnItems(grid, id, (MyFixedPoint)offer.SpawnIfCargoLessThan, station);

                                                                SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);


                                                                rnd = new Random();

                                                                int price = rnd.Next((int)offer.minPrice, (int)offer.maxPrice);

                                                                MyStoreItemData item = new MyStoreItemData(itemId, offer.SpawnIfCargoLessThan, price, null, null);
                                                                MyStoreInsertResults result = store.InsertOffer(item, out long notUsingThis);
                                                                if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum || result == MyStoreInsertResults.Fail_StoreLimitReached || result == MyStoreInsertResults.Error)
                                                                {
                                                                    Log.Error("Unable to insert this offer into store " + offer.typeId + " " + offer.subtypeId + " at station " + station.Name + " " + result.ToString());
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }



                                        }
                                        if (now >= station.nextBuyRefresh && station.DoBuyOrders)
                                        {
                                            station.nextBuyRefresh = now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);

                                            if (buyOrders.TryGetValue(store.DisplayNameText, out List<BuyOrder> orders))
                                            {

                                                station.nextSellRefresh = now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                                                ClearStoreOfPlayersSellingOrders(store);
                                                List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                                                inventories.AddRange(GetInventories(grid, station));
                                                Random rnd = new Random();
                                                foreach (BuyOrder order in orders)
                                                {

                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + order.typeId, order.subtypeId, out MyDefinitionId id))
                                                    {

                                                        SerializableDefinitionId itemId = new SerializableDefinitionId(id.TypeId, order.subtypeId);

                                                        rnd = new Random();
                                                        double chance = rnd.NextDouble();
                                                        if (chance <= order.chance)
                                                        {
                                                            int price = rnd.Next((int)order.minPrice, (int)order.maxPrice);
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

                                            }
                                        }

                                    }
                                }
                                SaveStation(station);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SaveStation(station);
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

        public List<Stations> stations = new List<Stations>();

        public static Dictionary<String, List<BuyOrder>> buyOrders = new Dictionary<string, List<BuyOrder>>();
        public static Dictionary<String, List<SellOffer>> sellOffers = new Dictionary<string, List<SellOffer>>();
        public static FileUtils utils = new FileUtils();
        public void LoadAllStations()
        {
            stations.Clear();
            foreach (String s in Directory.GetFiles(path + "//Stations//"))
            {


                try
                {
                    Stations stat = utils.ReadFromXmlFile<Stations>(s);
                    if (stat.Enabled)
                    {
                        stations.Add(stat);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error loading stations " + s + " " + ex.ToString());
                    throw;
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
            if (state == TorchSessionState.Loaded)
            {
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
            if (!Directory.Exists(path + "//Stations//"))
            {
                Directory.CreateDirectory(path + "//Stations//");
                Stations station = new Stations();
                station.Enabled = false;
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
            if (!Directory.Exists(path + "//ContractConfigs//Mining//"))
            {
                GeneratedContract contract = new GeneratedContract();
         
                Directory.CreateDirectory(path + "//ContractConfigs//Mining//");
                utils.WriteToXmlFile<GeneratedContract>(path + "//ContractConfigs//Mining//Example.xml", contract);
            }
            if (!Directory.Exists(path + "//ContractConfigs//Hauling//"))
            {
                Directory.CreateDirectory(path + "//ContractConfigs//Hauling//");
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
            TorchBase = Torch;
        }

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
