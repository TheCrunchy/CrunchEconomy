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
        public static void DoCrafting(Stations station, DateTime now)
        {
            if (now < station.nextCraftRefresh || !station.EnableStationCrafting) return;
            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) == null) return;

            if (MyAPIGateway.Entities.GetEntityById(station.StationEntityId) is MyCubeGrid grid)
            {
                var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
                foreach (var item in station.CraftableItems)
                {
                    var yeet = CrunchEconCore.rnd.NextDouble();
                    if (!(yeet <= item.chanceToCraft)) continue;
                    var comps = new Dictionary<MyDefinitionId, int>();
                    inventories.AddRange(InventoryLogic.GetInventories(grid, station));
                    if (!MyDefinitionId.TryParse("MyObjectBuilder_" + item.typeid, item.subtypeid, out var id)) continue;

                    foreach (var recipe in item.RequriedItems)
                    {
                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + recipe.typeid, recipe.subtypeid, out var id2))
                        {
                            comps.Add(id2, recipe.amount);
                        }
                    }

                    if (!InventoryLogic.ConsumeComponents(inventories, comps, 0L)) continue;

                    InventoryLogic.SpawnItems(grid, id, item.amountPerCraft, station);
                    comps.Clear();
                    inventories.Clear();
                }
            }

            station.nextCraftRefresh = DateTime.Now.AddSeconds(station.SecondsBetweenCrafting);
        }
    }
}
