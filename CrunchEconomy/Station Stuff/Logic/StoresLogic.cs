using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchEconomy.Station_Stuff.Logic
{
    public static class StoresLogic
    {
        public static Dictionary<long, DateTime> individualTimers = new Dictionary<long, DateTime>();
        public static void DoStationRefresh(Stations station, DateTime now)
        {
            try
            {
                var AddSellTime = false;
                var AddBuyTime = false;
                if (now >= station.nextBuyRefresh || now >= station.nextSellRefresh)
                {
                    var gps = station.getGPS();
           
                    var checkLocation = true;
                    MyCubeGrid storeGrid = null;
                    if (!station.WorldName.Equals("default"))
                    {
                        if (station.StationEntityId > 0)
                        {
                            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) != null)
                            {
                                if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is MyCubeGrid grid)
                                {
                                    checkLocation = false;
                                    storeGrid = grid;
                                    if (station.DoPeriodGridClearing && now >= station.nextGridInventoryClear)
                                    {
                                        station.nextGridInventoryClear =
                                            now.AddSeconds(station.SecondsBetweenClearEntireInventory);
                                        InventoryLogic.ClearInventories(grid, station);

                                        SaveStation(station);
                                    }
                                }
                            }
                        }
                    }

                    if (checkLocation)
                    {
                        var sphere = new BoundingSphereD(gps.Coords, 200);

                        foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere)
                                     .OfType<MyCubeGrid>())
                        {
                            if (!station.DoPeriodGridClearing || now < station.nextGridInventoryClear) continue;
                            station.nextGridInventoryClear =
                                now.AddSeconds(station.SecondsBetweenClearEntireInventory);
                            InventoryLogic.ClearInventories(grid, station);
                            storeGrid = grid;
                            SaveStation(station);
                        }
                    }



                    foreach (var store in storeGrid.GetFatBlocks().OfType<MyStoreBlock>())
                    {
                        if (!store.GetOwnerFactionTag().Equals(station.OwnerFactionTag)) continue;
                        station.StationEntityId = store.CubeGrid.EntityId;
                        station.WorldName = MyMultiplayer.Static.HostName;
                        if (now >= station.nextSellRefresh && station.DoSellOffers)
                        {
                            AddSellTime = true;
                            DoSellRefresh(station, now, store, storeGrid);
                        }

                        if (now >= station.nextBuyRefresh && station.DoBuyOrders)
                        {
                            AddBuyTime = true;
                            DoBuyRefresh(station, now, store, storeGrid);
                        }
                    }
                }

                var save = false;
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

            catch (Exception ex)
            {
                SaveStation(station);
                CrunchEconCore.Log.Error("Error on this station " + station.Name);
                CrunchEconCore.Log.Error(ex.ToString());
            }
        }

        public static void DoBuyRefresh(Stations station, DateTime now, MyStoreBlock store, MyCubeGrid storeGrid)
        {
            if (!CrunchEconCore.ConfigProvider.GetBuyOrders().TryGetValue(store.DisplayNameText, out var orders)) return;
            ClearStoreOfPlayersSellingOrders(store);
            var inventories =
                new List<VRage.Game.ModAPI.IMyInventory>();
            inventories.AddRange(InventoryLogic.GetInventories(storeGrid, station));

            foreach (var order in orders)
            {
                try
                {
                    if (order.IndividualRefreshTimer)
                    {
                        if (individualTimers.TryGetValue(store.EntityId, out var refresh))
                        {
                            if (now < refresh)
                            {
                                continue;
                            }

                            individualTimers[store.EntityId] =
                                DateTime.Now.AddSeconds(order.SecondsBetweenRefresh);
                        }
                        else
                        {
                            individualTimers.Add(store.EntityId,
                                DateTime.Now.AddSeconds(order.SecondsBetweenRefresh));
                        }
                    }

                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + order.typeId, order.subtypeId,
                            out var id)) continue;
                    var itemId =
                        new SerializableDefinitionId(id.TypeId, order.subtypeId);


                    var chance = CrunchEconCore.rnd.NextDouble();
                    if (!(chance <= order.chance)) continue;
                    var price = CrunchEconCore.rnd.Next((int)order.minPrice, (int)order.maxPrice);
                    price = Convert.ToInt32(price *
                                            station.GetModifier(
                                                order.StationModifierItemName));
                    var amount = CrunchEconCore.rnd.Next((int)order.minAmount, (int)order.maxAmount);
                    var item =
                        new MyStoreItemData(itemId, amount, price, null, null);
                    var result =
                        store.InsertOrder(item, out var notUsingThis);
                    if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum ||
                        result == MyStoreInsertResults.Fail_StoreLimitReached ||
                        result == MyStoreInsertResults.Error)
                    {
                        CrunchEconCore.Log.Error("Unable to insert this order into store " +
                                                 order.typeId + " " + order.subtypeId +
                                                 " at station " + station.Name + " " +
                                                 result.ToString());
                    }
                }


                catch (Exception ex)
                {

                    CrunchEconCore.Log.Error("Error on this buy order " + order.typeId + " " +
                                             order.subtypeId);
                    CrunchEconCore.Log.Error("for this station " + station.Name);
                    station.nextBuyRefresh =
                        now.AddSeconds(station.SecondsBetweenRefreshForBuyOrders);
                    CrunchEconCore.Log.Error(ex.ToString());
                    SaveStation(station);
                }

            }
        }

        public static void DoSellRefresh(Stations station, DateTime now, MyStoreBlock store, MyCubeGrid storeGrid)
        {
            if (!CrunchEconCore.ConfigProvider.GetSellOffers()
                    .TryGetValue(store.DisplayNameText, out var offers)) return;
            ClearStoreOfPlayersBuyingOffers(store);
            var inventories = new List<IMyInventory>();
            inventories.AddRange(InventoryLogic.GetInventories(storeGrid, station));

            foreach (var offer in offers)
            {
                try
                {
                    var chance = CrunchEconCore.rnd.NextDouble();
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + offer.typeId, offer.subtypeId,
                            out var id)) continue;
                    var hasAmount = InventoryLogic.CountComponents(inventories, id).ToIntSafe();
                    if (hasAmount > 0)
                    {
                        if (hasAmount < offer.SpawnIfCargoLessThan && offer.SpawnItemsIfNeeded)
                        {
                            var amountSpawned = 0;

                            amountSpawned = CrunchEconCore.rnd.Next(offer.minAmountToSpawn,
                                offer.maxAmountToSpawn);
                            if (offer.IndividualRefreshTimer)
                            {
                                if (individualTimers.TryGetValue(store.EntityId,
                                        out var refresh))
                                {
                                    if (now >= refresh)
                                    {
                                        individualTimers[store.EntityId] =
                                            DateTime.Now.AddSeconds(
                                                offer.SecondsBetweenRefresh);


                                        if (chance <= offer.chance)
                                        {
                                            InventoryLogic.SpawnItems(storeGrid, id,
                                                (MyFixedPoint)amountSpawned, station);
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
                                    individualTimers.Add(store.EntityId,
                                        DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));

                                    if (chance <= offer.chance)
                                    {
                                        InventoryLogic.SpawnItems(storeGrid, id,
                                            (MyFixedPoint)amountSpawned, station);
                                        hasAmount += amountSpawned;
                                    }
                                }
                            }
                            else
                            {
                                if (chance <= offer.chance)
                                {
                                    InventoryLogic.SpawnItems(storeGrid, id,
                                        (MyFixedPoint)amountSpawned, station);
                                    hasAmount += amountSpawned;
                                }
                            }

                        }

                        var itemId = new SerializableDefinitionId(id.TypeId, offer.subtypeId);

                        var price = CrunchEconCore.rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                        price = Convert.ToInt32(price *
                                                station.GetModifier(
                                                    offer.StationModifierItemName));
                        var item = new MyStoreItemData(itemId, hasAmount, price,
                            null, null);
                        //       CrunchEconCore.Log.Info("if it got here its creating the offer");
                        var result =
                            store.InsertOffer(item, out var notUsingThis);

                        if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum ||
                            result == MyStoreInsertResults.Fail_StoreLimitReached ||
                            result == MyStoreInsertResults.Error)
                        {
                            CrunchEconCore.Log.Error("Unable to insert this offer into store " +
                                                     offer.typeId + " " + offer.subtypeId +
                                                     " at station " + station.Name + " " +
                                                     result.ToString());
                        }
                    }
                    else
                    {
                        if (!offer.SpawnItemsIfNeeded) continue;
                        var amountSpawned = 0;

                        amountSpawned = CrunchEconCore.rnd.Next(offer.minAmountToSpawn,
                            offer.maxAmountToSpawn);
                        if (offer.IndividualRefreshTimer)
                        {
                            if (individualTimers.TryGetValue(store.EntityId,
                                    out var refresh))
                            {
                                if (now >= refresh)
                                {
                                    individualTimers[store.EntityId] =
                                        DateTime.Now.AddSeconds(
                                            offer.SecondsBetweenRefresh);


                                    if (chance <= offer.chance)
                                    {
                                        InventoryLogic.SpawnItems(storeGrid, id,
                                            (MyFixedPoint)amountSpawned, station);
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
                                individualTimers.Add(store.EntityId,
                                    DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));

                                if (chance <= offer.chance)
                                {
                                    InventoryLogic.SpawnItems(storeGrid, id,
                                        (MyFixedPoint)amountSpawned, station);
                                    hasAmount += amountSpawned;
                                }
                            }

                        }
                        else
                        {
                            InventoryLogic.SpawnItems(storeGrid, id,
                                (MyFixedPoint)offer.SpawnIfCargoLessThan, station);
                        }


                        var itemId =
                            new SerializableDefinitionId(id.TypeId, offer.subtypeId);




                        var price = CrunchEconCore.rnd.Next((int)offer.minPrice, (int)offer.maxPrice);
                        price = Convert.ToInt32(price *
                                                station.GetModifier(
                                                    offer.StationModifierItemName));
                        var item = new MyStoreItemData(itemId,
                            offer.SpawnIfCargoLessThan, price, null, null);
                        //    CrunchEconCore.Log.Info("if it got here its creating the offer");
                        var result =
                            store.InsertOffer(item, out var notUsingThis);
                        if (result == MyStoreInsertResults
                                .Fail_PricePerUnitIsLessThanMinimum ||
                            result == MyStoreInsertResults.Fail_StoreLimitReached ||
                            result == MyStoreInsertResults.Error)
                        {
                            CrunchEconCore.Log.Error(
                                "Unable to insert this offer into store " + offer.typeId +
                                " " + offer.subtypeId + " at station " + station.Name +
                                " " + result.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    CrunchEconCore.Log.Error("Error on this sell offer " + offer.typeId + " " +
                                             offer.subtypeId);
                    CrunchEconCore.Log.Error("for this station " + station.Name);
                    station.nextSellRefresh =
                        now.AddSeconds(station.SecondsBetweenRefreshForSellOffers);
                    CrunchEconCore.Log.Error(ex.ToString());
                    SaveStation(station);
                }
            }
        }

        public static void ClearStoreOfPlayersSellingOrders(MyStoreBlock store)
        {
            var yeet = store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Order).ToList();
            foreach (var item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public static void ClearStoreOfPlayersBuyingOffers(MyStoreBlock store)
        {

            var yeet = store.PlayerItems.Where(item => item.StoreItemType == StoreItemTypes.Offer).ToList();
            foreach (var item in yeet)
            {
                store.CancelStoreItem(item.Id);
            }
        }

        public static void SaveStation(Stations station)
        {
            CrunchEconCore.ConfigProvider.SaveStation(station);
        }
    }
}
