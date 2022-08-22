using System;
using System.Collections.Generic;
using System.IO;
using CrunchEconomy.Helpers;
using Sandbox.Game.World;

namespace CrunchEconomy.ShipMarket
{
    public class MarketList
    {
        private int _count = 0;
        public Dictionary<int, MarketItem> Items = new Dictionary<int, MarketItem>();

        public MarketItem GetItem(int Key)
        {
            if (Items.ContainsKey(Key))
            {
                return Items[Key];
            }
            return null;
        }
        FileUtils _utils = new FileUtils();
        public Dictionary<Guid, int> TempKeys = new Dictionary<Guid, int>();
        public void RefreshList()
        {
            TempKeys.Clear();
            foreach (var i in Items)
            {
                if (!TempKeys.ContainsKey(i.Value.ItemId))
                {
                    TempKeys.Add(i.Value.ItemId, i.Key);
                }
            }
            Items.Clear();
            foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//ShipMarket//ForSale"))
            {
                var item = _utils.ReadFromJsonFile<MarketItem>(s);
                if (TempKeys.ContainsKey(item.ItemId))
                {
                    Items.Add(TempKeys[item.ItemId], item);
                }
                else
                {
                    AddItem(item);
                }
            }
        }
        public void BuyShip(int Key, long BuyerId)
        {
            if (Items.ContainsKey(Key))
            {
                var item = Items[Key];
                var sellerId = TryGetIdentity(item.SellerSteamId.ToString());
                if (sellerId != null)
                {
                    EconUtils.AddMoney(sellerId.IdentityId, item.Price);
                    EconUtils.TakeMoney(BuyerId, item.Price);

                }
            }
        }
        public static MyIdentity TryGetIdentity(string PlayerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == PlayerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(PlayerNameOrSteamId, out var steamId))
                {
                    var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
        public Boolean AddItem(MarketItem Item)
        {
            var added = false;
            var attempt = _count +=1;
            while (!added)
            {
                if (!Items.ContainsKey(attempt))
                {
                    Items.Add(attempt, Item);
                    _count = attempt;
                    return true;
                }
                attempt++;
            }
          
            return false;
        }
        public Boolean RemoveItem(int Item)
        {
            if (Items.ContainsKey(Item))
            {
                Items.Remove(Item);
                return true;
            }
            return false;
        }

    }
}
