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
        public static Dictionary<long, DateTime> NextUpdate2 = new Dictionary<long, DateTime>();
        public static void Transfer(MyFunctionalBlock __instance)
        {
            if (CrunchEconCore.config.DoCombine)
            {
                if (!(__instance is MyStoreBlock store)) return;
                if (CrunchEconCore.config.DoCombine)
                {
                    if (NextUpdate2.TryGetValue(__instance.EntityId, out var time2))
                    {
                        if (DateTime.Now > time2)
                        {
                            NextUpdate2[store.EntityId] = DateTime.Now.AddMinutes(5);
                            CombineBlock(store);
                        }
                    }
                    else
                    {
                        NextUpdate2.Add(store.EntityId, DateTime.Now.AddMinutes(5));
                        CombineBlock(store);
                    }
                }
            }
        }


        public static void CombineBlock(MyStoreBlock store)
        {
            List<MyStoreItem> yeet = new List<MyStoreItem>();

            var orders = new Dictionary<SerializableDefinitionId, TempHolder>();
            var offers = new Dictionary<SerializableDefinitionId, TempHolder>();

            foreach (var order in store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Order))
            {
                if (order.Amount > 0)
                {

                    if (orders.TryGetValue(order.Item.Value, out var temp))
                    {
                        if (order.PricePerUnit > temp.pricePer)
                        {
                            temp.pricePer = order.PricePerUnit;
                        }
                        temp.amount += order.Amount;
                    }
                    else
                    {
                        orders.Add(order.Item.Value, new TempHolder() { amount = order.Amount, pricePer = order.PricePerUnit });
                    }
                }
                yeet.Add(order);
            }
            foreach (var offer in store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Offer))
            {
                if (offer.Amount > 0)
                {
                    if (offers.TryGetValue(offer.Item.Value, out var temp))
                    {
                        if (offer.PricePerUnit > temp.pricePer)
                        {
                            temp.pricePer = offer.PricePerUnit;
                        }
                        temp.amount += offer.Amount;
                    }
                    else
                    {
                        offers.Add(offer.Item.Value, new TempHolder() { amount = offer.Amount, pricePer = offer.PricePerUnit });

                    }
                }
                yeet.Add(offer);
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
