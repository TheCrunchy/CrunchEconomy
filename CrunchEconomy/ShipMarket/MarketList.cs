﻿using CrunchEconomy;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipMarket
{
    public class MarketList
    {
        private int count = 0;
        public Dictionary<int, MarketItem> items = new Dictionary<int, MarketItem>();

        public MarketItem GetItem(int key)
        {
            if (items.ContainsKey(key))
            {
                return items[key];
            }
            return null;
        }
        FileUtils utils = new FileUtils();
        public Dictionary<Guid, int> tempKeys = new Dictionary<Guid, int>();
        public void RefreshList()
        {
            tempKeys.Clear();
            foreach (KeyValuePair<int, MarketItem> i in items)
            {
                if (!tempKeys.ContainsKey(i.Value.ItemId))
                {
                    tempKeys.Add(i.Value.ItemId, i.Key);
                }
            }
            items.Clear();
            foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//ShipMarket//ForSale"))
            {
                MarketItem item = utils.ReadFromJsonFile<MarketItem>(s);
                if (tempKeys.ContainsKey(item.ItemId))
                {
                    items.Add(tempKeys[item.ItemId], item);
                }
                else
                {
                    AddItem(item);
                }
            }
        }
        public void BuyShip(int key, long BuyerId)
        {
            if (items.ContainsKey(key))
            {
                MarketItem item = items[key];
                MyIdentity SellerId = TryGetIdentity(item.SellerSteamId.ToString());
                if (SellerId != null)
                {
                    EconUtils.addMoney(SellerId.IdentityId, item.Price);
                    EconUtils.takeMoney(BuyerId, item.Price);

                }
            }
        }
        public static MyIdentity TryGetIdentity(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
        public Boolean AddItem(MarketItem item)
        {
            bool added = false;
            int attempt = count +=1;
            while (!added)
            {
                if (!items.ContainsKey(attempt))
                {
                    items.Add(attempt, item);
                    count = attempt;
                    return true;
                }
                attempt++;
            }
          
            return false;
        }
        public Boolean RemoveItem(int item)
        {
            if (items.ContainsKey(item))
            {
                items.Remove(item);
                return true;
            }
            return false;
        }

    }
}
