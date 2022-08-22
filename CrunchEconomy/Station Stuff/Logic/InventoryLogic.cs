using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchEconomy.Station_Stuff.Logic
{
    public static class InventoryLogic
    {
        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id)
        {
            return inventories.Select(inv => inv.FindItem(id)).Where(invItem => invItem != null).Aggregate<IMyInventoryItem, MyFixedPoint>(0, (current, invItem) => current + invItem.Amount);
        }
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventoriesForContract(MyCubeGrid grid)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks())
            {
                if (block is MyReactor reactor)
                {
                    continue;
                }
                for (var i = 0; i < block.InventoryCount; i++)
                {

                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }

            }
            return inventories;
        }

        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid grid, Stations station)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in grid.GetFatBlocks().Where(block => block.GetOwnerFactionTag().Equals(station.OwnerFactionTag)))
            {
                if (block is MyReactor reactor)
                {
                    continue;
                }
                if (station.ViewOnlyNamedCargo)
                {
                    var temp = station.CargoName.Split(',').ToList();
                    var cargos = temp.Select(outer => outer.Trim()).ToList();
                    if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText))
                    {
                        continue;
                    }
                }
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inventories.Add(inv);
                }
            }
            return inventories;
        }

        public static List<VRage.Game.ModAPI.IMyInventory> ClearInventories(MyCubeGrid grid, Stations station)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var temp = station.CargoName.Split(',').ToList();
            var cargos = temp.Select(outer => outer.Trim()).ToList();
            foreach (var block in grid.GetFatBlocks().Where(block => block.GetOwnerFactionTag().Equals(station.OwnerFactionTag)))
            {
                if (station.ViewOnlyNamedCargo)
                {
                    if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText))
                    {
                        continue;
                    }
                }
                for (var i = 0; i < block.InventoryCount; i++)
                {

                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);
                    inv.Clear();
                }
            }
            return inventories;
        }

        public static bool SpawnLoot(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount)
        {
            if (grid == null) return false;
            foreach (var block in grid.GetFatBlocks())
            {


                for (var i = 0; i < block.InventoryCount; i++)
                {

                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);

                    var itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                    if (!inv.CanItemsBeAdded(amount, itemType)) continue;
                    inv.AddItems(amount,
                        (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                    return true;
                }
            }
            return false;
        }

        public static bool SpawnItems(MyCubeGrid grid, MyDefinitionId id, MyFixedPoint amount, Stations station)
        {
            //  CrunchEconCore.Log.Info("SPAWNING 1 " + amount);
            if (grid == null) return false;
            var found = false;
            var temp = station.CargoName.Split(',').ToList();
            var cargos = temp.Select(outer => outer.Trim()).ToList();
            foreach (var block in grid.GetFatBlocks().Where(block => block.GetOwnerFactionTag().Equals(station.OwnerFactionTag)))
            {
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    if (station.ViewOnlyNamedCargo)
                    {
                        if (block.DisplayNameText != null && !cargos.Contains(block.DisplayNameText) && !found)
                        {
                            continue;
                        }
                        found = true;
                    }
                    else
                    {
                        found = true;
                    }

                    var inv = ((IMyCubeBlock)block).GetInventory(i);
                    if (!found) continue;
                    var itemType = new MyInventoryItemFilter(id.TypeId + "/" + id.SubtypeName).ItemType;
                    if (!inv.CanItemsBeAdded(amount, itemType)) continue;
                    inv.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
                    return true;
                }
            }
            return false;
        }

        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, IDictionary<MyDefinitionId, int> components, ulong steamid)
        {
            var toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (var c in components)
            {
                var needed = CountComponentsTwo(inventories, c.Key, c.Value, toRemove);
                if (needed <= 0) continue;
                if (steamid != 0L)
                {
                    CrunchEconCore.SendMessage("[Econ]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)steamid);
                }
                return false;
            }

            foreach (var item in toRemove)
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    item.Item1.RemoveItemAmount(item.Item2, item.Item3);
                });
            return true;
        }

        public static MyFixedPoint CountComponentsTwo(IEnumerable<VRage.Game.ModAPI.IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (var inv in inventories)
            {
                var invItem = inv.FindItem(id);
                if (invItem == null) continue;
                if (invItem.Amount >= targetAmount)
                {
                    items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                    targetAmount = 0;
                    return targetAmount;
                }

                items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                targetAmount -= invItem.Amount;
            }
            return targetAmount;
        }
    }
}
