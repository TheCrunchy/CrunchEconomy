using CrunchEconomy.Contracts;
using NLog;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRageMath;
using static CrunchEconomy.Contracts.GeneratedContract;

namespace CrunchEconomy
{
    [PatchShim]
    public static class MyStorePatch
    {
        //why read only file git ffs
        internal static readonly MethodInfo logupdate =
         typeof(MyStoreBlock).GetMethod("SendSellItemResult", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(string), typeof(long), typeof(int), typeof(MyStoreSellItemResults) }, null) ??
         throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatchLog =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethodSell), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo logupdate2 =
  typeof(MyStoreBlock).GetMethod("SendBuyItemResult", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(long), typeof(string), typeof(long), typeof(int), typeof(MyStoreBuyItemResults) }, null) ??
  throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatchLog2 =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethodBuy), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo update =
            typeof(MyStoreBlock).GetMethod("BuyFromPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
             typeof(MyStorePatch).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
             throw new Exception("Failed to find patch method");


        internal static readonly MethodInfo updateTwo =
       typeof(MyStoreBlock).GetMethod("SellToPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
       throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatchTwo =
             typeof(MyStorePatch).GetMethod(nameof(StorePatchMethodTwo), BindingFlags.Static | BindingFlags.Public) ??
             throw new Exception("Failed to find patch method");
        public static Logger log = LogManager.GetLogger("Stores");
        public static void ApplyLogging()
        {

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--)
            {

                var rule = rules[i];

                if (rule.LoggerNamePattern == "Stores")
                    rules.RemoveAt(i);
            }



            var logTarget = new FileTarget
            {
                FileName = "Logs/Stores-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".txt",
                Layout = "${var:logStamp} ${var:logContent}"
            };

            var logRule = new LoggingRule("Stores", LogLevel.Debug, logTarget)
            {
                Final = true
            };

            rules.Insert(0, logRule);

            LogManager.Configuration.Reload();
        }


        public static void Patch(PatchContext ctx)
        {

            ApplyLogging();

            ctx.GetPattern(logupdate).Suffixes.Add(storePatchLog);
            ctx.GetPattern(logupdate2).Suffixes.Add(storePatchLog2);

            ctx.GetPattern(update).Prefixes.Add(storePatch);
            ctx.GetPattern(updateTwo).Prefixes.Add(storePatchTwo);

        }
        //        log.Info("SteamId:" + player.Id.SteamId + ",action:sold,Amount:" + amount + ",TypeId:" + myStoreItem.Item.Value.TypeIdString + ",SubTypeId:" + myStoreItem.Item.Value.SubtypeName + ",TotalMoney:" + myStoreItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag());
        ////patch this to see if sell was success SendSellItemResult
        ///
        public static Dictionary<long, string> PossibleLogs = new Dictionary<long, string>();

        public static void StorePatchMethodSell(long id, string name, long price, int amount, MyStoreSellItemResults result)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PatchesEnabled)
            {
                return;
            }
            //  AlliancePlugin.Log.Info("sold to store");
            if (result == MyStoreSellItemResults.Success && PossibleLogs.ContainsKey(id))
            {
                log.Info(PossibleLogs[id]);
            }
            PossibleLogs.Remove(id);
            return;
        }

        public static void StorePatchMethodBuy(long id, string name, long price, int amount, MyStoreBuyItemResults result)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PatchesEnabled)
            {
                return;
            }
            //  AlliancePlugin.Log.Info("sold to store");
            if (result == MyStoreBuyItemResults.Success && PossibleLogs.ContainsKey(id))
            {
                log.Info(PossibleLogs[id]);
            }
            PossibleLogs.Remove(id);
            return;
        }
        public static Boolean StorePatchMethodTwo(long id, int amount, long sourceEntityId, MyPlayer player, MyStoreBlock __instance)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PatchesEnabled)
            {
                return true;
            }

            if (!(__instance is MyStoreBlock store)) return true;
            var myStoreItem = store.PlayerItems.FirstOrDefault(playerItem => playerItem.Id == id);

            if (myStoreItem == null)
            {
                return false;
            }

            if (!PossibleLogs.ContainsKey(id))
            {
                PossibleLogs.Add(id, "SteamId:" + player.Id.SteamId + ",action:sold,Amount:" + amount + ",TypeId:" + myStoreItem.Item.Value.TypeIdString + ",SubTypeId:" + myStoreItem.Item.Value.SubtypeName + ",TotalMoney:" + myStoreItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag() + ",ModifierName:notimplemented" + ",GridName:" + store.CubeGrid.DisplayName);
            }

            return true;
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

        public static Dictionary<ulong, Dictionary<string, DateTime>> MiningCooldowns = new Dictionary<ulong, Dictionary<string, DateTime>>();
        public static Dictionary<ulong, Dictionary<string, DateTime>> HaulingCooldowns = new Dictionary<ulong, Dictionary<string, DateTime>>();
        public static bool StorePatchMethod(long id, int amount, long targetEntityId, MyPlayer player, MyAccountInfo playerAccountInfo, MyStoreBlock __instance)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PatchesEnabled)
            {
                return true;
            }
            //  CrunchEconCore.Log.Info("bruh");
            if (!(__instance is MyStoreBlock store)) return true;
            var storeItem = store.PlayerItems.FirstOrDefault(playerItem => playerItem.Id == id);
            if (storeItem == null)
            {
                return true;
            }

            if (!PossibleLogs.ContainsKey(id))
            {
                PossibleLogs.Add(id, "SteamId:" + player.Id.SteamId + ",action:bought,Amount:" + amount + ",TypeId:" + storeItem.Item.Value.TypeIdString + ",SubTypeId:" + storeItem.Item.Value.SubtypeName + ",TotalMoney:" + storeItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag() + ",ModifierName:notimplemented" + ",GridName:" + store.CubeGrid.DisplayName);
            }
            //this code is awful
            Sandbox.Game.Entities.MyEntities.TryGetEntityById(targetEntityId, out var entity, false);
            if (entity is MyCharacter && player.Character != entity || entity is MyCubeBlock myCubeBlock && !myCubeBlock.CubeGrid.BigOwners.Contains(player.Identity.IdentityId))
            {
                return true;
            }

            MyInventory inventory;
            if (!entity.TryGetInventory(out inventory)) return true;
            if (!CrunchEconCore.ConfigProvider.GetGridsForSale().ContainsKey(storeItem.Item.Value.SubtypeName) && !CrunchEconCore.sellOffers.ContainsKey(store.DisplayNameText))
            {
                //  CrunchEconCore.Log.Info("bruh");
                return true;
            }

            if (amount > storeItem.Amount)
            {
                return true;
            }

            var totalPrice = (long)storeItem.PricePerUnit * (long)amount;
            if (totalPrice > playerAccountInfo.Balance)
                return true;
            if (totalPrice < 0L)
            {
                return true;
            }

            //   CrunchEconCore.Log.Info("grids?");
            if (CrunchEconCore.ConfigProvider.GetGridsForSale().TryGetValue(storeItem.Item.Value.SubtypeName, out GridSale sale))
            {
                if (amount > 1)
                {
                    var m = new DialogMessage("Shop Error", "Can only buy one ship at a time");
                    ModCommunication.SendMessageTo(m, player.Id.SteamId);
                    return false;
                }

                if (!store.GetOwnerFactionTag().Equals(sale.OwnerFactionTag) ||
                    !store.DisplayNameText.Equals(sale.StoreBlockName)) return true;

                if (!File.Exists(CrunchEconCore.path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc"))
                    return true;
                var idd = player.Id.SteamId;
                if (sale.GiveOwnerShipToNPC)
                {
                    idd = (ulong)store.OwnerId;
                }
                Vector3 Position = player.Character.PositionComp.GetPosition();
                var random = new Random();

                Position.Add(new Vector3(random.Next(1000, 2000), random.Next(1000, 2000), random.Next(1000, 2000)));
                if (!GridManager.LoadGrid(CrunchEconCore.path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc", Position, false, idd, storeItem.Item.Value.SubtypeName, false))
                {
                    CrunchEconCore.Log.Info(player.Id.SteamId + " failure when purchasing grid");
                    var m = new DialogMessage("Shop Error", "Unable to paste the grid, is it obstructed?");
                    ModCommunication.SendMessageTo(m, player.Id.SteamId);
                    return false;
                }

                if (sale.PayPercentageToPlayer)
                {
                    var ident = GetIdentityByNameOrId(sale.steamId.ToString());
                    if (ident != null)
                    {
                        EconUtils.addMoney(ident.IdentityId, Convert.ToInt64(storeItem.PricePerUnit * sale.percentage));
                    }
                }
                CrunchEconCore.Log.Info(player.Id.SteamId + " purchased grid " + sale.ExportedGridName);
            }
            else
            {
                if (!CrunchEconCore.sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                    return true;

                foreach (var offer in offers)
                {
                    if (offer.BuyingGivesGPS)
                    {
                        var pickFrom = new List<MyGps>();
                        var gpscol = MySession.Static.Gpss;
                        var playerList = new List<IMyGps>();
                        gpscol.GetGpsList(player.Identity.IdentityId, playerList);
                        foreach (var gps in from s in offer.gpsToPickFrom select CrunchEconCore.ParseGPS(s) into gps where gps != null where playerList.Any(gp => gp.Coords != gps.Coords) select gps)
                        {
                            var myGps = gps;
                            myGps.AlwaysVisible = true;
                            myGps.ShowOnHud = true;
                            gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref myGps);
                            return true;
                        }
                    }

                    if (!offer.BuyingGivesHaulingContract && !offer.BuyingGivesMiningContract) continue;

                    //   CrunchEconCore.Log.Info("has a contract");
                    if (!store.GetOwnerFactionTag().Equals(offer.IfGivesContractNPCTag))
                    {
                        continue;
                    }
                    if (amount > 1)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                        var m = new DialogMessage("Shop Error", "", sb.ToString());
                        ModCommunication.SendMessageTo(m, player.Id.SteamId);
                        return false;
                    }
                    //  CrunchEconCore.Log.Info("is the faction tag");
                    //  CrunchEconCore.Log.Info(storeItem.Item.Value.TypeIdString + " " + storeItem.Item.Value.SubtypeName) ;
                    if (!offer.typeId.Equals(storeItem.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "")) ||
                        !offer.subtypeId.Equals(storeItem.Item.Value.SubtypeName)) continue;

                    CrunchEconCore.PlayerStorageProvider.playerData.TryGetValue(player.Id.SteamId, out var data);
                    if (data == null)
                    {
                        //       CrunchEconCore.Log.Info("Data was null");
                        data = new PlayerData
                        {
                            steamId = player.Id.SteamId
                        };
                    }
                    if (offer.BuyingGivesMiningContract && CrunchEconCore.config.MiningContractsEnabled)
                    {
                        var max = 1;
                        if (data.MiningReputation >= 250)
                        {
                            max++;
                        }
                        //   CrunchEconCore.Log.Info(data.getMiningContracts().Count);
                        DialogMessage message;
                        if (data.GetMiningContracts().Count < max)
                        {
                            if (amount > max || data.GetMiningContracts().Count + amount > max)
                            {

                                var sb = new StringBuilder();
                                sb.AppendLine($"Cannot purchase more than {max} contracts!");
                                sb.AppendLine($"Maximum you can purchase is {(max - data.GetMiningContracts().Count).ToString()}");
                                message = new DialogMessage("Shop Error", "", sb.ToString());
                                ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                return false;
                            }
                            if (ContractUtils.newContracts.TryGetValue(offer.ContractName, out var contract))
                            {


                                var temp = ContractUtils.GeneratedToPlayer(contract);
                                var random = new Random();
                                var locations = new List<StationDelivery>();
                                var temporaryStations = new Dictionary<string, Stations>();
                                var picked = false;
                                foreach (var del in contract.StationsToDeliverTo)
                                {

                                    if (del.Name.Equals("TAKEN"))
                                    {
                                        var gps = new MyGps
                                        {
                                            Coords = store.CubeGrid.PositionComp.GetPosition()
                                        };
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = store.CubeGrid.EntityId;

                                        temp.PlayerSteamId = player.Id.SteamId;
                                        picked = true;
                                        break;
                                    }

                                    if (!(random.Next(0, 100) <= del.chance)) continue;
                                    foreach (var stat in CrunchEconCore.stations.Where(stat => stat.Name.Equals(del.Name)))
                                    {
                                        if (!temporaryStations.ContainsKey(del.Name))
                                        {
                                            temporaryStations.Add(del.Name, stat);
                                        }
                                        locations.Add(del);
                                    }
                                }
                                if (!picked)
                                {
                                    if (locations.Count == 1)
                                    {
                                        var gps = temporaryStations[locations[0].Name].getGPS();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[0].Name].StationEntityId;
                                        temp.PlayerSteamId = player.Id.SteamId;
                                        var distance = Vector3.Distance(player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                    else
                                    {
                                        var r = random.Next(locations.Count);
                                        var gps = temporaryStations[locations[r].Name].getGPS();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[r].Name].StationEntityId;
                                        temp.PlayerSteamId = player.Id.SteamId;

                                        var distance = Vector3.Distance(player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                }

                                temp.PlayerSteamId = player.Id.SteamId;

                                if (MiningCooldowns.TryGetValue(player.Id.SteamId, out var cd))
                                {
                                    if (cd.TryGetValue(temp.SubType, out var time))
                                    {
                                        if (DateTime.Now <= time)
                                        {
                                            var sb = new StringBuilder();
                                            var diff = time.Subtract(DateTime.Now);
                                            var time2 = $"{diff.Hours} Hours {diff.Minutes} Minutes {diff.Seconds} Seconds";
                                            sb.AppendLine("Cannot purchase another contract of this type for " + time2);
                                            var m = new DialogMessage("Shop Error", "", sb.ToString());
                                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                            return false;
                                        }
                                    }
                                    MiningCooldowns[player.Id.SteamId].Remove(temp.SubType);
                                    MiningCooldowns[player.Id.SteamId].Add(temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds));
                                }
                                else
                                {
                                    var temporary = new Dictionary<string, DateTime> { { temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds) } };
                                    MiningCooldowns.Add(player.Id.SteamId, temporary);
                                }

                                data.addMining(temp);
                                //do this the lazy way instead of checking then setting by the key
                                CrunchEconCore.PlayerStorageProvider.playerData.Remove(player.Id.SteamId);
                                CrunchEconCore.PlayerStorageProvider.playerData.Add(player.Id.SteamId, data);


                                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(temp);

                                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
                                var contractDetails = new StringBuilder();
                                foreach (var c in data.GetMiningContracts().Values)
                                {
                                    if (c.minedAmount >= c.amountToMineOrDeliver)
                                    {
                                        c.DoPlayerGps(player.Identity.IdentityId);
                                        contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                                    }
                                    else
                                    {
                                        contractDetails.AppendLine("Mine " + c.SubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                                    }
                                    contractDetails.AppendLine("");
                                }
                                contractDetails.AppendLine("View all contract details with !contract info");
                                var m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                ModCommunication.SendMessageTo(m2, player.Id.SteamId);
                                return true;
                            }


                            message = new DialogMessage("Shop Error", "Failed to find contract.");
                            ModCommunication.SendMessageTo(message, player.Id.SteamId);
                            return false;

                        }


                        message = new DialogMessage("Shop Error", "", "You already have the maximum amount of mining contracts. View them with !contract info");
                        ModCommunication.SendMessageTo(message, player.Id.SteamId);
                        return false;

                    }

                    if (!offer.BuyingGivesHaulingContract || !CrunchEconCore.config.HaulingContractsEnabled)
                        continue;
                    {
                        if (amount > 1)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                            var m = new DialogMessage("Shop Error", "", sb.ToString());
                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                            return false;
                        }
                        var max = 1;
                        if (data.HaulingReputation >= 250)
                        {
                            max++;
                        }
                        //   CrunchEconCore.Log.Info(data.getHaulingContracts().Count);
                        if (data.GetHaulingContracts().Count < max)
                        {
                            DialogMessage m;
                            if (amount > max || data.GetHaulingContracts().Count + amount > max)
                            {

                                var sb = new StringBuilder();
                                sb.AppendLine("Cannot purchase more than " + max + " contracts!");
                                sb.AppendLine("Maximum you can purchase is " + (max - data.GetHaulingContracts().Count).ToString());
                                m = new DialogMessage("Shop Error", "", sb.ToString());
                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                return false;
                            }
                            if (ContractUtils.newContracts.TryGetValue(offer.ContractName, out var contract))
                            {



                                var temp = ContractUtils.GeneratedToPlayer(contract);
                                if (HaulingCooldowns.TryGetValue(player.Id.SteamId, out var cd))
                                {
                                    if (cd.TryGetValue(temp.SubType, out var time))
                                    {
                                        if (DateTime.Now <= time)
                                        {
                                            var sb = new StringBuilder();
                                            var diff = time.Subtract(DateTime.Now);
                                            var time2 = $"{diff.Hours} Hours {diff.Minutes} Minutes {diff.Seconds} Seconds";
                                            sb.AppendLine("Cannot purchase another contract of this type for " + time2);

                                            var message = new DialogMessage("Shop Error", "", sb.ToString());
                                            ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                            return false;
                                        }
                                    }

                                    HaulingCooldowns[player.Id.SteamId].Remove(temp.SubType);
                                    HaulingCooldowns[player.Id.SteamId].Add(temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds));
                                }
                                else
                                {
                                    var temporary = new Dictionary<string, DateTime> { { temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds) } };
                                    HaulingCooldowns.Add(player.Id.SteamId, temporary);
                                }
                                var random = new Random();
                                var locations = new List<StationDelivery>();
                                var temporaryStations = new Dictionary<string, Stations>();
                                var picked = false;
                                foreach (var del in contract.StationsToDeliverTo)
                                {

                                    if (del.Name.Equals("TAKEN"))
                                    {
                                        var gps = new MyGps
                                        {
                                            Coords = store.CubeGrid.PositionComp.GetPosition()
                                        };
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = store.CubeGrid.EntityId;
                                        picked = true;
                                        temp.PlayerSteamId = player.Id.SteamId;
                                        break;
                                    }

                                    if (!(random.Next(0, 100) <= del.chance)) continue;
                                    foreach (var stat in CrunchEconCore.stations.Where(stat => stat.Name.Equals(del.Name)).Where(stat => !temporaryStations.ContainsKey(del.Name)))
                                    {
                                        temporaryStations.Add(del.Name, stat);
                                        locations.Add(del);
                                    }
                                }
                                if (!picked)
                                {
                                    if (locations.Count == 1)
                                    {
                                        var gps = temporaryStations[locations[0].Name].getGPS();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[0].Name].StationEntityId;
                                        temp.PlayerSteamId = player.Id.SteamId;
                                        var distance = Vector3.Distance(player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                    else
                                    {
                                        var r = random.Next(0, locations.Count);
                                        var gps = temporaryStations[locations[r].Name].getGPS();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[r].Name].StationEntityId;
                                        temp.PlayerSteamId = player.Id.SteamId;

                                        var distance = Vector3.Distance(player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                }
                                //  return locations[r];



                                data.addHauling(temp);
                                temp.PlayerSteamId = player.Id.SteamId;

                                if (contract.SpawnItemsInPlayerInvent)
                                {
                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + temp.TypeIfHauling + "/" + temp.SubType, out var itemId))
                                    {
                                        var itemType = new MyInventoryItemFilter(itemId.TypeId + "/" + itemId.SubtypeName).ItemType;
                                        if (!player.Character.GetInventory().CanItemsBeAdded(temp.amountToMineOrDeliver, itemType))
                                        {
                                            var sb = new StringBuilder();
                                            sb.AppendLine("Cannot add items to character inventory!");
                                            var message = new DialogMessage("Shop Error", "", sb.ToString());
                                            ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        var sb = new StringBuilder();
                                        sb.AppendLine("Cannot add items to character inventory, definition id does not parse!");
                                        var message = new DialogMessage("Shop Error", "", sb.ToString());
                                        ModCommunication.SendMessageTo(message, player.Id.SteamId);
                                        return false;
                                    }
                                    player.Character.GetInventory().AddItems(temp.amountToMineOrDeliver, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId));
                                }
                                //do this the lazy way instead of checking then setting by the key
                                CrunchEconCore.PlayerStorageProvider.playerData.Remove(player.Id.SteamId);
                                CrunchEconCore.PlayerStorageProvider.playerData.Add(player.Id.SteamId, data);

                                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(temp);
                                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);

                                var contractDetails = new StringBuilder();
                                foreach (var c in data.GetHaulingContracts().Values)
                                {
                                    c.DoPlayerGps(player.Identity.IdentityId);
                                    contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                    contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                                    contractDetails.AppendLine("Distance bonus :" + String.Format("{0:n0}", c.DistanceBonus) + " SC.");

                                    contractDetails.AppendLine("");
                                }
                                contractDetails.AppendLine("View all contract details with !contract info");
                                var m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                ModCommunication.SendMessageTo(m2, player.Id.SteamId);
                                return true;
                            }

                            
                            m = new DialogMessage("Shop Error", "Failed to find contract.");
                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                            return false;
                            
                        }
                        else
                        {
                            var m = new DialogMessage("Shop Error", "", "You already have the maximum amount of Hauling contracts. View them with !contract info");
                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                            return false;
                        }
                    }

                    //DialogMessage m3 = new DialogMessage("Shop Error", "", "Something failed here to provide a contract.");
                    //ModCommunication.SendMessageTo(m3, player.Id.SteamId);
                    //return false;

                }
            }
            return true;

        }
    }
}


