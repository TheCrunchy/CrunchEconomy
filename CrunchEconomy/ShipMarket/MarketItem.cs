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
        public int Pcu;
        public string Description;
        public int BlockCount;
       public float GridMass;

        public void AddTag(string Tag)
        {
            if (!GridTags.Contains(Tag))
            {
                GridTags.Add(Tag);
            }
        }
        public void RemoveTag(string Tag)
        {
            if (GridTags.Contains(Tag))
            {
                GridTags.Remove(Tag);
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
        public DateTime SoldAt;
        public ulong Buyer;
        public void Setup(List<MyCubeGrid> Grids, string Name, long Price, ulong SteamId)
        {
            Status = ItemStatus.Listed;
            this.Name = Name;
            this.Price = Price;
            this.SellerSteamId = SteamId;
            this.Description = "Not set.";
            foreach (var grid in Grids)
            {
                this.Pcu += grid.BlocksPCU;
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
        public void AddToCargo(MyDefinitionId Id, MyFixedPoint Amount)
        {
            if (Cargo.ContainsKey(Id.ToString()))
            {
                Cargo[Id.ToString()] += Amount;
            }
            else
            {
                Cargo.Add(Id.ToString(), Amount);
            }
        }
        public void AddToBlockCounts(string Type, string Subtype)
        {
            if (CountsOfBlocks.TryGetValue(Type, out var counts))
            {
                if (counts.ContainsKey(Subtype))
                {
                    //     blockCounts.Remove(type);
                    //     blockCounts.Add(type, value++);

                    //     Does this work?
                    counts[Subtype]++;
                }
                else
                {
                    counts.Add(Subtype, 1);
                }

            }
            else
            {
                var temp = new Dictionary<string, int>();
                temp.Add(Subtype, 1);
                CountsOfBlocks.Add(Type, temp);
            }
        }
    }
}
