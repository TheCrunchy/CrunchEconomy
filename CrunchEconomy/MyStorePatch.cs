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
using CrunchEconomy.Helpers;
using CrunchEconomy.Station_Stuff;
using CrunchEconomy.Station_Stuff.Objects;
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
        public static Logger Log = LogManager.GetLogger("Stores");
        public static void ApplyLogging()
        {

            var rules = LogManager.Configuration.LoggingRules;

            for (var i = rules.Count - 1; i >= 0; i--)
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


        public static void Patch(PatchContext Ctx)
        {

            ApplyLogging();

            Ctx.GetPattern(logupdate).Suffixes.Add(storePatchLog);
            Ctx.GetPattern(logupdate2).Suffixes.Add(storePatchLog2);

            Ctx.GetPattern(update).Prefixes.Add(storePatch);
            Ctx.GetPattern(updateTwo).Prefixes.Add(storePatchTwo);

        }
        //        log.Info("SteamId:" + player.Id.SteamId + ",action:sold,Amount:" + amount + ",TypeId:" + myStoreItem.Item.Value.TypeIdString + ",SubTypeId:" + myStoreItem.Item.Value.SubtypeName + ",TotalMoney:" + myStoreItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag());
        ////patch this to see if sell was success SendSellItemResult
        ///
        public static Dictionary<long, string> PossibleLogs = new Dictionary<long, string>();

        public static void StorePatchMethodSell(long Id, string Name, long Price, int Amount, MyStoreSellItemResults Result)
        {
            if (CrunchEconCore.Config != null && !CrunchEconCore.Config.PatchesEnabled)
            {
                return;
            }
            //  AlliancePlugin.Log.Info("sold to store");
            if (Result == MyStoreSellItemResults.Success && PossibleLogs.ContainsKey(Id))
            {
                Log.Info(PossibleLogs[Id]);
            }
            PossibleLogs.Remove(Id);
            return;
        }

        public static void StorePatchMethodBuy(long Id, string Name, long Price, int Amount, MyStoreBuyItemResults Result)
        {
            if (CrunchEconCore.Config != null && !CrunchEconCore.Config.PatchesEnabled)
            {
                return;
            }
            //  AlliancePlugin.Log.Info("sold to store");
            if (Result == MyStoreBuyItemResults.Success && PossibleLogs.ContainsKey(Id))
            {
                Log.Info(PossibleLogs[Id]);
            }
            PossibleLogs.Remove(Id);
            return;
        }
        public static Boolean StorePatchMethodTwo(long Id, int Amount, long SourceEntityId, MyPlayer Player, MyStoreBlock Instance)
        {
            if (CrunchEconCore.Config != null && !CrunchEconCore.Config.PatchesEnabled)
            {
                return true;
            }

            if (!(Instance is MyStoreBlock store)) return true;
            var myStoreItem = store.PlayerItems.FirstOrDefault(PlayerItem => PlayerItem.Id == Id);

            if (myStoreItem == null)
            {
                return false;
            }

            if (!PossibleLogs.ContainsKey(Id))
            {
                PossibleLogs.Add(Id, "SteamId:" + Player.Id.SteamId + ",action:sold,Amount:" + Amount + ",TypeId:" + myStoreItem.Item.Value.TypeIdString + ",SubTypeId:" + myStoreItem.Item.Value.SubtypeName + ",TotalMoney:" + myStoreItem.PricePerUnit * (long)Amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag() + ",ModifierName:notimplemented" + ",GridName:" + store.CubeGrid.DisplayName);
            }

            return true;
        }
        public static MyIdentity GetIdentityByNameOrId(string PlayerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == PlayerNameOrSteamId)
                    return identity;
                if (!ulong.TryParse(PlayerNameOrSteamId, out var steamId)) continue;
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
        public static bool StorePatchMethod(long Id, int Amount, long TargetEntityId, MyPlayer Player, MyAccountInfo PlayerAccountInfo, MyStoreBlock Instance)
        {
            if (CrunchEconCore.Config != null && !CrunchEconCore.Config.PatchesEnabled)
            {
                return true;
            }
            //  CrunchEconCore.Log.Info("bruh");
            if (!(Instance is MyStoreBlock store)) return true;
            var storeItem = store.PlayerItems.FirstOrDefault(PlayerItem => PlayerItem.Id == Id);
            if (storeItem == null)
            {
                return true;
            }

            if (!PossibleLogs.ContainsKey(Id))
            {
                PossibleLogs.Add(Id, "SteamId:" + Player.Id.SteamId + ",action:bought,Amount:" + Amount + ",TypeId:" + storeItem.Item.Value.TypeIdString + ",SubTypeId:" + storeItem.Item.Value.SubtypeName + ",TotalMoney:" + storeItem.PricePerUnit * (long)Amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag() + ",ModifierName:notimplemented" + ",GridName:" + store.CubeGrid.DisplayName);
            }
            //this code is awful
            Sandbox.Game.Entities.MyEntities.TryGetEntityById(TargetEntityId, out var entity, false);
            if (entity is MyCharacter && Player.Character != entity || entity is MyCubeBlock myCubeBlock && !myCubeBlock.CubeGrid.BigOwners.Contains(Player.Identity.IdentityId))
            {
                return true;
            }

            MyInventory inventory;
            if (!entity.TryGetInventory(out inventory)) return true;
            if (!CrunchEconCore.ConfigProvider.GetGridsForSale().ContainsKey(storeItem.Item.Value.SubtypeName) && !CrunchEconCore.ConfigProvider.GetSellOffers().ContainsKey(store.DisplayNameText))
            {
                //  CrunchEconCore.Log.Info("bruh");
                return true;
            }

            if (Amount > storeItem.Amount)
            {
                return true;
            }

            var totalPrice = (long)storeItem.PricePerUnit * (long)Amount;
            if (totalPrice > PlayerAccountInfo.Balance)
                return true;
            if (totalPrice < 0L)
            {
                return true;
            }

            //   CrunchEconCore.Log.Info("grids?");
            if (CrunchEconCore.ConfigProvider.GetGridsForSale().TryGetValue(storeItem.Item.Value.SubtypeName, out var sale))
            {
                if (Amount > 1)
                {
                    var m = new DialogMessage("Shop Error", "Can only buy one ship at a time");
                    ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                    return false;
                }

                if (!store.GetOwnerFactionTag().Equals(sale.OwnerFactionTag) ||
                    !store.DisplayNameText.Equals(sale.StoreBlockName)) return true;

                if (!File.Exists(CrunchEconCore.Path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc"))
                    return true;
                var idd = Player.Id.SteamId;
                if (sale.GiveOwnerShipToNpc)
                {
                    idd = (ulong)store.OwnerId;
                }
                Vector3 position = Player.Character.PositionComp.GetPosition();
                var random = new Random();

                position.Add(new Vector3(random.Next(1000, 2000), random.Next(1000, 2000), random.Next(1000, 2000)));
                if (!GridManager.LoadGrid(CrunchEconCore.Path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc", position, false, idd, storeItem.Item.Value.SubtypeName, false))
                {
                    CrunchEconCore.Log.Info(Player.Id.SteamId + " failure when purchasing grid");
                    var m = new DialogMessage("Shop Error", "Unable to paste the grid, is it obstructed?");
                    ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                    return false;
                }

                if (sale.PayPercentageToPlayer)
                {
                    var ident = GetIdentityByNameOrId(sale.SteamId.ToString());
                    if (ident != null)
                    {
                        EconUtils.AddMoney(ident.IdentityId, Convert.ToInt64(storeItem.PricePerUnit * sale.Percentage));
                    }
                }
                CrunchEconCore.Log.Info(Player.Id.SteamId + " purchased grid " + sale.ExportedGridName);
            }
            else
            {
                if (!CrunchEconCore.ConfigProvider.GetSellOffers().TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                    return true;

                foreach (var offer in offers)
                {
                    if (offer.BuyingGivesGps)
                    {
                        var pickFrom = new List<MyGps>();
                        var gpscol = MySession.Static.Gpss;
                        var playerList = new List<IMyGps>();
                        gpscol.GetGpsList(Player.Identity.IdentityId, playerList);
                        foreach (var gps in from s in offer.GpsToPickFrom select GpsHelper.ParseGps(s) into gps where gps != null where playerList.Any(Gp => Gp.Coords != gps.Coords) select gps)
                        {
                            var myGps = gps;
                            myGps.AlwaysVisible = true;
                            myGps.ShowOnHud = true;
                            gpscol.SendAddGpsRequest(Player.Identity.IdentityId, ref myGps);
                            return true;
                        }
                    }

                    if (!offer.BuyingGivesHaulingContract && !offer.BuyingGivesMiningContract) continue;

                    //   CrunchEconCore.Log.Info("has a contract");
                    if (!store.GetOwnerFactionTag().Equals(offer.IfGivesContractNpcTag))
                    {
                        continue;
                    }
                    if (Amount > 1)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                        var m = new DialogMessage("Shop Error", "", sb.ToString());
                        ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                        return false;
                    }
                    //  CrunchEconCore.Log.Info("is the faction tag");
                    //  CrunchEconCore.Log.Info(storeItem.Item.Value.TypeIdString + " " + storeItem.Item.Value.SubtypeName) ;
                    if (!offer.TypeId.Equals(storeItem.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "")) ||
                        !offer.SubtypeId.Equals(storeItem.Item.Value.SubtypeName)) continue;

                    CrunchEconCore.PlayerStorageProvider.PlayerData.TryGetValue(Player.Id.SteamId, out var data);
                    if (data == null)
                    {
                        //       CrunchEconCore.Log.Info("Data was null");
                        data = new PlayerData
                        {
                            SteamId = Player.Id.SteamId
                        };
                    }
                    if (offer.BuyingGivesMiningContract && CrunchEconCore.Config.MiningContractsEnabled)
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
                            if (Amount > max || data.GetMiningContracts().Count + Amount > max)
                            {

                                var sb = new StringBuilder();
                                sb.AppendLine($"Cannot purchase more than {max} contracts!");
                                sb.AppendLine($"Maximum you can purchase is {(max - data.GetMiningContracts().Count).ToString()}");
                                message = new DialogMessage("Shop Error", "", sb.ToString());
                                ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                                return false;
                            }
                            if (ContractUtils.NewContracts.TryGetValue(offer.ContractName, out var contract))
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

                                        temp.PlayerSteamId = Player.Id.SteamId;
                                        picked = true;
                                        break;
                                    }

                                    if (!(random.Next(0, 100) <= del.Chance)) continue;
                                    foreach (var stat in CrunchEconCore.Stations.Where(Stat => Stat.Name.Equals(del.Name)))
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
                                        var gps = temporaryStations[locations[0].Name].GetGps();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[0].Name].StationEntityId;
                                        temp.PlayerSteamId = Player.Id.SteamId;
                                        var distance = Vector3.Distance(Player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                    else
                                    {
                                        var r = random.Next(locations.Count);
                                        var gps = temporaryStations[locations[r].Name].GetGps();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[r].Name].StationEntityId;
                                        temp.PlayerSteamId = Player.Id.SteamId;

                                        var distance = Vector3.Distance(Player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                }

                                temp.PlayerSteamId = Player.Id.SteamId;

                                if (MiningCooldowns.TryGetValue(Player.Id.SteamId, out var cd))
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
                                            ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                                            return false;
                                        }
                                    }
                                    MiningCooldowns[Player.Id.SteamId].Remove(temp.SubType);
                                    MiningCooldowns[Player.Id.SteamId].Add(temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds));
                                }
                                else
                                {
                                    var temporary = new Dictionary<string, DateTime> { { temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds) } };
                                    MiningCooldowns.Add(Player.Id.SteamId, temporary);
                                }

                                data.AddMining(temp);
                                //do this the lazy way instead of checking then setting by the key
                                CrunchEconCore.PlayerStorageProvider.PlayerData.Remove(Player.Id.SteamId);
                                CrunchEconCore.PlayerStorageProvider.PlayerData.Add(Player.Id.SteamId, data);


                                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(temp);

                                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
                                var contractDetails = new StringBuilder();
                                foreach (var c in data.GetMiningContracts().Values)
                                {
                                    if (c.MinedAmount >= c.AmountToMineOrDeliver)
                                    {
                                        c.DoPlayerGps(Player.Identity.IdentityId);
                                        contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.AmountToMineOrDeliver));
                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.ContractPrice) + " SC. and " + c.Reputation + " reputation gain.");
                                    }
                                    else
                                    {
                                        contractDetails.AppendLine("Mine " + c.SubType + " Ore " + String.Format("{0:n0}", c.MinedAmount) + " / " + String.Format("{0:n0}", c.AmountToMineOrDeliver));
                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.ContractPrice) + " SC. and " + c.Reputation + " reputation gain.");
                                    }
                                    contractDetails.AppendLine("");
                                }
                                contractDetails.AppendLine("View all contract details with !contract info");
                                var m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                ModCommunication.SendMessageTo(m2, Player.Id.SteamId);
                                return true;
                            }


                            message = new DialogMessage("Shop Error", "Failed to find contract.");
                            ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                            return false;

                        }


                        message = new DialogMessage("Shop Error", "", "You already have the maximum amount of mining contracts. View them with !contract info");
                        ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                        return false;

                    }

                    if (!offer.BuyingGivesHaulingContract || !CrunchEconCore.Config.HaulingContractsEnabled)
                        continue;
                    {
                        if (Amount > 1)
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                            var m = new DialogMessage("Shop Error", "", sb.ToString());
                            ModCommunication.SendMessageTo(m, Player.Id.SteamId);
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
                            if (Amount > max || data.GetHaulingContracts().Count + Amount > max)
                            {

                                var sb = new StringBuilder();
                                sb.AppendLine("Cannot purchase more than " + max + " contracts!");
                                sb.AppendLine("Maximum you can purchase is " + (max - data.GetHaulingContracts().Count).ToString());
                                m = new DialogMessage("Shop Error", "", sb.ToString());
                                ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                                return false;
                            }
                            if (ContractUtils.NewContracts.TryGetValue(offer.ContractName, out var contract))
                            {



                                var temp = ContractUtils.GeneratedToPlayer(contract);
                                if (HaulingCooldowns.TryGetValue(Player.Id.SteamId, out var cd))
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
                                            ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                                            return false;
                                        }
                                    }

                                    HaulingCooldowns[Player.Id.SteamId].Remove(temp.SubType);
                                    HaulingCooldowns[Player.Id.SteamId].Add(temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds));
                                }
                                else
                                {
                                    var temporary = new Dictionary<string, DateTime> { { temp.SubType, DateTime.Now.AddSeconds(temp.CooldownInSeconds) } };
                                    HaulingCooldowns.Add(Player.Id.SteamId, temporary);
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
                                        temp.PlayerSteamId = Player.Id.SteamId;
                                        break;
                                    }

                                    if (!(random.Next(0, 100) <= del.Chance)) continue;
                                    foreach (var stat in CrunchEconCore.Stations.Where(Stat => Stat.Name.Equals(del.Name)).Where(Stat => !temporaryStations.ContainsKey(del.Name)))
                                    {
                                        temporaryStations.Add(del.Name, stat);
                                        locations.Add(del);
                                    }
                                }
                                if (!picked)
                                {
                                    if (locations.Count == 1)
                                    {
                                        var gps = temporaryStations[locations[0].Name].GetGps();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[0].Name].StationEntityId;
                                        temp.PlayerSteamId = Player.Id.SteamId;
                                        var distance = Vector3.Distance(Player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                    else
                                    {
                                        var r = random.Next(0, locations.Count);
                                        var gps = temporaryStations[locations[r].Name].GetGps();
                                        temp.DeliveryLocation = gps.ToString();
                                        temp.StationEntityId = temporaryStations[locations[r].Name].StationEntityId;
                                        temp.PlayerSteamId = Player.Id.SteamId;

                                        var distance = Vector3.Distance(Player.GetPosition(), gps.Coords);
                                        var deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;

                                        temp.DistanceBonus = deliveryBonus;
                                    }
                                }
                                //  return locations[r];



                                data.AddHauling(temp);
                                temp.PlayerSteamId = Player.Id.SteamId;

                                if (contract.SpawnItemsInPlayerInvent)
                                {
                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + temp.TypeIfHauling + "/" + temp.SubType, out var itemId))
                                    {
                                        var itemType = new MyInventoryItemFilter(itemId.TypeId + "/" + itemId.SubtypeName).ItemType;
                                        if (!Player.Character.GetInventory().CanItemsBeAdded(temp.AmountToMineOrDeliver, itemType))
                                        {
                                            var sb = new StringBuilder();
                                            sb.AppendLine("Cannot add items to character inventory!");
                                            var message = new DialogMessage("Shop Error", "", sb.ToString());
                                            ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        var sb = new StringBuilder();
                                        sb.AppendLine("Cannot add items to character inventory, definition id does not parse!");
                                        var message = new DialogMessage("Shop Error", "", sb.ToString());
                                        ModCommunication.SendMessageTo(message, Player.Id.SteamId);
                                        return false;
                                    }
                                    Player.Character.GetInventory().AddItems(temp.AmountToMineOrDeliver, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId));
                                }
                                //do this the lazy way instead of checking then setting by the key
                                CrunchEconCore.PlayerStorageProvider.PlayerData.Remove(Player.Id.SteamId);
                                CrunchEconCore.PlayerStorageProvider.PlayerData.Add(Player.Id.SteamId, data);

                                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(temp);
                                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);

                                var contractDetails = new StringBuilder();
                                foreach (var c in data.GetHaulingContracts().Values)
                                {
                                    c.DoPlayerGps(Player.Identity.IdentityId);
                                    contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.AmountToMineOrDeliver));
                                    contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.ContractPrice) + " SC. and " + c.Reputation + " reputation gain.");
                                    contractDetails.AppendLine("Distance bonus :" + String.Format("{0:n0}", c.DistanceBonus) + " SC.");

                                    contractDetails.AppendLine("");
                                }
                                contractDetails.AppendLine("View all contract details with !contract info");
                                var m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                ModCommunication.SendMessageTo(m2, Player.Id.SteamId);
                                return true;
                            }

                            
                            m = new DialogMessage("Shop Error", "Failed to find contract.");
                            ModCommunication.SendMessageTo(m, Player.Id.SteamId);
                            return false;
                            
                        }
                        else
                        {
                            var m = new DialogMessage("Shop Error", "", "You already have the maximum amount of Hauling contracts. View them with !contract info");
                            ModCommunication.SendMessageTo(m, Player.Id.SteamId);
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


