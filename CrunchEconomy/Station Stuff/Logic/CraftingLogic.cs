using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;

namespace CrunchEconomy.Station_Stuff.Logic
{
    public static class CraftingLogic
    {
        public static void DoCrafting(Stations Station, DateTime Now)
        {
            if (Now < Station.NextCraftRefresh || !Station.EnableStationCrafting) return;
            if (MyAPIGateway.Entities.GetEntityById(Station.StationEntityId) == null) return;

            if (MyAPIGateway.Entities.GetEntityById(Station.StationEntityId) is MyCubeGrid grid)
            {
                var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                foreach (var item in Station.CraftableItems)
                {
                    var yeet = CrunchEconCore.Rnd.NextDouble();
                    if (!(yeet <= item.ChanceToCraft)) continue;
                    var comps = new Dictionary<MyDefinitionId, int>();
                    inventories.AddRange(InventoryLogic.GetInventories(grid, Station));
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.Typeid, item.Subtypeid, out var id)) continue;

                    foreach (var recipe in item.RequriedItems)
                    {
                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.Typeid, recipe.Subtypeid, out var id2))
                        {
                            comps.Add(id2, recipe.Amount);
                        }
                    }

                    if (!InventoryLogic.ConsumeComponents(inventories, comps, 0L)) continue;

                    InventoryLogic.SpawnItems(grid, id, item.AmountPerCraft, Station);
                    comps.Clear();
                    inventories.Clear();
                }
            }

            Station.NextCraftRefresh = DateTime.Now.AddSeconds(Station.SecondsBetweenCrafting);
        }
    }
}
