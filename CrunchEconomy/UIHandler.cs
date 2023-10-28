using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CrunchEconModels.Models;
using CrunchEconModels.Models.Events;
using CrunchEconUI.EntityFramework;
using Newtonsoft.Json;
using Sandbox.Game.World;
using RestSharp;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders.Private;
using VRageMath;
using IMyCargoContainer = Sandbox.ModAPI.IMyCargoContainer;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;

namespace CrunchEconomy
{
    public static class UIHandler
    {
        private static EconContext Context { get; set; }
        public static bool SentDefinitions = false;
        private static DateTime balances = DateTime.Now;

        public static void SetupContext(string connection)
        {
            try
            {
                Context = new EconContext(connection);
            }
            catch (Exception e)
            {
                CrunchEconCore.Log.Error(e);
            }
        }
        public static async Task Handle()
        {
            try
            {

                if (!SentDefinitions && CrunchEconCore.config.SendDefinitions)
                {
                    await SendTextures();
                }
            }
            catch (Exception e)
            {
                CrunchEconCore.Log.Error($"Event error {e}");
            }
            //   CrunchEconCore.Log.Info("start processing events");

            //     await Task.Delay(TimeSpan.FromSeconds(1));
            try
            {
                await ProcessEventsDB();
            }
            catch (Exception e)
            {
                CrunchEconCore.Log.Error($"Event error {e}");
                var client = new WebClient();
                client.Headers.Add("Content-Type", "application/json");
                //send to ingame and nexus 
                var payloadJson = JsonConvert.SerializeObject(new
                {
                    username = "Econ Error",
                    embeds = new[]
                        {
                            new
                            {
                                description = e.ToString(),
                                title = "Econ Error",
                            }
                        }
                }
                );

                var payload = payloadJson;

                var utf8 = Encoding.UTF8.GetBytes(payload);
                try
                {
                    client.UploadData("https://discord.com/api/webhooks/1137339838794846259/ENvaHHung085Rp0kCRzVKEUmMJhtNMkMenmt7z_MWqqpG4fiLS5cVCom-82AmA6_Q7cL", utf8);
                }
                catch (Exception ex)
                {
                }
            }

            if (DateTime.Now > balances)
            {
                balances = DateTime.Now.AddSeconds(5);
                try
                {
                    await UpdateBalances();
                    await SendPrefabs();
                }
                catch (Exception e)
                {
                    CrunchEconCore.Log.Error($"Balance update error {e}");
                }

            }

            //  CrunchEconCore.Log.Info("Done processing events");
        }

        public static async Task SendPrefabs()
        {
            var returnevent = new Event();
            var files = new List<String>();
            foreach (var file in Directory.GetFiles(CrunchEconCore.path + "//GridSelling//Grids//", "*", SearchOption.AllDirectories))
            {
                files.Add(Path.GetFileName(file));
            }

            if (files.Any())
            {
                var prefabEvent = new PrefabEvent()
                {
                    Prefabs = files
                };
                var client = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostEvent");
                var request = new RestRequest();
                var message = new APIMessage();
                message.APIKEY = CrunchEconCore.config.ApiKey;
                returnevent.JsonEvent = JsonConvert.SerializeObject(prefabEvent);
                returnevent.EventType = EventType.PrefabEvent;
                message.JsonMessage = JsonConvert.SerializeObject(returnevent);
                request.AddStringBody(JsonConvert.SerializeObject(message), DataFormat.Json);
                var response = await client.PostAsync(request);
            }
        }

        public static async Task SendTextures()
        {
            var returnevent = new Event();
            var textures = new List<TextureEvent>();



            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                try
                {
                    if (def as MyPhysicalItemDefinition == null) continue;
                    if (def.Icons == null || !def.Icons.Any()) continue;
                    var icon = def.Icons[0];
                    if (icon == null) continue;
                    var path = icon.Substring(def.Icons[0].IndexOf("Textures"));
                    if (icon.StartsWith("Textures"))
                    {
                        var parent = Directory.GetParent(CrunchEconCore.basePath);
                        icon = $"{parent}/{icon}";
                    }

                    if (!File.Exists(icon)) continue;
                    if (def.Id.ToString().Contains("MyObjectBuilder_TreeObject"))
                    {
                        continue;
                    }
                    byte[] imageArray = System.IO.File.ReadAllBytes(icon);

                    textures.Add(new TextureEvent()
                    {
                        DefinitionId = def.Id.ToString(),
                        TexturePath = path,
                        Base64Texture = imageArray
                    });
                }
                catch (Exception e)
                {
                }
            }

