using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using VRage;
using VRage.Game;
using VRage.Game.Entity;

namespace CrunchEconomy.ShipMarket
{
    public class MarketItem
    {
        public ulong SellerSteamId;
        public Guid ItemId = System.Guid.NewGuid();
        public long Price;
        public Dictionary<String, Dictionary<String, int>> CountsOfBlocks = new Dictionary<String, Dictionary<String, int>>();
        public Dictionary<string, MyFixedPoint> Cargo = new Dictionary<string, MyFixedPoint>();
        public string Name;
        public List<String> GridTags;
        public int PCU;
        public string Description;
        public int BlockCount;
       public float GridMass;

        public void AddTag(string tag)
        {
            if (!GridTags.Contains(tag))
            {
                GridTags.Add(tag);
            }
        }
        public void RemoveTag(string tag)
        {
            if (GridTags.Contains(tag))
            {
                GridTags.Remove(tag);
            }
        }
        public List<String> GetLowerTags()
        {
            var l = new List<string>();
            foreach (var s in GridTags)
            {
                l.Add(s.ToLower());
            }
            return l;
        }
        public ItemStatus Status;
        public DateTime soldAt;
        public ulong Buyer;
        public void Setup(List<MyCubeGrid> grids, string name, long price, ulong SteamId)
        {
            Status = ItemStatus.Listed;
            this.Name = name;
            this.Price = price;
            this.SellerSteamId = SteamId;
            this.Description = "Not set.";
            foreach (var grid in grids)
            {
                this.PCU += grid.BlocksPCU;
                this.BlockCount += grid.BlocksCount;
               this.GridMass += grid.Mass;
                foreach (var block in grid.GetFatBlocks())
                {
                    
                        AddToBlockCounts(block.BlockDefinition.Id.TypeId.ToString().Replace("MyObjectBuilder_", ""), block.BlockDefinition.Id.SubtypeName);
                    
                    if (block.HasInventory)
                    {
                        var items = new List<MyPhysicalInventoryItem>();
                        items = block.GetInventory().GetItems();
                        foreach (var item in items)
                        {
                            AddToCargo(item.Content.GetObjectId(), item.Amount);
                        }

                    }
                }
            }
            var sb = new StringBuilder();
            foreach (var keys in CountsOfBlocks)
            {
                sb.AppendLine(keys.Key);
                foreach (var key2 in keys.Value)
                {
                    sb.AppendLine(key2.Key + " - " + key2.Value);
                }
            }
        }
        public void AddToCargo(MyDefinitionId id, MyFixedPoint amount)
        {
            if (Cargo.ContainsKey(id.ToString()))
            {
                Cargo[id.ToString()] += amount;
            }
            else
            {
                Cargo.Add(id.ToString(), amount);
            }
        }
        public void AddToBlockCounts(string type, string subtype)
        {
            if (CountsOfBlocks.TryGetValue(type, out var counts))
            {
                if (counts.ContainsKey(subtype))
                {
                    //     blockCounts.Remove(type);
                    //     blockCounts.Add(type, value++);

                    //     Does this work?
                    counts[subtype]++;
                }
                else
                {
                    counts.Add(subtype, 1);
                }

            }
            else
            {
                var temp = new Dictionary<string, int>();
                temp.Add(subtype, 1);
                CountsOfBlocks.Add(type, temp);
            }
        }
    }
}
