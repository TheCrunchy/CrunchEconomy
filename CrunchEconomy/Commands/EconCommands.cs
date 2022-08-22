using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Station_Stuff.Logic;
using CrunchEconomy.Station_Stuff.Objects;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace CrunchEconomy.Commands
{
    [Category("crunchecon")]
    public class EconCommands : CommandModule
    {
        [Command("storeids", "grab ids from store offers")]
        [Permission(MyPromoteLevel.Admin)]
        public void GrabIds()
        {

            var sphere = new BoundingSphereD(Context.Player.GetPosition(), 400);

            foreach (var store in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyStoreBlock>())
            {

                foreach (var item in store.PlayerItems)
                {
                    Context.Respond(item.Item.Value.ToString());
                }

            }


        }


        [Command("generatefromgrid", "generate a station and configs from a grid")]
        [Permission(MyPromoteLevel.Admin)]
        public void GenerateFromGrid(string NewStationName, string CargoName, bool RemakeStoreFiles = false)
        {

            var sphere = new BoundingSphereD(Context.Player.GetPosition(), 400);
            var newStation = new Stations();
            newStation.Enabled = true;
            newStation.Name = NewStationName;
            newStation.CargoName = CargoName;
            newStation.ViewOnlyNamedCargo = true;
            newStation.WorldName = "default";
            var storeName = "";
            var done = false;
            foreach (var store in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyStoreBlock>())
            {
                if (!done)
                {
                    var gps = new MyGps();
                    gps.Name = "EconStationGPS";
                    gps.Coords = store.PositionComp.GetPosition();
                    newStation.StationGps = gps.ToString();
                    newStation.OwnerFactionTag = store.GetOwnerFactionTag();
                    done = true;
                }
                if (RemakeStoreFiles)
                {
                    foreach (var item in store.PlayerItems)
                    {

                        storeName = store.DisplayNameText;
                        if (item.StoreItemType == StoreItemTypes.Order)
                        {
                            if (!Directory.Exists(CrunchEconCore.Path + "//BuyOrders//" + storeName + "//"))
                            {
                                Directory.CreateDirectory(CrunchEconCore.Path + "//BuyOrders//" + storeName + "//");
                            }

                            var order = new BuyOrder();
                            order.MinAmount = item.Amount;
                            order.MaxAmount = item.Amount + 1;
                            order.MinPrice = item.PricePerUnit;
                            order.MaxPrice = item.PricePerUnit + 1;
                            order.SubtypeId = item.Item.Value.SubtypeName;
                            order.TypeId = item.Item.Value.TypeIdString;
                            order.Chance = 100;
                            order.Enabled = true;
                            CrunchEconCore.Utils.WriteToXmlFile<BuyOrder>(CrunchEconCore.Path + "//BuyOrders//" + storeName + "//" + order.TypeId + "-" + order.SubtypeId + ".xml", order);
                            //generate a folder for this store name
                            //this is what the store is buying

                        }

                        if (item.StoreItemType == StoreItemTypes.Offer)
                        {
                            //this is what the store is selling
                            if (!Directory.Exists(CrunchEconCore.Path + "//SellOffers//" + storeName + "//"))
                            {
                                Directory.CreateDirectory(CrunchEconCore.Path + "//SellOffers//" + storeName + "//");
                                var offer = new SellOffer();
                                offer.MinAmountToSpawn = item.Amount;
                                offer.MaxAmountToSpawn = item.Amount + 1;
                                offer.MinPrice = item.PricePerUnit;
                                offer.SpawnItemsIfNeeded = true;
                                offer.SpawnIfCargoLessThan = item.Amount;
                                offer.MaxPrice = item.PricePerUnit + 1;
                                offer.SubtypeId = item.Item.Value.SubtypeName;
                                offer.TypeId = item.Item.Value.TypeIdString;
                                offer.Chance = 100;
                                offer.Enabled = true;
                                CrunchEconCore.Utils.WriteToXmlFile<SellOffer>(CrunchEconCore.Path + "//SellOffers//" + storeName + "//" + offer.TypeId + "-" + offer.SubtypeId + ".xml", offer);
                            }
                        }
                    }
                }
            }
            if (done)
            {
                CrunchEconCore.Utils.WriteToXmlFile<Stations>(CrunchEconCore.Path + "//Stations//" + newStation.Name + ".xml", newStation);
                Context.Respond("Files generated, they have not been added to live data, !crunchecon reload or wait for auto loading.");
            }
            else
            {
                Context.Respond("Could not generate files");
            }


        }



        static object _syncRoot = new object();

        [Command("moneys", "view all money added through contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public async void HowMuchMoneys(String Type)
        {
            var watch = new Stopwatch();

            long sc = 0;
            if (Enum.TryParse(Type, out ContractType contract))
            {

                Context.Respond("Doing this async, may take a while");
                //  long output = await Task.Run(() => CountMoney(contract));
                watch.Start();
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(CrunchEconCore.Path + "//PlayerData//Mining//Completed//");
                    Parallel.ForEach(files, File =>
                    {
                        var contract1 = CrunchEconCore.Utils.ReadFromXmlFile<Contract>(File);
                        lock (_syncRoot)
                        {
                            sc += contract1.AmountPaid;
                        }
                    });
                  
                });
                Context.Respond(watch.ElapsedMilliseconds.ToString());
                Context.Respond(String.Format("{0:n0}", sc) + " SC Added to economy through " + Type + " contracts.");
                Context.Respond("Check the CrunchEcon logs folder for detailed output. Completed in " + watch.ElapsedMilliseconds + "ms");
            }
            else
            {
                Context.Respond("Not a valid contract type");
            }



        }

        public class LogObject
        {
            public Dictionary<string, Dictionary<string, long>> MoneyFromTypes = new Dictionary<string, Dictionary<string, long>>();
            public Dictionary<string, Dictionary<string, int>> AmountFromTypes = new Dictionary<string, Dictionary<string, int>>();
        }
        public Dictionary<long, LogObject> ContractLogs = new Dictionary<long, LogObject>();

        public LogObject AddToLog(LogObject Log, Contract Contract)
        {
            string timeKey;
            if (Contract.TimeCompleted != null)
            {
                timeKey = Contract.TimeCompleted.ToString("MM-dd-yyyy");
            }
            else
            {
                timeKey = DateTime.MinValue.ToString("MM-dd-yyyy");
            }
            if (Log.AmountFromTypes.ContainsKey(timeKey))
            {
                if (Log.AmountFromTypes[timeKey].ContainsKey(Contract.SubType))
                {
                    Log.AmountFromTypes[timeKey][Contract.SubType] += 1;

                }
                else
                {
                    Log.AmountFromTypes[timeKey].Add(Contract.SubType, 1);
                }
            }
            else
            {
                var temp = new Dictionary<string, int>();
                temp.Add(Contract.SubType, 1);
                Log.AmountFromTypes.Add(timeKey, temp);
            }
            if (Log.MoneyFromTypes.ContainsKey(timeKey))
            {
                if (Log.MoneyFromTypes[timeKey].ContainsKey(Contract.SubType))
                {
                    Log.MoneyFromTypes[timeKey][Contract.SubType] += Contract.AmountPaid;
                }
                else
                {
                    Log.MoneyFromTypes[timeKey].Add(Contract.SubType, Contract.AmountPaid);
                }
            }
            else
            {
                var temp = new Dictionary<string, long>();
                temp.Add(Contract.SubType, Contract.AmountPaid);
                Log.MoneyFromTypes.Add(timeKey, temp);
            }

            return Log;
        }
        public long CountMoney(ContractType Type)
        {
            var moneyFromTypes = new Dictionary<string, long>();
            var amountCompleted = new Dictionary<string, int>();

            // String timeformat = "MM-dd-yyyy";
            //ToString(timeformat);


            //store by station id 
            //store by subtype
            //store by date - amount completed, total money
            var megalog = new StringBuilder();
            long output = 0;
            megalog.AppendLine("StationId,TimeCompleted,SubType,AmountPaid,AmountDelivered,PlayerSteamId,ContractType");
            switch (Type)
            {

                case ContractType.Mining:

                    foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//PlayerData//Mining//Completed//"))
                    {

                        var contract = CrunchEconCore.Utils.ReadFromXmlFile<Contract>(s);
                        if (ContractLogs.TryGetValue(contract.StationEntityId, out var log))
                        {
                            log = AddToLog(log, contract);
                        }
                        else
                        {
                            var log2 = new LogObject();
                            log2 = AddToLog(log2, contract);
                            ContractLogs.Add(contract.StationEntityId, log2);
                        }
                        if (contract.TimeCompleted != null)
                        {
                            megalog.AppendLine(contract.StationEntityId + "," + contract.TimeCompleted + "," + contract.SubType + "," + contract.AmountPaid + "," + contract.AmountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.Type);
                        }
                        else
                        {
                            megalog.AppendLine(contract.StationEntityId + "," + DateTime.MinValue.ToString() + "," + contract.SubType + "," + contract.AmountPaid + "," + contract.AmountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.Type);
                        }


                        output += contract.AmountPaid;
                    }
                    break;
                case ContractType.Hauling:
                    foreach (var s in Directory.GetFiles(CrunchEconCore.Path + "//PlayerData//Hauling//Completed//"))
                    {


                        var contract = CrunchEconCore.Utils.ReadFromXmlFile<Contract>(s);
                        if (ContractLogs.TryGetValue(contract.StationEntityId, out var log))
                        {
                            log = AddToLog(log, contract);
                            ContractLogs[contract.StationEntityId] = log;
                        }
                        else
                        {
                            var log2 = new LogObject();
                            log2 = AddToLog(log2, contract);
                            ContractLogs.Add(contract.StationEntityId, log2);
                        }
                        if (contract.TimeCompleted != null)
                        {
                            megalog.AppendLine(contract.StationEntityId + "," + contract.TimeCompleted + "," + contract.TypeIfHauling + "/" + contract.SubType + "," + contract.AmountPaid + "," + contract.AmountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.Type);
                        }
                        else
                        {
                            megalog.AppendLine(contract.StationEntityId + "," + DateTime.MinValue.ToString() + "," + contract.TypeIfHauling + "/" + contract.SubType + "," + contract.AmountPaid + "," + contract.AmountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.Type);
                        }
                        output += contract.AmountPaid;

                    }
                    break;

            }

            var sb = new StringBuilder();
            sb.AppendLine("StationId,Date,SubType,Money,AmountCompleted");
            foreach (var station in ContractLogs)
            {

                foreach (var money in station.Value.MoneyFromTypes)
                {
                    foreach (var s in money.Value)
                    {
                        sb.AppendLine(station.Key + "," + money.Key + "," + s.Key + "," + s.Value + "," + station.Value.AmountFromTypes[money.Key][s.Key]);
                    }

                }

            }
            File.WriteAllText(CrunchEconCore.Path + "//Logs//NotMegaLog.txt", sb.ToString());
            File.WriteAllText(CrunchEconCore.Path + "//Logs//MEGALOG.txt", megalog.ToString());
            Context.Respond(sb.ToString());
            return output;
        }
        [Command("reload", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            Context.Respond("reloading");
            StoresLogic.IndividualTimers.Clear();
            CrunchEconCore.LoadConfig();
            CrunchEconCore.ConfigProvider.LoadStations();
            CrunchEconCore.ConfigProvider.LoadAllBuyOrders();
            CrunchEconCore.ConfigProvider.LoadAllSellOffers();
            CrunchEconCore.ConfigProvider.LoadAllGridSales();
        }

        [Command("pause", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Pause()
        {
            Context.Respond("Pausing the economy refreshing.");
            CrunchEconCore.Paused = true;
        }

        [Command("start", "start the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Start()
        {
            Context.Respond("Starting the economy refreshing.");
            CrunchEconCore.ConfigProvider.LoadStations();
            foreach (var station in CrunchEconCore.Stations)
            {
                station.NextBuyRefresh = DateTime.Now;
                station.NextSellRefresh = DateTime.Now;
            }
            CrunchEconCore.Paused = false;
        }
    }
}
