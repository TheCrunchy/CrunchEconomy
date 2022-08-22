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
        public static MyFixedPoint CountComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> Inventories, MyDefinitionId Id)
        {
            return Inventories.Select(Inv => Inv.FindItem(Id)).Where(InvItem => InvItem != null).Aggregate<IMyInventoryItem, MyFixedPoint>(0, (Current, InvItem) => Current + InvItem.Amount);
        }
        public static List<VRage.Game.ModAPI.IMyInventory> GetInventoriesForContract(MyCubeGrid Grid)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in Grid.GetFatBlocks())
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

        public static List<VRage.Game.ModAPI.IMyInventory> GetInventories(MyCubeGrid Grid, Stations Station)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();

            foreach (var block in Grid.GetFatBlocks().Where(Block => Block.GetOwnerFactionTag().Equals(Station.OwnerFactionTag)))
            {
                if (block is MyReactor reactor)
                {
                    continue;
                }
                if (Station.ViewOnlyNamedCargo)
                {
                    var temp = Station.CargoName.Split(',').ToList();
                    var cargos = temp.Select(Outer => Outer.Trim()).ToList();
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

        public static List<VRage.Game.ModAPI.IMyInventory> ClearInventories(MyCubeGrid Grid, Stations Station)
        {
            var inventories = new List<VRage.Game.ModAPI.IMyInventory>();
            var temp = Station.CargoName.Split(',').ToList();
            var cargos = temp.Select(Outer => Outer.Trim()).ToList();
            foreach (var block in Grid.GetFatBlocks().Where(Block => Block.GetOwnerFactionTag().Equals(Station.OwnerFactionTag)))
            {
                if (Station.ViewOnlyNamedCargo)
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

        public static bool SpawnLoot(MyCubeGrid Grid, MyDefinitionId Id, MyFixedPoint Amount)
        {
            if (Grid == null) return false;
            foreach (var block in Grid.GetFatBlocks())
            {


                for (var i = 0; i < block.InventoryCount; i++)
                {

                    var inv = ((VRage.Game.ModAPI.IMyCubeBlock)block).GetInventory(i);

                    var itemType = new MyInventoryItemFilter(Id.TypeId + "/" + Id.SubtypeName).ItemType;
                    if (!inv.CanItemsBeAdded(Amount, itemType)) continue;
                    inv.AddItems(Amount,
                        (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(Id));
                    return true;
                }
            }
            return false;
        }

        public static bool SpawnItems(MyCubeGrid Grid, MyDefinitionId Id, MyFixedPoint Amount, Stations Station)
        {
            //  CrunchEconCore.Log.Info("SPAWNING 1 " + amount);
            if (Grid == null) return false;
            var found = false;
            var temp = Station.CargoName.Split(',').ToList();
            var cargos = temp.Select(Outer => Outer.Trim()).ToList();
            foreach (var block in Grid.GetFatBlocks().Where(Block => Block.GetOwnerFactionTag().Equals(Station.OwnerFactionTag)))
            {
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    if (Station.ViewOnlyNamedCargo)
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
                    var itemType = new MyInventoryItemFilter(Id.TypeId + "/" + Id.SubtypeName).ItemType;
                    if (!inv.CanItemsBeAdded(Amount, itemType)) continue;
                    inv.AddItems(Amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(Id));
                    return true;
                }
            }
            return false;
        }

        public static bool ConsumeComponents(IEnumerable<VRage.Game.ModAPI.IMyInventory> Inventories, IDictionary<MyDefinitionId, int> Components, ulong Steamid)
        {
            var toRemove = new List<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, VRage.MyFixedPoint>>();
            foreach (var c in Components)
            {
                var needed = CountComponentsTwo(Inventories, c.Key, c.Value, toRemove);
                if (needed <= 0) continue;
                if (Steamid != 0L)
                {
                    CrunchEconCore.SendMessage("[Econ]", "Missing " + needed + " " + c.Key.SubtypeName + " All components must be inside one grid.", Color.Red, (long)Steamid);
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

        public static MyFixedPoint CountComponentsTwo(IEnumerable<VRage.Game.ModAPI.IMyInventory> Inventories, MyDefinitionId Id, int Amount, ICollection<MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>> Items)
        {
            MyFixedPoint targetAmount = Amount;
            foreach (var inv in Inventories)
            {
                var invItem = inv.FindItem(Id);
                if (invItem == null) continue;
                if (invItem.Amount >= targetAmount)
                {
                    Items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                    targetAmount = 0;
                    return targetAmount;
                }

                Items.Add(new MyTuple<VRage.Game.ModAPI.IMyInventory, VRage.Game.ModAPI.IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                targetAmount -= invItem.Amount;
            }
            return targetAmount;
        }
    }
}