            if (textures.Any())
            {
                //CrunchEconCore.Log.Error("Sending");
                foreach (var ev in textures)
                {
                    var client = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostEvent");
                    var request = new RestRequest();
                    var message = new APIMessage();
                    message.APIKEY = CrunchEconCore.config.ApiKey;
                    returnevent.JsonEvent = JsonConvert.SerializeObject(ev);
                    returnevent.EventType = EventType.TextureEvent;
                    message.JsonMessage = JsonConvert.SerializeObject(returnevent);
                    request.AddStringBody(JsonConvert.SerializeObject(message), DataFormat.Json);
                    var response = await client.PostAsync(request);
                    Thread.Sleep(10000);
                }

                var client2 = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostEvent");
                var request2 = new RestRequest();
                var message2 = new APIMessage();
                message2.APIKEY = CrunchEconCore.config.ApiKey;
                returnevent.JsonEvent = JsonConvert.SerializeObject(new Event() { EventType = EventType.SaveTexturesJson });
                returnevent.EventType = EventType.SaveTexturesJson;
                message2.JsonMessage = JsonConvert.SerializeObject(returnevent);
                request2.AddStringBody(JsonConvert.SerializeObject(message2), DataFormat.Json);
                var response2 = await client2.PostAsync(request2);

                //CrunchEconCore.Log.Error("Sent");
            }
        }

        public static async Task ProcessEventsDB()
        {
            CrunchEconCore.Log.Info("Start DB call");
            var players = MySession.Static.Players.GetOnlinePlayers().Select(player => player.Client.SteamUserId).ToList();
            var returningEvents = new List<Event>();
            foreach (var item in Context.ArchivedEvents.Where(x => x.Waiting && !x.Processed).ToList())
            {
                if (!players.Contains((ulong)item.OriginatingPlayerId))
                {
                    CrunchEconCore.Log.Info("player not online");
                    continue;
                }
                    
                //  CrunchEconCore.Log.Info("5");
                item.Processed = true;
                item.Source = EventSource.Torch;
                switch (item.EventType)
                {
                    case EventType.ListItem:
                        {
                            var parsedEvent = JsonConvert.DeserializeObject<CreateListingEvent>(item.JsonEvent);
                            try
                            {
                                var eventresult = await HandleListEvent(item, parsedEvent);
                                parsedEvent.Result = eventresult;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.ListItemResult;

                            }
                            catch (Exception)
                            {
                                parsedEvent.Result = EventResult.Failure;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.ListItemResult;

                            }

                            break;
                        }
                    case EventType.BuyShip:
                        {
                            //         CrunchEconCore.Log.Info("6");
                            var parsedEvent = JsonConvert.DeserializeObject<BuyShipEvent>(item.JsonEvent);
                            try
                            {
                                var eventresult = await HandleBuyShipEvent(item, parsedEvent);
                                parsedEvent.Result = eventresult;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.BuyShipResult;

                            }
                            catch (Exception)
                            {
                                parsedEvent.Result = EventResult.Failure;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.BuyShipResult;

                            }

                            //    CrunchEconCore.Log.Info(eventresult.ToString());
                            break;
                        }
                    case EventType.BuyItem:
                        {
                            //         CrunchEconCore.Log.Info("6");
                            var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
                            try
                            {
                                var eventresult = await HandleBuyEvent(item, parsedEvent);
                                parsedEvent.Result = eventresult;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.BuyItemResult;

                            }
                            catch (Exception)
                            {
                                parsedEvent.Result = EventResult.Failure;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.BuyItemResult;

                            }

                            //    CrunchEconCore.Log.Info(eventresult.ToString());
                            break;
                        }
                    case EventType.SellItem:
                        {
                            var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
                            try
                            {

                                var eventresult = await HandleSellEvent(item, parsedEvent);
                                parsedEvent.Result = eventresult;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.SellItemResult;

                            }
                            catch (Exception)
                            {
                                parsedEvent.Result = EventResult.Failure;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.SellItemResult;
                            }

                            break;
                        }
                    case EventType.DeleteListing:
                        {
                            var parsedEvent = JsonConvert.DeserializeObject<DeleteListingEvent>(item.JsonEvent);
                            try
                            {
                                var eventresult = await HandleDeleteEvent(item, parsedEvent);
                                parsedEvent.Result = eventresult;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.DeleteListing;

                            }
                            catch (Exception)
                            {
                                parsedEvent.Result = EventResult.Failure;
                                item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                item.EventType = EventType.DeleteListing;
                            }

                            break;
                        }
                    default:
                        break;
                }
            }

            await Context.SaveChangesAsync();
            CrunchEconCore.Log.Info("End DB call");
        }

