using CrunchEconomy.Contracts;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
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
using VRage.Game;
using VRage.Game.Entity;

namespace CrunchEconomy
{
    [PatchShim]
    public static class MyStorePatch
    {


        internal static readonly MethodInfo update =
            typeof(MyStoreBlock).GetMethod("BuyFromPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
             typeof(MyStorePatch).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
             throw new Exception("Failed to find patch method");


        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(storePatch);

        }




        public static Boolean StorePatchMethod(long id,
      int amount,
      long targetEntityId,
      MyPlayer player,
      MyAccountInfo playerAccountInfo, MyStoreBlock __instance)
        {
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

                        if (!CrunchEconCore.gridsForSale.ContainsKey(storeItem.Item.Value.SubtypeName) && !CrunchEconCore.sellOffers.ContainsKey(store.DisplayNameText))
                        {
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
                                            if (!GridManager.LoadGrid(CrunchEconCore.path + "//GridSelling//Grids//" + sale.ExportedGridName + ".sbc", player.Character.PositionComp.GetPosition(), false, idd, storeItem.Item.Value.SubtypeName, false))
                                            {
                                                CrunchEconCore.Log.Info(player.Id.SteamId + " failure when purchasing grid");
                                                DialogMessage m = new DialogMessage("Shop Error", "Unable to paste the grid, is it obstructed?");
                                                ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                return false;
                                            }
                                            else
                                            {
                                                CrunchEconCore.Log.Info(player.Id.SteamId + " purchased grid " + sale.ExportedGridName);
                                            }
                                        }
                                    }
                                }
                                else
                                {

                                    if (CrunchEconCore.sellOffers.TryGetValue(store.DisplayNameText, out List<SellOffer> offers))
                                    {
                                        //  CrunchEconCore.Log.Info("1");
                                        foreach (SellOffer offer in offers)
                                        {//

                                            if (offer.BuyingGivesHaulingContract || offer.BuyingGivesMiningContract)
                                            {
                                                //  CrunchEconCore.Log.Info("has a contract");
                                                if (!store.GetOwnerFactionTag().Equals(offer.IfGivesContractNPCTag))
                                                {
                                                    //     CrunchEconCore.Log.Info("not the faction tag");
                                                    continue;
                                                }
                                                // CrunchEconCore.Log.Info("is the faction tag");
                                                //  CrunchEconCore.Log.Info(storeItem.Item.Value.TypeIdString + " " + storeItem.Item.Value.SubtypeName) ;
                                                if (offer.typeId.Equals(storeItem.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "")) && offer.subtypeId.Equals(storeItem.Item.Value.SubtypeName))
                                                {

                                                    CrunchEconCore.playerData.TryGetValue(player.Id.SteamId, out PlayerData data);
                                                    if (data == null)
                                                    {
                                                        //   CrunchEconCore.Log.Info("Data was null");
                                                        data = new PlayerData();
                                                        data.steamId = player.Id.SteamId;
                                                    }
                                                    if (offer.BuyingGivesMiningContract)
                                                    {
                                                        int max = 1;
                                                        if (data.MiningReputation >= 100)
                                                        {
                                                            max++;
                                                        }
                                                        if (data.MiningReputation >= 200)
                                                        {
                                                            max++;
                                                        }
                                                        if (data.MiningReputation >= 300)
                                                        {
                                                            max++;
                                                        }
                                                        if (data.MiningReputation >= 400)
                                                        {
                                                            max++;
                                                        }
                                                        //  CrunchEconCore.Log.Info(data.getMiningContracts().Count);
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
                                                            if (ContractUtils.newContracts.TryGetValue(offer.MiningContractName, out GeneratedContract contract))
                                                            {

                                                                for (int i = 1; i <= amount; i++)
                                                                {

                                                                    MiningContract temp = ContractUtils.GeneratedToPlayer(contract);
                                                                    data.MiningContracts.Add(temp.ContractId);
                                                                    MyGps gps = new MyGps();
                                                                    gps.Coords = player.GetPosition();
                                                                    temp.DeliveryLocation = gps.ToString();
                                                                    data.addMining(temp);
                                                                    temp.PlayerSteamId = player.Id.SteamId;


                                                                    //do this the lazy way instead of checking then setting by the key
                                                                    CrunchEconCore.playerData.Remove(player.Id.SteamId);
                                                                    CrunchEconCore.playerData.Add(player.Id.SteamId, data);


                                                                    CrunchEconCore.miningSave.Remove(player.Id.SteamId);
                                                                    CrunchEconCore.miningSave.Add(player.Id.SteamId, temp);


                                                                    CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);



                                                                }
                                                                StringBuilder contractDetails = new StringBuilder();
                                                                foreach (MiningContract c in data.getMiningContracts().Values)
                                                                {
                                                             
                                                                    if (c.minedAmount >= c.amountToMine)
                                                                    {
                                                                        c.DoPlayerGps(player.Identity.IdentityId);
                                                                        contractDetails.AppendLine("Deliver " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.amountToMine));
                                                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC.");
                                                                    }
                                                                    else
                                                                    {
                                                                        contractDetails.AppendLine("Mine " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMine));
                                                                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC.");
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
                                                    else
                                                    {
                                                        DialogMessage m = new DialogMessage("Shop Error", "", "You already have the maximum amount of mining contracts. View them with !contract info");
                                                        ModCommunication.SendMessageTo(m, player.Id.SteamId);
                                                        return false;
                                                    }

                                                }
                                                if (offer.BuyingGivesHaulingContract)
                                                {

                                                }
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


