using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace CrunchEconomy.StoreStuff
{
    [PatchShim]
    public class FunctionalBlockPatch
    {


        internal static readonly MethodInfo update2 =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation100", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
            typeof(FunctionalBlockPatch).GetMethod(nameof(Transfer), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update2).Prefixes.Add(storePatch);
        }

        public static Dictionary<long, DateTime> NextUpdate = new Dictionary<long, DateTime>();
        public static void Transfer(MyFunctionalBlock __instance)
        {
            if (CrunchEconCore.config.DoCombine || CrunchEconCore.config.RefreshPlayerStoresOnLoad)
            {
                if (!(__instance is MyStoreBlock store)) return;
                if (NextUpdate.TryGetValue(__instance.EntityId, out var time))
                {
                    if (DateTime.Now >= time)
                    {
                        NextUpdate[store.EntityId] = DateTime.Now.AddMinutes(15);
                        if (CrunchEconCore.config.DoCombine)
                        {
                            CombineBlock(store);
                        }
                        if (CrunchEconCore.config.RefreshPlayerStoresOnLoad)
                        {
                            RefreshBlock(store);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    NextUpdate.Add(store.EntityId, DateTime.Now.AddMinutes(15));
                    if (CrunchEconCore.config.RefreshPlayerStoresOnLoad)
                    {
                        RefreshBlock(store);
                    }
                }
            }
        }

        public static void RefreshBlock(MyStoreBlock store)
        {
            ClearStoreOfPlayersBuyingOffers(store);
            ClearStoreOfPlayersSellingOrders(store);
        }

        public static void ClearStoreOfPlayersSellingOrders(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Order && item.Amount <= 0)
                {
                    yeet.Add(item);
                }
            }

            Sandbox.ModAPI.MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                foreach (MyStoreItem item in yeet)
                {
                    store.CancelStoreItem(item.Id);
                    var data = new MyStoreItemData(item.Item.Value, item.Amount, item.PricePerUnit, null, null);
                    store.InsertOrder(data, out long dontCare);
                }
            });
        }

        public static void ClearStoreOfPlayersBuyingOffers(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();
            foreach (MyStoreItem item in store.PlayerItems)
            {
                if (item.StoreItemType == StoreItemTypes.Offer && item.Amount <= 0)
                {
                    yeet.Add(item);
                }
            }
            Sandbox.ModAPI.MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                foreach (MyStoreItem item in yeet)
                {
                    store.CancelStoreItem(item.Id);
                    var data = new MyStoreItemData(item.Item.Value, item.Amount, item.PricePerUnit, null, null);
                    store.InsertOffer(data, out long dontCare);
                }
            });
        }

        public static void CombineBlock(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();

            var orders = new Dictionary<SerializableDefinitionId, TempHolder>();
            var offers = new Dictionary<SerializableDefinitionId, TempHolder>();

            foreach (var item in store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Order))
            {
                if (orders.TryGetValue(item.Item.Value, out var temp))
                {
                    if (item.PricePerUnit > temp.pricePer)
                    {
                        temp.pricePer = item.PricePerUnit;
                    }
                    temp.amount += item.Amount;
                }
                else
                {
                    orders.Add(item.Item.Value, new TempHolder() { amount = item.Amount, pricePer = item.PricePerUnit });
                }
                yeet.Add(item);
            }
            foreach (var item in store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Offer))
            {
                if (offers.TryGetValue(item.Item.Value, out var temp))
                {
                    if (item.PricePerUnit > temp.pricePer)
                    {
                        temp.pricePer = item.PricePerUnit;
                    }
                    temp.amount += item.Amount;
                }
                else
                {
                    offers.Add(item.Item.Value, new TempHolder() { amount = item.Amount, pricePer = item.PricePerUnit });
                }
                yeet.Add(item);
            }

            Sandbox.ModAPI.MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                foreach (MyStoreItem item in yeet)
                {
                    store.CancelStoreItem(item.Id);
                }

                foreach (var item in orders)
                {
                    var data = new MyStoreItemData(item.Key, item.Value.amount, item.Value.pricePer, null, null);
                    store.InsertOrder(data, out long dontCare);
                }
                foreach (var item in offers)
                {
                    var data = new MyStoreItemData(item.Key, item.Value.amount, item.Value.pricePer, null, null);
                    store.InsertOffer(data, out long dontCare);
                }
            });
        }

        public class TempHolder
        {
            public int amount { get; set; }
            public int pricePer { get; set; }
        }

    }
}