        public static async Task ProcessEventsApi()
        {
            var players = new List<ulong>();
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                players.Add(player.Client.SteamUserId);
            }

            //if (!players.Any())
            //{
            //    return;
            //}
            //  CrunchEconCore.Log.Info("1");
            var client = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/GetEventsForPlayers");
            var request = new RestRequest();
            var message = new APIMessage();
            message.APIKEY = CrunchEconCore.config.ApiKey;
            message.JsonMessage = JsonConvert.SerializeObject(players);
            request.AddStringBody(JsonConvert.SerializeObject(message), DataFormat.Json);
            var returningEvents = new List<Event>();
            ///  CrunchEconCore.Log.Info("2");
            var response = await client.PostAsync(request);
            //  CrunchEconCore.Log.Info("3");
            if (response.IsSuccessful)
            {
                //   CrunchEconCore.Log.Info("4");
                var temp = JsonConvert.DeserializeObject<string>(response.Content);
                var messages = JsonConvert.DeserializeObject<Dictionary<ulong, List<Event>>>(temp);
                foreach (var playerEvent in messages)
                {
                    //  CrunchEconCore.Log.Info("5");
                    var value = playerEvent.Value;
                    foreach (var item in value)
                    {
                        switch (item.EventType)
                        {
                            case EventType.ListItem:
                                {
                                    var parsedEvent = JsonConvert.DeserializeObject<CreateListingEvent>(item.JsonEvent);
                                    try
                                    {
                                        var eventresult = await HandleListEvent(item, parsedEvent);
                                        parsedEvent.Result = eventresult;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.ListItemResult;
                                        returningEvents.Add(item);
                                    }
                                    catch (Exception)
                                    {
                                        parsedEvent.Result = EventResult.Failure;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.ListItemResult;
                                        returningEvents.Add(item);
                                    }
                                    break;
                                }
                            case EventType.BuyShip:
                                {
                                    //         CrunchEconCore.Log.Info("6");
                                    var parsedEvent = JsonConvert.DeserializeObject<BuyShipEvent>(item.JsonEvent);
                                    try
                                    {
                                        var eventresult = await HandleBuyShipEvent(item, parsedEvent);
                                        parsedEvent.Result = eventresult;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.BuyShipResult;
                                        returningEvents.Add(item);
                                    }
                                    catch (Exception)
                                    {
                                        parsedEvent.Result = EventResult.Failure;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.BuyShipResult;
                                        returningEvents.Add(item);
                                    }

                                    //    CrunchEconCore.Log.Info(eventresult.ToString());
                                    break;
                                }
                            case EventType.BuyItem:
                                {
                                    //         CrunchEconCore.Log.Info("6");
                                    var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
                                    try
                                    {
                                        var eventresult = await HandleBuyEvent(item, parsedEvent);
                                        parsedEvent.Result = eventresult;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.BuyItemResult;
                                        returningEvents.Add(item);
                                    }
                                    catch (Exception)
                                    {
                                        parsedEvent.Result = EventResult.Failure;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.BuyItemResult;
                                        returningEvents.Add(item);
                                    }

                                    //    CrunchEconCore.Log.Info(eventresult.ToString());
                                    break;
                                }
                            case EventType.SellItem:
                                {
                                    var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
                                    try
                                    {

                                        var eventresult = await HandleSellEvent(item, parsedEvent);
                                        parsedEvent.Result = eventresult;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.SellItemResult;
                                        returningEvents.Add(item);
                                    }
                                    catch (Exception)
                                    {
                                        parsedEvent.Result = EventResult.Failure;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.SellItemResult;
                                        returningEvents.Add(item);
                                    }
                                    break;
                                }
                            case EventType.DeleteListing:
                                {
                                    var parsedEvent = JsonConvert.DeserializeObject<DeleteListingEvent>(item.JsonEvent);
                                    try
                                    {
                                        var eventresult = await HandleDeleteEvent(item, parsedEvent);
                                        parsedEvent.Result = eventresult;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.DeleteListing;
                                        returningEvents.Add(item);
                                    }
                                    catch (Exception)
                                    {
                                        parsedEvent.Result = EventResult.Failure;
                                        item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
                                        item.EventType = EventType.DeleteListing;
                                        returningEvents.Add(item);
                                    }

                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
                if (returningEvents.Any())
                {
                    client = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostMultipleEvents");
                    request = new RestRequest();

                    message = new APIMessage();
                    message.APIKEY = CrunchEconCore.config.ApiKey;
                    var events = returningEvents.ToList();

                    message.JsonMessage = JsonConvert.SerializeObject(events);
                    request.AddStringBody(JsonConvert.SerializeObject(message), DataFormat.Json);

                    var result = await client.PostAsync(request);
                }
            }
            //  CrunchEconCore.Log.Info("done");
        }

        public static async Task UpdateBalances()
        {
            var events = new List<BalanceUpdateEvent>();

            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                events.Add(new BalanceUpdateEvent()
                {
                    OriginatingPlayerSteamId = player.Client.SteamUserId,
                    Balance = EconUtils.getBalance(player.Identity.IdentityId)
                });
            }

            if (events.Any())
            {
                var message = new APIMessage();
                message.APIKEY = CrunchEconCore.config.ApiKey;
                var eventMessage = new CrunchEconModels.Models.Events.Event();
                eventMessage.EventType = EventType.BalanceUpdate;
                eventMessage.JsonEvent = JsonConvert.SerializeObject(events);
                message.JsonMessage = JsonConvert.SerializeObject(eventMessage);
                var client = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostEvent");
                var request = new RestRequest();

                request.AddStringBody(JsonConvert.SerializeObject(message), DataFormat.Json);

                var result = await client.PostAsync(request);
            }
        }
        public static async Task<EventResult> HandleDeleteEvent(Event playerEvent, DeleteListingEvent deleteEvent)
        {
            var playerId = deleteEvent.OriginatingPlayerSteamId;
            if (!MySession.Static.Players.TryGetPlayerBySteamId(playerId, out var player)) return EventResult.NotOnline;

            if (!MyDefinitionId.TryParse(deleteEvent.Listing.ItemId,
                    out MyDefinitionId id)) return EventResult.ItemIdDoesntExist;


            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var sphere = new BoundingSphereD(player.Character.PositionComp.GetPosition(), 1000 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>()
                         .Where(x => x.Projector == null && x.BlocksCount >= 1))
            {
                inventories.AddRange(GetInventories(grid, player.Identity.IdentityId));
            }

            var ListedItem = deleteEvent.Listing;

            if (ListedItem.IsSelling || ListedItem.Amount > 0)
            {
                if (!SpawnItems(id, deleteEvent.Listing.Amount, inventories)) return EventResult.NotEnoughSpaceInCargo;
            }

            if (ListedItem.IsBuying && ListedItem.MaxAmountToBuy > 0)
            {
                EconUtils.addMoney(player.Identity.IdentityId, ListedItem.SellPricePerItem * ListedItem.MaxAmountToBuy);
            }

            return EventResult.Success;
        }

        public static async Task<EventResult> HandleBuyShipEvent(Event playerEvent, BuyShipEvent buyEvent)
        {
            var playerId = buyEvent.OriginatingPlayerSteamId;
            if (!MySession.Static.Players.TryGetPlayerBySteamId(playerId, out var player)) return EventResult.NotOnline;

            var balance = EconUtils.getBalance(player.Identity.IdentityId);
            if (balance < buyEvent.ShipListing.Price)
            {
                return EventResult.BuyerCannotAfford;
            }

            if (!File.Exists(CrunchEconCore.path + "//GridSelling//Grids//" + buyEvent.ShipListing.ShipPrefabName))
            {
                return EventResult.PrefabDoesntExist;
            }

            if (buyEvent.ShipListing.RequireReputation)
            {
                foreach (var faction in buyEvent.ShipListing.FactionTag.Split(','))
                {
                    var trim = faction.Trim();
                    var fac = MySession.Static.Factions.TryGetFactionByTag(trim);
                    if (fac == null)
                    {
                        return EventResult.FactionNotFound;
                    }

                    var rep = MySession.Static.Factions.GetRelationBetweenPlayerAndFaction(player.Identity.IdentityId,
                        fac.FactionId);
                    if (buyEvent.ShipListing.ReputationRequirement < 0)
                    {
                        if (rep.Item2 > buyEvent.ShipListing.ReputationRequirement)
                        {
                            return EventResult.NotEnoughReputation;
                        }
                    }
                    else
                    {
                        if (rep.Item2 < buyEvent.ShipListing.ReputationRequirement)
                        {
                            return EventResult.NotEnoughReputation;
                        }
                    }
                }
            }
            Vector3 Position = player.Character.PositionComp.GetPosition();
            Random random = new Random();

            Position.Add(new Vector3(random.Next(1000, 1000), random.Next(1000, 1000), random.Next(1000, 1000)));
            bool pasteResult = true;
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                if (!GridManager.LoadGrid(
                        CrunchEconCore.path + "//GridSelling//Grids//" + buyEvent.ShipListing.ShipPrefabName, Position,
                        false, buyEvent.OriginatingPlayerSteamId, buyEvent.ShipListing.ShipName, false))
                {
                    pasteResult = false;
                }
            });

            if (!pasteResult)
            {
                return EventResult.CouldntPasteGrid;
            }

            EconUtils.takeMoney(player.Identity.IdentityId, buyEvent.ShipListing.Price);

            return EventResult.Success;
        }

