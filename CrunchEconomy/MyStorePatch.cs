using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.BankingAndCurrency;
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

                        if (!CrunchEconCore.gridsForSale.ContainsKey(storeItem.Item.Value.SubtypeName))
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
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

    }
}

