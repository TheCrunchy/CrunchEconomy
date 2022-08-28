using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
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
        public static Dictionary<long, DateTime> IndividualTimers = new Dictionary<long, DateTime>();
        public static void DoStationRefresh(Stations Station, DateTime Now)
        {
            try
            {
                var addSellTime = false;
                var addBuyTime = false;
                if (Now >= Station.NextBuyRefresh || Now >= Station.NextSellRefresh)
                {
                    var gps = Station.GetGps();
           
                    var checkLocation = true;
                    MyCubeGrid storeGrid = null;
                    if (!Station.WorldName.Equals("default"))
                    {
                        if (Station.StationEntityId > 0)
                        {
                            if (MyAPIGateway.Entities.GetEntityById(Station.StationEntityId) != null)
                            {
                                if (MyAPIGateway.Entities.GetEntityById(Station.StationEntityId) is MyCubeGrid grid)
                                {
                                    checkLocation = false;
                                    storeGrid = grid;
                                    if (Station.DoPeriodGridClearing && Now >= Station.NextGridInventoryClear)
                                    {
                                        Station.NextGridInventoryClear =
                                            Now.AddSeconds(Station.SecondsBetweenClearEntireInventory);
                                        InventoryLogic.ClearInventories(grid, Station);

                                        SaveStation(Station);
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
                            if (!Station.DoPeriodGridClearing || Now < Station.NextGridInventoryClear) continue;
                            Station.NextGridInventoryClear =
                                Now.AddSeconds(Station.SecondsBetweenClearEntireInventory);
                            InventoryLogic.ClearInventories(grid, Station);
                            storeGrid = grid;
                            SaveStation(Station);
                        }
                    }



                    foreach (var store in storeGrid.GetFatBlocks().OfType<MyStoreBlock>())
                    {
                        if (!store.GetOwnerFactionTag().Equals(Station.OwnerFactionTag)) continue;
                        Station.StationEntityId = store.CubeGrid.EntityId;
                        Station.WorldName = MyMultiplayer.Static.HostName;
                        if (Now >= Station.NextSellRefresh && Station.DoSellOffers)
                        {
                            addSellTime = true;
                            DoSellRefresh(Station, Now, store, storeGrid);
                        }

                        if (Now >= Station.NextBuyRefresh && Station.DoBuyOrders)
                        {
                            addBuyTime = true;
                            DoBuyRefresh(Station, Now, store, storeGrid);
                        }
                    }
                }

                var save = false;
                if (addSellTime)
                {
                    Station.NextSellRefresh = Now.AddSeconds(Station.SecondsBetweenRefreshForSellOffers);
                    save = true;
                }

                if (addBuyTime)
                {
                    Station.NextBuyRefresh = Now.AddSeconds(Station.SecondsBetweenRefreshForBuyOrders);
                    save = true;
                }

                if (save)
                {
                    SaveStation(Station);
                }
            }

            catch (Exception ex)
            {
                SaveStation(Station);
                CrunchEconCore.Log.Error("Error on this station " + Station.Name);
                CrunchEconCore.Log.Error(ex.ToString());
            }
        }

        public static void DoBuyRefresh(Stations Station, DateTime Now, MyStoreBlock Store, MyCubeGrid StoreGrid)
        {
            if (!CrunchEconCore.ConfigProvider.GetBuyOrders().TryGetValue(Store.DisplayNameText, out var orders)) return;
            ClearStoreOfPlayersSellingOrders(Store);
            var inventories =
                new List<VRage.Game.ModAPI.IMyInventory>();
            inventories.AddRange(InventoryLogic.GetInventories(StoreGrid, Station));

            foreach (var order in orders)
            {
                try
                {
                    if (order.IndividualRefreshTimer)
                    {
                        if (IndividualTimers.TryGetValue(Store.EntityId, out var refresh))
                        {
                            if (Now < refresh)
                            {
                                continue;
                            }

                            IndividualTimers[Store.EntityId] =
                                DateTime.Now.AddSeconds(order.SecondsBetweenRefresh);
                        }
                        else
                        {
                            IndividualTimers.Add(Store.EntityId,
                                DateTime.Now.AddSeconds(order.SecondsBetweenRefresh));
                        }
                    }

                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + order.TypeId, order.SubtypeId,
                            out var id)) continue;
                    var itemId =
                        new SerializableDefinitionId(id.TypeId, order.SubtypeId);


                    var chance = CrunchEconCore.Rnd.NextDouble();
                    if (!(chance <= order.Chance)) continue;
                    var price = CrunchEconCore.Rnd.Next((int)order.MinPrice, (int)order.MaxPrice);
                    price = Convert.ToInt32(price *
                                            Station.GetModifier(
                                                order.StationModifierItemName));
                    var amount = CrunchEconCore.Rnd.Next((int)order.MinAmount, (int)order.MaxAmount);
                    var item =
                        new MyStoreItemData(itemId, amount, price, null, null);
                    var result =
                        Store.InsertOrder(item, out var notUsingThis);
                    if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum ||
                        result == MyStoreInsertResults.Fail_StoreLimitReached ||
                        result == MyStoreInsertResults.Error)
                    {
                        CrunchEconCore.Log.Error("Unable to insert this order into store " +
                                                 order.TypeId + " " + order.SubtypeId +
                                                 " at station " + Station.Name + " " +
                                                 result.ToString());
                    }
                }


                catch (Exception ex)
                {

                    CrunchEconCore.Log.Error("Error on this buy order " + order.TypeId + " " +
                                             order.SubtypeId);
                    CrunchEconCore.Log.Error("for this station " + Station.Name);
                    Station.NextBuyRefresh =
                        Now.AddSeconds(Station.SecondsBetweenRefreshForBuyOrders);
                    CrunchEconCore.Log.Error(ex.ToString());
                    SaveStation(Station);
                }

            }
        }

        public static void DoSellRefresh(Stations Station, DateTime Now, MyStoreBlock Store, MyCubeGrid StoreGrid)
        {
            if (!CrunchEconCore.ConfigProvider.GetSellOffers()
                    .TryGetValue(Store.DisplayNameText, out var offers)) return;
            ClearStoreOfPlayersBuyingOffers(Store);
            var inventories = new List<IMyInventory>();
            inventories.AddRange(InventoryLogic.GetInventories(StoreGrid, Station));

            foreach (var offer in offers)
            {
                try
                {
                    var chance = CrunchEconCore.Rnd.NextDouble();
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + offer.TypeId, offer.SubtypeId,
                            out var id)) continue;
                    var hasAmount = InventoryLogic.CountComponents(inventories, id).ToIntSafe();
                    if (hasAmount > 0)
                    {
                        if (hasAmount < offer.SpawnIfCargoLessThan && offer.SpawnItemsIfNeeded)
                        {
                            var amountSpawned = 0;

                            amountSpawned = CrunchEconCore.Rnd.Next(offer.MinAmountToSpawn,
                                offer.MaxAmountToSpawn);
                            if (offer.IndividualRefreshTimer)
                            {
                                if (IndividualTimers.TryGetValue(Store.EntityId,
                                        out var refresh))
                                {
                                    if (Now >= refresh)
                                    {
                                        IndividualTimers[Store.EntityId] =
                                            DateTime.Now.AddSeconds(
                                                offer.SecondsBetweenRefresh);


                                        if (chance <= offer.Chance)
                                        {
                                            InventoryLogic.SpawnItems(StoreGrid, id,
                                                (MyFixedPoint)amountSpawned, Station);
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
                                    IndividualTimers.Add(Store.EntityId,
                                        DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));

                                    if (chance <= offer.Chance)
                                    {
                                        InventoryLogic.SpawnItems(StoreGrid, id,
                                            (MyFixedPoint)amountSpawned, Station);
                                        hasAmount += amountSpawned;
                                    }
                                }
                            }
                            else
                            {
                                if (chance <= offer.Chance)
                                {
                                    InventoryLogic.SpawnItems(StoreGrid, id,
                                        (MyFixedPoint)amountSpawned, Station);
                                    hasAmount += amountSpawned;
                                }
                            }

                        }

                        var itemId = new SerializableDefinitionId(id.TypeId, offer.SubtypeId);

                        var price = CrunchEconCore.Rnd.Next((int)offer.MinPrice, (int)offer.MaxPrice);
                        price = Convert.ToInt32(price *
                                                Station.GetModifier(
                                                    offer.StationModifierItemName));
                        var item = new MyStoreItemData(itemId, hasAmount, price,
                            null, null);
                        //       CrunchEconCore.Log.Info("if it got here its creating the offer");
                        var result =
                            Store.InsertOffer(item, out var notUsingThis);

                        if (result == MyStoreInsertResults.Fail_PricePerUnitIsLessThanMinimum ||
                            result == MyStoreInsertResults.Fail_StoreLimitReached ||
                            result == MyStoreInsertResults.Error)
                        {
                            CrunchEconCore.Log.Error("Unable to insert this offer into store " +
                                                     offer.TypeId + " " + offer.SubtypeId +
                                                     " at station " + Station.Name + " " +
                                                     result.ToString());
                        }
                    }
                    else
                    {
                        if (!offer.SpawnItemsIfNeeded) continue;
                        var amountSpawned = 0;

                        amountSpawned = CrunchEconCore.Rnd.Next(offer.MinAmountToSpawn,
                            offer.MaxAmountToSpawn);
                        if (offer.IndividualRefreshTimer)
                        {
                            if (IndividualTimers.TryGetValue(Store.EntityId,
                                    out var refresh))
                            {
                                if (Now >= refresh)
                                {
                                    IndividualTimers[Store.EntityId] =
                                        DateTime.Now.AddSeconds(
                                            offer.SecondsBetweenRefresh);


                                    if (chance <= offer.Chance)
                                    {
                                        InventoryLogic.SpawnItems(StoreGrid, id,
                                            (MyFixedPoint)amountSpawned, Station);
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
                                IndividualTimers.Add(Store.EntityId,
                                    DateTime.Now.AddSeconds(offer.SecondsBetweenRefresh));

                                if (chance <= offer.Chance)
                                {
                                    InventoryLogic.SpawnItems(StoreGrid, id,
                                        (MyFixedPoint)amountSpawned, Station);
                                    hasAmount += amountSpawned;
                                }
                            }

                        }
                        else
                        {
                            InventoryLogic.SpawnItems(StoreGrid, id,
                                (MyFixedPoint)offer.SpawnIfCargoLessThan, Station);
                        }


                        var itemId =
                            new SerializableDefinitionId(id.TypeId, offer.SubtypeId);




                        var price = CrunchEconCore.Rnd.Next((int)offer.MinPrice, (int)offer.MaxPrice);
                        price = Convert.ToInt32(price *
                                                Station.GetModifier(
                                                    offer.StationModifierItemName));
                        var item = new MyStoreItemData(itemId,
                            offer.SpawnIfCargoLessThan, price, null, null);
                        //    CrunchEconCore.Log.Info("if it got here its creating the offer");
                        var result =
                            Store.InsertOffer(item, out var notUsingThis);
                        if (result == MyStoreInsertResults
                                .Fail_PricePerUnitIsLessThanMinimum ||
                            result == MyStoreInsertResults.Fail_StoreLimitReached ||
                            result == MyStoreInsertResults.Error)
                        {
                            CrunchEconCore.Log.Error(
                                "Unable to insert this offer into store " + offer.TypeId +
                                " " + offer.SubtypeId + " at station " + Station.Name +
                                " " + result.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    CrunchEconCore.Log.Error("Error on this sell offer " + offer.TypeId + " " +
                                             offer.SubtypeId);
                    CrunchEconCore.Log.Error("for this station " + Station.Name);
                    Station.NextSellRefresh =
                        Now.AddSeconds(Station.SecondsBetweenRefreshForSellOffers);
                    CrunchEconCore.Log.Error(ex.ToString());
                    SaveStation(Station);
                }
            }
        }

        public static void ClearStoreOfPlayersSellingOrders(MyStoreBlock Store)
        {
            var yeet = Store.PlayerItems.Where(Item => Item.StoreItemType == StoreItemTypes.Order).ToList();
            foreach (var item in yeet)
            {
                Store.CancelStoreItem(item.Id);
            }
        }

        public static void ClearStoreOfPlayersBuyingOffers(MyStoreBlock Store)
        {

            var yeet = Store.PlayerItems.Where(Item => Item.StoreItemType == StoreItemTypes.Offer).ToList();
            foreach (var item in yeet)
            {
                Store.CancelStoreItem(item.Id);
            }
        }

        public static void SaveStation(Stations Station)
        {
            CrunchEconCore.ConfigProvider.SaveStation(Station);
        }

        public static void RefreshWhitelists(Stations Station)
        {
            if (!Station.WhitelistedSafezones) return;
            var sphere = new BoundingSphereD(Station.GetGps().Coords, 200);

            foreach (var zone in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MySafeZone>())
            {
                zone.Factions.Clear();
                zone.AccessTypeFactions = Station.DoBlacklist ? MySafeZoneAccess.Blacklist : MySafeZoneAccess.Whitelist;

                foreach (var s in Station.Whitelist)
                {
                    if (s.Contains("LIST:"))
                    {
                        var temp = s.Split(':')[1];
       
                        foreach (var tag in from list in CrunchEconCore.ConfigProvider.Whitelist.Values where list.ListName == temp from tag in list.FactionTags where MySession.Static.Factions.TryGetFactionByTag(tag) != null select tag)
                        {
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
    }
}