        public static async Task<EventResult> HandleBuyEvent(Event playerEvent, BuyItemEvent buyEvent)
        {
            var playerId = buyEvent.OriginatingPlayerSteamId;
            if (!MySession.Static.Players.TryGetPlayerBySteamId(playerId, out var player)) return EventResult.NotOnline;

            var balance = EconUtils.getBalance(player.Identity.IdentityId);
            if (balance < buyEvent.Price)
            {
                return EventResult.BuyerCannotAfford;
            }
            if (!MyDefinitionId.TryParse(buyEvent.DefinitionIdString,
                    out MyDefinitionId id)) return EventResult.ItemIdDoesntExist;


            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var sphere = new BoundingSphereD(player.Character.PositionComp.GetPosition(), 1000 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>()
                         .Where(x => x.Projector == null && x.BlocksCount >= 1))
            {
                inventories.AddRange(GetInventories(grid, player.Identity.IdentityId));
            }

            if (!SpawnItems(id, buyEvent.Amount, inventories)) return EventResult.NotEnoughSpaceInCargo;

            EconUtils.takeMoney(player.Identity.IdentityId, buyEvent.Price);

            if (!buyEvent.IsAdminSale)
            {
                var seller = CrunchEconCore.GetIdentityByNameOrId(buyEvent.SellerSteamId.ToString());
                EconUtils.addMoney(seller.IdentityId, buyEvent.Price);
            }
            return EventResult.Success;
        }
        public static async Task<EventResult> HandleListEvent(Event playerEvent, CreateListingEvent buyEvent)
        {
            var playerId = buyEvent.OriginatingPlayerSteamId;
            if (!MySession.Static.Players.TryGetPlayerBySteamId(playerId, out var player)) return EventResult.NotOnline;

            if (!MyDefinitionId.TryParse(buyEvent.Listing.ItemId,
                    out MyDefinitionId id)) return EventResult.ItemIdDoesntExist;

            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var sphere = new BoundingSphereD(player.Character.PositionComp.GetPosition(), 1000 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>()
                         .Where(x => x.Projector == null && x.BlocksCount >= 1))
            {
                inventories.AddRange(GetInventories(grid, player.Identity.IdentityId));
            }

            var comps = new Dictionary<MyDefinitionId, int>();
            comps.Add(id, buyEvent.Listing.Amount);

            var prio = (from inv in inventories let cargo = inv.Owner as IMyCargoContainer where cargo.DisplayNameText != null && cargo.DisplayNameText.ToLower().Contains("priority") select inv).Cast<VRage.Game.ModAPI.IMyInventory>().ToList();
            if (buyEvent.Listing.IsBuying)
            {
                if (EconUtils.getBalance(player.Identity.IdentityId) <
                    buyEvent.Listing.BuyPricePerItem * buyEvent.Listing.MaxAmountToBuy)
                {
                    return EventResult.BuyerCannotAfford;
                }
            }

            if (buyEvent.Listing.IsSelling)
            {
                if (!CrunchEconCore.ConsumeComponents(inventories, comps, buyEvent.OriginatingPlayerSteamId)) return EventResult.NotEnoughItems;
            }

            if (buyEvent.Listing.IsBuying)
            {
                EconUtils.takeMoney(player.Identity.IdentityId,
                    buyEvent.Listing.BuyPricePerItem * buyEvent.Listing.MaxAmountToBuy);
            }

            return EventResult.Success;
        }

