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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchEconomy
{
    [PatchShim]
    public static class MyStorePatch
    {
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
            if (__instance is MyStoreBlock store)
            {
                MyStoreItem myStoreItem = (MyStoreItem)null;

                foreach (MyStoreItem playerItem in store.PlayerItems)
                {
                    if (playerItem.Id == id)
                    {
                        myStoreItem = playerItem;
                        break;
                    }
                }
                if (myStoreItem == null)
                {
                    return false;
                }

                if (!PossibleLogs.ContainsKey(id))
                {
                    PossibleLogs.Add(id, "SteamId:" + player.Id.SteamId + ",action:sold,Amount:" + amount + ",TypeId:" + myStoreItem.Item.Value.TypeIdString + ",SubTypeId:" + myStoreItem.Item.Value.SubtypeName + ",TotalMoney:" + myStoreItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag());
                }

                if (CrunchEconCore.playerData.TryGetValue(player.Id.SteamId, out PlayerData data))
                {
                    if (store.DisplayNameText != null && CrunchEconCore.buyOrders.TryGetValue(store.DisplayNameText, out List<BuyOrder> orders))
                    {


                        foreach (BuyOrder order in orders)
                        {
                            if (order.SellingThisCancelsContract && store.GetOwnerFactionTag().Equals(order.FactionTagOwnerForCancelling) && order.typeId.Equals(myStoreItem.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "")) && order.subtypeId.Equals(myStoreItem.Item.Value.SubtypeName))
                            {
                                //this should cancel

                                if (ContractUtils.newContracts.TryGetValue(order.ContractToCancel, out GeneratedContract gen))
                                {
                                    if (amount > 1)
                                    {
                                        CrunchEconCore.SendMessage("Boss Dave", "Cannot cancel more than one contract a time!", Color.Red, (long)player.Id.SteamId);
                                        return false;
                                    }
                                    List<Contract> maybeCancel = new List<Contract>();
                                    if (gen.type == ContractType.Mining)
                                    {
                                        foreach (Contract contract in data.getMiningContracts().Values)
                                        {
                                            if (contract.ContractName.Equals(gen.Name))
                                            {
                                                maybeCancel.Add(contract);
                                            }
                                        }
                                    }
                                    if (gen.type == ContractType.Hauling)
                                    {
                                        foreach (Contract contract in data.getHaulingContracts().Values)
                                        {
                                            if (contract.ContractName.Equals(gen.Name))
                                            {
                                                maybeCancel.Add(contract);
                                            }
                                        }
                                    }
                                    Contract cancel = null;
                                    List<Contract> cancelIfNoOthers = new List<Contract>();
                                    foreach (Contract contract in maybeCancel)
                                    {
                                        if (cancel == null)
                                        {
                                            cancel = contract;
                                            continue;
                                        }
                                        if (contract.type == ContractType.Mining)
                                        {
                                            if (contract.minedAmount >= contract.amountToMineOrDeliver)
                                            {
                                                cancelIfNoOthers.Add(contract);
                                            }
                                            else
                                            {
                                                if (contract.minedAmount < cancel.minedAmount && contract.amountToMineOrDeliver < cancel.amountToMineOrDeliver)
                                                {
                                                    cancel = contract;
                                                }
                                            }
                                        }
                                    }
                                    if (cancel != null)
                                    {
                                        data.getMiningContracts().Remove(cancel.ContractId);
                                        data.MiningContracts.Remove(cancel.ContractId);
                                        data.MiningReputation -= cancel.reputation * 2;
                                        CrunchEconCore.playerData[player.Id.SteamId] = data;
                                        StringBuilder sb = new StringBuilder();
                                        sb.AppendLine("Cancelled contract");
                                        sb.AppendLine("Mine " + cancel.SubType + " Ore " + String.Format("{0:n0}", cancel.minedAmount) + " / " + String.Format("{0:n0}", cancel.amountToMineOrDeliver));

                                        sb.AppendLine("Reputation lowered by " + cancel.reputation * 2);
                                        sb.AppendLine();
                                        sb.AppendLine("Remaining Contracts");
                                        sb.AppendLine();
                                        foreach (Contract c in data.getMiningContracts().Values)
                                        {

                                            if (c.minedAmount >= c.amountToMineOrDeliver)
                                            {
                                                c.DoPlayerGps(player.Identity.IdentityId);
                                                sb.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                                sb.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                                            }
                                            else
                                            {
                                                sb.AppendLine("Mine " + c.SubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                                sb.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");

                                            }
                                            sb.AppendLine("");
                                        }
                                        DialogMessage m = new DialogMessage("Contract", "Cancel", sb.ToString());
                                        ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                        cancel.status = ContractStatus.Failed;
                                        File.Delete(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + cancel.ContractId + ".xml");
                                        CrunchEconCore.ContractSave.Remove(cancel.ContractId);
                                        CrunchEconCore.ContractSave.Add(cancel.ContractId, cancel);
                                        CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);
                                        return true;
                                    }
                                    else
                                    {

                                        return false;
                                    }
                                }
                                else
                                {
                                    //cancel hauling here
                                }
                            }
                        }

                    }
                }
            }
            return true;
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


        public static Boolean StorePatchMethod(long id,
      int amount,
      long targetEntityId,
      MyPlayer player,
      MyAccountInfo playerAccountInfo, MyStoreBlock __instance)
        {
            //  CrunchEconCore.Log.Info("bruh");
            if (__instance is MyStoreBlock store)
            {

                MyEntity entity = (MyEntity)null;
                if (!Sandbox.Game.Entities.MyEntities.TryGetEntityById(targetEntityId, out entity, false))
                {

                }

                else if (entity is MyCubeBlock myCubeBlock && !myCubeBlock.CubeGrid.BigOwners.Contains(player.Identity.IdentityId))
                {

                }

                else if (entity is MyCharacter && player.Character != entity)
                {

                }
                else
                {
                    MyInventory inventory;
                    if (!entity.TryGetInventory(out inventory))
                    {

                    }
                    else
                    {
                        MyStoreItem storeItem = (MyStoreItem)null;
                        foreach (MyStoreItem playerItem in store.PlayerItems)
                        {
                            if (playerItem.Id == id)
                            {
                                storeItem = playerItem;
                                break;
                            }
                        }
                        if (storeItem == null)
                        {

                            return true;
                        }
                        if (!PossibleLogs.ContainsKey(id))
                        {
                            PossibleLogs.Add(id, "SteamId:" + player.Id.SteamId + ",action:bought,Amount:" + amount + ",TypeId:" + storeItem.Item.Value.TypeIdString + ",SubTypeId:" + storeItem.Item.Value.SubtypeName + ",TotalMoney:" + storeItem.PricePerUnit * (long)amount + ",GridId:" + store.CubeGrid.EntityId + ",FacTag:" + store.GetOwnerFactionTag());
                        }
                        if (!CrunchEconCore.gridsForSale.ContainsKey(storeItem.Item.Value.SubtypeName) && !CrunchEconCore.sellOffers.ContainsKey(store.DisplayNameText))
                        {
                            //  CrunchEconCore.Log.Info("bruh");
                            return true;
                        }
                        else if (amount > storeItem.Amount)
                        {
                            return true;
                        }
                        else
                        {
                            long totalPrice = (long)storeItem.PricePerUnit * (long)amount;
                            if (totalPrice > playerAccountInfo.Balance)
                                return true;
                            else if (totalPrice < 0L)
                            {
                                return true;
                            }
                            else
                            {

                                //   CrunchEconCore.Log.Info("grids?");
                                if (CrunchEconCore.gridsForSale.TryGetValue(storeItem.Item.Value.SubtypeName, out GridSale sale))
                                {
                                    if (amount > 1)
                                    {
                                        DialogMessage m = new DialogMessage("Shop Error", "Can only buy one ship at a time");
                                        ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                        return false;
                                    }
                                    if (store.GetOwnerFactionTag().Equals(sale.OwnerFactionTag) && store.DisplayNameText.Equals(sale.StoreBlockName))
                                    {
                                        if (File.Exists(CrunchEconCore.path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc"))
                                        {
                                            ulong idd = player.Id.SteamId;
                                            if (sale.GiveOwnerShipToNPC)
                                            {
                                                idd = (ulong)store.OwnerId;
                                            }
                                            Vector3 Position = player.Character.PositionComp.GetPosition();
                                            Random random = new Random();

                                            Position.Add(new Vector3(random.Next(1000, 2000), random.Next(1000, 2000), random.Next(1000, 2000)));
                                            if (!GridManager.LoadGrid(CrunchEconCore.path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc", Position, false, idd, storeItem.Item.Value.SubtypeName, false))
                                            {
                                                CrunchEconCore.Log.Info(player.Id.SteamId + " failure when purchasing grid");
                                                DialogMessage m = new DialogMessage("Shop Error", "Unable to paste the grid, is it obstructed?");
                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                return false;
                                            }
                                            else
                                            {
                                                if (sale.PayPercentageToPlayer)
                                                {
                                                    MyIdentity ident = GetIdentityByNameOrId(sale.steamId.ToString());
                                                    if (ident != null)
                                                    {
                                                        EconUtils.addMoney(ident.IdentityId, Convert.ToInt64(storeItem.PricePerUnit * sale.percentage));
                                                    }
                                                }
                                                CrunchEconCore.Log.Info(player.Id.SteamId + " purchased grid " + sale.ExportedGridName);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //     CrunchEconCore.Log.Info("not a grid sale");
                                    if (CrunchEconCore.sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                    {
                                        //     CrunchEconCore.Log.Info("1");
                                        //improve this, store the offers by their object builder in a dictionary
                                 
                                        foreach (SellOffer offer in offers)
                                        {//
                                       
                                            if (offer.BuyingGivesHaulingContract || offer.BuyingGivesMiningContract)
                                            {
                                                //   CrunchEconCore.Log.Info("has a contract");
                                                if (!store.GetOwnerFactionTag().Equals(offer.IfGivesContractNPCTag))
                                                {
                                                    //  CrunchEconCore.Log.Info("not the faction tag");

                                                    continue;
                                                }
                                                if (amount > 1)
                                                {
                                                    StringBuilder sb = new StringBuilder();
                                                    sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                                                    DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                    ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                    return false;
                                                }
                                                //  CrunchEconCore.Log.Info("is the faction tag");
                                                //  CrunchEconCore.Log.Info(storeItem.Item.Value.TypeIdString + " " + storeItem.Item.Value.SubtypeName) ;
                                                if (offer.typeId.Equals(storeItem.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "")) && offer.subtypeId.Equals(storeItem.Item.Value.SubtypeName))
                                                {

                                                    CrunchEconCore.playerData.TryGetValue(player.Id.SteamId, out PlayerData data);
                                                    if (data == null)
                                                    {
                                                        //       CrunchEconCore.Log.Info("Data was null");
                                                        data = new PlayerData();
                                                        data.steamId = player.Id.SteamId;
                                                    }
                                                    if (offer.BuyingGivesMiningContract && CrunchEconCore.config.MiningContractsEnabled)
                                                    {
                                                        int max = 1;
                                                        if (data.MiningReputation >= 250)
                                                        {
                                                            max++;
                                                        }
                                                        //   CrunchEconCore.Log.Info(data.getMiningContracts().Count);
                                                        if (data.getMiningContracts().Count < max)
                                                        {
                                                            if (amount > max || data.getMiningContracts().Count + amount > max)
                                                            {

                                                                StringBuilder sb = new StringBuilder();
                                                                sb.AppendLine("Cannot purchase more than " + max + " contracts!");
                                                                sb.AppendLine("Maximum you can purchase is " + (max - data.getMiningContracts().Count).ToString());
                                                                DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                return false;
                                                            }
                                                            if (ContractUtils.newContracts.TryGetValue(offer.ContractName, out GeneratedContract contract))
                                                            {



                                                                Contract temp = ContractUtils.GeneratedToPlayer(contract);

                                                                MyGps gps = new MyGps();
                                                                gps.Coords = store.CubeGrid.PositionComp.GetPosition();
                                                                temp.DeliveryLocation = gps.ToString();
                                                                temp.StationEntityId = store.CubeGrid.EntityId;
                                                                data.addMining(temp);
                                                                temp.PlayerSteamId = player.Id.SteamId;


                                                                //do this the lazy way instead of checking then setting by the key
                                                                CrunchEconCore.playerData.Remove(player.Id.SteamId);
                                                                CrunchEconCore.playerData.Add(player.Id.SteamId, data);


                                                                CrunchEconCore.ContractSave.Remove(temp.ContractId);
                                                                CrunchEconCore.ContractSave.Add(temp.ContractId, temp);

                                                                CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);




                                                                StringBuilder contractDetails = new StringBuilder();
                                                                foreach (Contract c in data.getMiningContracts().Values)
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
                                                                DialogMessage m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                                                ModCommunication.SendMessageTo(m2, player.Id.SteamId);
                                                                return true;
                                                            }
                                                            else
                                                            {
                                                                DialogMessage m = new DialogMessage("Shop Error", "Failed to find contract.");
                                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                return false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DialogMessage m = new DialogMessage("Shop Error", "", "You already have the maximum amount of mining contracts. View them with !contract info");
                                                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                            return false;
                                                        }
                                                    }

                                                    if (offer.BuyingGivesHaulingContract && CrunchEconCore.config.HaulingContractsEnabled)
                                                    {
                                                        if (amount > 1)
                                                        {
                                                            StringBuilder sb = new StringBuilder();
                                                            sb.AppendLine("Cannot purchase more than 1 contract in single purchase!");

                                                            DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                            return false;
                                                        }
                                                        int max = 1;
                                                        if (data.HaulingReputation >= 250)
                                                        {
                                                            max++;
                                                        }
                                                        //   CrunchEconCore.Log.Info(data.getHaulingContracts().Count);
                                                        if (data.getHaulingContracts().Count < max)
                                                        {
                                                            if (amount > max || data.getHaulingContracts().Count + amount > max)
                                                            {

                                                                StringBuilder sb = new StringBuilder();
                                                                sb.AppendLine("Cannot purchase more than " + max + " contracts!");
                                                                sb.AppendLine("Maximum you can purchase is " + (max - data.getHaulingContracts().Count).ToString());
                                                                DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                return false;
                                                            }
                                                            if (ContractUtils.newContracts.TryGetValue(offer.ContractName, out GeneratedContract contract))
                                                            {



                                                                Contract temp = ContractUtils.GeneratedToPlayer(contract);
                                                                Stations station = ContractUtils.GetDeliveryLocation(temp);
                                                                MyGps delivery = station.getGPS();
                                                                temp.StationEntityId = station.StationEntityId;
                                                                float distance = Vector3.Distance(player.GetPosition(), delivery.Coords);
                                                                long deliveryBonus = Convert.ToInt64(distance / 100000) * 50000;
                                                                temp.DeliveryLocation = delivery.ToString();
                                                                temp.DistanceBonus = deliveryBonus;
                                                                data.addHauling(temp);
                                                                temp.PlayerSteamId = player.Id.SteamId;

                                                                if (contract.SpawnItemsInPlayerInvent)
                                                                {
                                                                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + contract.type + "/" + contract.SubType, out MyDefinitionId itemId))
                                                                    {
                                                                        MyItemType itemType = new MyInventoryItemFilter(itemId.TypeId + "/" + itemId.SubtypeName).ItemType;
                                                                        if (!player.Character.GetInventory().CanItemsBeAdded(temp.amountToMineOrDeliver, itemType))
                                                                        {
                                                                            StringBuilder sb = new StringBuilder();
                                                                            sb.AppendLine("Cannot add items to character inventory!");
                                                                            DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                            return false;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        StringBuilder sb = new StringBuilder();
                                                                        sb.AppendLine("Cannot add items to character inventory, definition id does not parse!");
                                                                        DialogMessage m = new DialogMessage("Shop Error", "", sb.ToString());
                                                                        ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                        return false;
                                                                    }
                                                                    player.Character.GetInventory().AddItems(temp.amountToMineOrDeliver, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId));
                                                                }
                                                                //do this the lazy way instead of checking then setting by the key
                                                                CrunchEconCore.playerData.Remove(player.Id.SteamId);
                                                                CrunchEconCore.playerData.Add(player.Id.SteamId, data);


                                                                CrunchEconCore.ContractSave.Remove(temp.ContractId);
                                                                CrunchEconCore.ContractSave.Add(temp.ContractId, temp);

                                                                CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);




                                                                StringBuilder contractDetails = new StringBuilder();
                                                                foreach (Contract c in data.getHaulingContracts().Values)
                                                                {
                                                                    c.DoPlayerGps(player.Identity.IdentityId);
                                                                    contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                                                                    contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                                                                    contractDetails.AppendLine("Distance bonus :" + String.Format("{0:n0}", c.DistanceBonus) + " SC.");

                                                                    contractDetails.AppendLine("");
                                                                }
                                                                contractDetails.AppendLine("View all contract details with !contract info");
                                                                DialogMessage m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                                                                ModCommunication.SendMessageTo(m2, player.Id.SteamId);
                                                                return true;
                                                            }
                                                            else
                                                            {
                                                                DialogMessage m = new DialogMessage("Shop Error", "Failed to find contract.");
                                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                                return false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            DialogMessage m = new DialogMessage("Shop Error", "", "You already have the maximum amount of Hauling contracts. View them with !contract info");
                                                            ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                            return false;
                                                        }
                                                    }

                                                }
                                                //DialogMessage m3 = new DialogMessage("Shop Error", "", "Something failed here to provide a contract.");
                                                //ModCommunication.SendMessageTo(m3, player.Id.SteamId);
                                                //return false;
                                            }
                                        }

                                    }
                                }


                            }
                            return true;
                        }
                    }
                }
            }

            return true;
        }
    }
}


