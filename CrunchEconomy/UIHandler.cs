using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CrunchEconModels.Models;
using CrunchEconModels.Models.Events;
using Newtonsoft.Json;
using Sandbox.Game.World;
using RestSharp;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
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
		public static bool SentDefinitions = false;
		public static async Task Handle()
		{
			try
			{
				if (!SentDefinitions)
				{
					SentDefinitions = true;
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
				await ProcessEvents();
			}
			catch (Exception e)
			{
				CrunchEconCore.Log.Error($"Event error {e}");
			}
			try
			{
				await UpdateBalances();

			}
			catch (Exception e)
			{
				CrunchEconCore.Log.Error($"Balance update error {e}");
			}

			//  CrunchEconCore.Log.Info("Done processing events");
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
				}
				
                var client2 = new RestClient($"{CrunchEconCore.config.UIURL}api/Event/PostEvent");
                var request2 = new RestRequest();
                var message2 = new APIMessage();
                message2.APIKEY = CrunchEconCore.config.ApiKey;
                returnevent.JsonEvent = JsonConvert.SerializeObject(new Event(){EventType = EventType.SaveTexturesJson});
                returnevent.EventType = EventType.SaveTexturesJson;
                message2.JsonMessage = JsonConvert.SerializeObject(returnevent);
                request2.AddStringBody(JsonConvert.SerializeObject(message2), DataFormat.Json);
                var response2 = await client2.PostAsync(request2);
				
				//CrunchEconCore.Log.Error("Sent");
			}
		}

		public static async Task ProcessEvents()
		{
			var players = new List<ulong>();
			foreach (var player in MySession.Static.Players.GetOnlinePlayers())
			{
				players.Add(player.Client.SteamUserId);
			}

			if (!players.Any())
			{
				return;
			}
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


								break;
							case EventType.BuyItem:
								{
									//         CrunchEconCore.Log.Info("6");
									var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
									var eventresult = await HandleBuyEvent(item, parsedEvent);
									parsedEvent.Result = eventresult;
									item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
									item.EventType = EventType.BuyItemResult;
									returningEvents.Add(item);
									//    CrunchEconCore.Log.Info(eventresult.ToString());
									break;
								}
							case EventType.SellItem:
								{
									var parsedEvent = JsonConvert.DeserializeObject<BuyItemEvent>(item.JsonEvent);
									var eventresult = await HandleSellEvent(item, parsedEvent);
									parsedEvent.Result = eventresult;
									item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
									item.EventType = EventType.SellItemResult;
									returningEvents.Add(item);
									break;
								}
							case EventType.DeleteListing:
								{
									var parsedEvent = JsonConvert.DeserializeObject<DeleteListingEvent>(item.JsonEvent);
									var eventresult = await HandleDeleteEvent(item, parsedEvent);
									parsedEvent.Result = eventresult;
									item.JsonEvent = JsonConvert.SerializeObject(parsedEvent);
									item.EventType = EventType.DeleteListing;
									returningEvents.Add(item);
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

			if (ListedItem.IsSelling && ListedItem.Amount > 0)
			{
				if (!SpawnItems(id, deleteEvent.Listing.Amount, inventories)) return EventResult.NotEnoughSpaceInCargo;
			}

			if (ListedItem.IsBuying && ListedItem.MaxAmountToBuy > 0)
			{
				EconUtils.addMoney(player.Identity.IdentityId,ListedItem.SellPricePerItem * ListedItem.MaxAmountToBuy);
			}

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

		public static async Task<EventResult> HandleListEvent(Event playerEvent)
		{
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