        public static async Task<EventResult> HandleSellEvent(Event playerEvent, BuyItemEvent buyEvent)
        {
            var playerId = buyEvent.OriginatingPlayerSteamId;
            if (!MySession.Static.Players.TryGetPlayerBySteamId(playerId, out var player)) return EventResult.NotOnline;

            if (!MyDefinitionId.TryParse(buyEvent.DefinitionIdString,
                    out MyDefinitionId id)) return EventResult.ItemIdDoesntExist;

            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var sphere = new BoundingSphereD(player.Character.PositionComp.GetPosition(), 1000 * 2);
            foreach (var grid in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCubeGrid>()
                         .Where(x => x.Projector == null && x.BlocksCount >= 1))
            {
                inventories.AddRange(GetInventories(grid, player.Identity.IdentityId));
            }

            var comps = new Dictionary<MyDefinitionId, int>();
            comps.Add(id, buyEvent.Amount);

            var prio = (from inv in inventories let cargo = inv.Owner as IMyCargoContainer where cargo.DisplayNameText != null && cargo.DisplayNameText.ToLower().Contains("priority") select inv).Cast<VRage.Game.ModAPI.IMyInventory>().ToList();

            if (CrunchEconCore.ConsumeComponents(prio, comps, buyEvent.OriginatingPlayerSteamId))
            {
                EconUtils.addMoney(player.Identity.IdentityId, buyEvent.Price);

                return EventResult.Success;
            }

            if (!CrunchEconCore.ConsumeComponents(inventories, comps, buyEvent.OriginatingPlayerSteamId)) return EventResult.NotEnoughItems;

            EconUtils.addMoney(player.Identity.IdentityId, buyEvent.Price);

            return EventResult.Success;
        }

        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid, long playerIdentityId)
        {
            List<VRage.Game.ModAPI.IMyInventory> inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                if (block is IMyCargoContainer)
                {
                    switch (block.GetUserRelationToOwner(playerIdentityId))
                    {
                        case MyRelationsBetweenPlayerAndBlock.Owner:
                        case MyRelationsBetweenPlayerAndBlock.FactionShare:
                            for (int i = 0; i < block.InventoryCount; i++)
                            {
                                VRage.Game.ModAPI.IMyInventory
                                    inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                                inventories.Add(inv);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return inventories;
        }

        public static bool SpawnItems(MyDefinitionId id, MyFixedPoint amount, List<VRage.Game.ModAPI.IMyInventory> inventories)
        {
            //  CrunchEconCore.Log.Info("SPAWNING 1 " + amount);

            foreach (var inv in from inv in inventories let cargo = inv.Owner as IMyCargoContainer where cargo.DisplayNameText != null && cargo.DisplayNameText.ToLower().Contains("priority") select inv)
            {
                //   CrunchEconCore.Log.Info("priority cargo");
                MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                if (inv.CanItemsBeAdded(amount, itemType))
                {
                    inv.AddItems(amount,
                        (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializerKeen.CreateNewObject(id));
                    return true;
                }
            }
            foreach (var inv in inventories)
            {

                MyItemType itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                if (inv.CanItemsBeAdded(amount, itemType))
                {
                    inv.AddItems(amount,
                        (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializerKeen.CreateNewObject(id));
                    return true;
                }
            }

            return false;
        }
    }
}
