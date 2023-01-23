using CrunchEconomy.Contracts;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Groups;
using VRageMath;

namespace CrunchEconomy
{
    [Category("crunchecon")]
    public class EconCommands : CommandModule
    {
        [Command("storeids", "grab ids from store offers")]
        [Permission(MyPromoteLevel.Admin)]
        public void GrabIds()
        {

            BoundingSphereD sphere = new BoundingSphereD(Context.Player.GetPosition(), 400);

            foreach (MyStoreBlock store in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyStoreBlock>())
            {

                foreach (MyStoreItem item in store.PlayerItems)
                {
                    Context.Respond(item.Item.Value.ToString());
                }

            }


        }

        [Command("exportgrid", "export a grid to a sellable file")]
        [Permission(MyPromoteLevel.Admin)]
        public void ExportGrid()
        {
        
            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> gridWithSubGrids = GridFinder.FindLookAtGridGroup(Context.Player.Character);
            List<MyCubeGrid> grids = new List<MyCubeGrid>();
            foreach (var item in gridWithSubGrids)
            {
                foreach (MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Node groupNodes in item.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;

                    foreach (MyProjectorBase proj in grid.GetFatBlocks().OfType<MyProjectorBase>())
                    {
                        proj.Clipboard.Clear();
                    }
                    grids.Add(grid);
                    foreach (var block in grid.GetFatBlocks().OfType<MyCockpit>())
                    {
                        if (block.Pilot != null)
                        {
                            block.RemovePilot();
                        }
                    }

                }

            }
            if (grids.Count == 0)
            {
                Context.Respond("Could not find grid.");
                return;
            }

            string gridname = grids.First().GetBiggestGridInGroup().DisplayName;
            var gridsell = new GridSale();
            gridsell.Enabled = true;
            gridsell.ExportedGridName = $"{gridname}";
            gridsell.GiveOwnerShipToNPC = false;
            GridManager.SaveGridNoDelete($"{CrunchEconCore.path}\\GridSelling\\Grids\\{gridname}.sbc", $"{gridname}.sbc", false, false, grids);
            CrunchEconCore.utils.WriteToXmlFile($"{CrunchEconCore.path}\\GridSelling\\{gridname}.xml", gridsell);

            CrunchEconCore.LoadAllGridSales();
            Context.Respond($"Exported grid: {gridname}");

        }

        [Command("generatefromgrid", "generate a station and configs from a grid")]
        [Permission(MyPromoteLevel.Admin)]
        public void GenerateFromGrid(string NewStationName, string CargoName, bool RemakeStoreFiles = false)
        {

            BoundingSphereD sphere = new BoundingSphereD(Context.Player.GetPosition(), 400);
            Stations newStation = new Stations();
            newStation.Enabled = true;
            newStation.Name = NewStationName;
            newStation.CargoName = CargoName;
            newStation.ViewOnlyNamedCargo = true;
            newStation.WorldName = "default";
            var StoreName = "";
            bool done = false;
            foreach (MyStoreBlock store in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyStoreBlock>())
            {
                if (!done)
                {
                    MyGps gps = new MyGps();
                    gps.Name = "EconStationGPS";
                    gps.Coords = store.PositionComp.GetPosition();
                    newStation.stationGPS = gps.ToString();
                    newStation.OwnerFactionTag = store.GetOwnerFactionTag();
                    done = true;
                }
                if (RemakeStoreFiles)
                {
                    foreach (MyStoreItem item in store.PlayerItems)
                    {

                        StoreName = store.DisplayNameText;
                        if (item.StoreItemType == StoreItemTypes.Order)
                        {
                            if (!Directory.Exists(CrunchEconCore.path + "//BuyOrders//" + StoreName + "//"))
                            {
                                Directory.CreateDirectory(CrunchEconCore.path + "//BuyOrders//" + StoreName + "//");
                            }

                            BuyOrder order = new BuyOrder();
                            order.minAmount = item.Amount;
                            order.maxAmount = item.Amount + 1;
                            order.minPrice = item.PricePerUnit;
                            order.maxPrice = item.PricePerUnit + 1;
                            order.subtypeId = item.Item.Value.SubtypeName;
                            order.typeId = item.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "");
                            order.chance = 1;
                            order.Enabled = true;
                            CrunchEconCore.utils.WriteToXmlFile<BuyOrder>(CrunchEconCore.path + "//BuyOrders//" + StoreName + "//" + order.typeId + "-" + order.subtypeId + ".xml", order);
                            //generate a folder for this store name
                            //this is what the store is buying

                        }

                        if (item.StoreItemType == StoreItemTypes.Offer)
                        {
                            //this is what the store is selling
                            if (!Directory.Exists(CrunchEconCore.path + "//SellOffers//" + StoreName + "//"))
                            {
                                Directory.CreateDirectory(CrunchEconCore.path + "//SellOffers//" + StoreName + "//");
                                SellOffer offer = new SellOffer();
                                offer.minAmountToSpawn = item.Amount;
                                offer.maxAmountToSpawn = item.Amount + 1;
                                offer.minPrice = item.PricePerUnit;
                                offer.SpawnItemsIfNeeded = true;
                                offer.SpawnIfCargoLessThan = item.Amount;
                                offer.maxPrice = item.PricePerUnit + 1;
                                offer.subtypeId = item.Item.Value.SubtypeName;
                                offer.typeId = item.Item.Value.TypeIdString.Replace("MyObjectBuilder_", "");
                                offer.chance = 1;
                                offer.Enabled = true;
                                CrunchEconCore.utils.WriteToXmlFile<SellOffer>(CrunchEconCore.path + "//SellOffers//" + StoreName + "//" + offer.typeId + "-" + offer.subtypeId + ".xml", offer);
                            }
                        }
                    }
                }
            }
            if (done)
            {
                CrunchEconCore.utils.WriteToXmlFile<Stations>(CrunchEconCore.path + "//Stations//" + newStation.Name + ".xml", newStation);
                Context.Respond("Files generated, they have not been added to live data, !crunchecon reload or wait for auto loading.");
            }
            else
            {
                Context.Respond("Could not generate files");
            }


        }



        static object syncRoot = new object();

        [Command("moneys", "view all money added through contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public async void HowMuchMoneys(String type)
        {
            Stopwatch watch = new Stopwatch();

            long sc = 0;
            if (Enum.TryParse(type, out ContractType contract))
            {

                Context.Respond("Doing this async, may take a while");
                //  long output = await Task.Run(() => CountMoney(contract));
                watch.Start();
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(CrunchEconCore.path + "//PlayerData//Mining//Completed//");
                    Parallel.ForEach(files, file =>
                    {
                        Contract contract1 = CrunchEconCore.utils.ReadFromXmlFile<Contract>(file);
                        lock (syncRoot)
                        {
                            sc += contract1.AmountPaid;
                        }
                    });

                });
                Context.Respond(watch.ElapsedMilliseconds.ToString());
                Context.Respond(String.Format("{0:n0}", sc) + " SC Added to economy through " + type + " contracts.");
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
        public Dictionary<long, LogObject> contractLogs = new Dictionary<long, LogObject>();

        public LogObject AddToLog(LogObject log, Contract contract)
        {
            string timeKey;
            if (contract.TimeCompleted != null)
            {
                timeKey = contract.TimeCompleted.ToString("MM-dd-yyyy");
            }
            else
            {
                timeKey = DateTime.MinValue.ToString("MM-dd-yyyy");
            }
            if (log.AmountFromTypes.ContainsKey(timeKey))
            {
                if (log.AmountFromTypes[timeKey].ContainsKey(contract.SubType))
                {
                    log.AmountFromTypes[timeKey][contract.SubType] += 1;

                }
                else
                {
                    log.AmountFromTypes[timeKey].Add(contract.SubType, 1);
                }
            }
            else
            {
                Dictionary<string, int> temp = new Dictionary<string, int>();
                temp.Add(contract.SubType, 1);
                log.AmountFromTypes.Add(timeKey, temp);
            }
            if (log.MoneyFromTypes.ContainsKey(timeKey))
            {
                if (log.MoneyFromTypes[timeKey].ContainsKey(contract.SubType))
                {
                    log.MoneyFromTypes[timeKey][contract.SubType] += contract.AmountPaid;
                }
                else
                {
                    log.MoneyFromTypes[timeKey].Add(contract.SubType, contract.AmountPaid);
                }
            }
            else
            {
                Dictionary<string, long> temp = new Dictionary<string, long>();
                temp.Add(contract.SubType, contract.AmountPaid);
                log.MoneyFromTypes.Add(timeKey, temp);
            }

            return log;
        }
        public long CountMoney(ContractType type)
        {
            Dictionary<string, long> MoneyFromTypes = new Dictionary<string, long>();
            Dictionary<string, int> AmountCompleted = new Dictionary<string, int>();

            // String timeformat = "MM-dd-yyyy";
            //ToString(timeformat);


            //store by station id 
            //store by subtype
            //store by date - amount completed, total money
            StringBuilder MEGALOG = new StringBuilder();
            long output = 0;
            MEGALOG.AppendLine("StationId,TimeCompleted,SubType,AmountPaid,AmountDelivered,PlayerSteamId,ContractType");
            switch (type)
            {

                case ContractType.Mining:

                    foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//PlayerData//Mining//Completed//"))
                    {

                        Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(s);
                        if (contractLogs.TryGetValue(contract.StationEntityId, out LogObject log))
                        {
                            log = AddToLog(log, contract);
                        }
                        else
                        {
                            LogObject log2 = new LogObject();
                            log2 = AddToLog(log2, contract);
                            contractLogs.Add(contract.StationEntityId, log2);
                        }
                        if (contract.TimeCompleted != null)
                        {
                            MEGALOG.AppendLine(contract.StationEntityId + "," + contract.TimeCompleted + "," + contract.SubType + "," + contract.AmountPaid + "," + contract.amountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.type);
                        }
                        else
                        {
                            MEGALOG.AppendLine(contract.StationEntityId + "," + DateTime.MinValue.ToString() + "," + contract.SubType + "," + contract.AmountPaid + "," + contract.amountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.type);
                        }


                        output += contract.AmountPaid;
                    }
                    break;
                case ContractType.Hauling:
                    foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//PlayerData//Hauling//Completed//"))
                    {


                        Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(s);
                        if (contractLogs.TryGetValue(contract.StationEntityId, out LogObject log))
                        {
                            log = AddToLog(log, contract);
                            contractLogs[contract.StationEntityId] = log;
                        }
                        else
                        {
                            LogObject log2 = new LogObject();
                            log2 = AddToLog(log2, contract);
                            contractLogs.Add(contract.StationEntityId, log2);
                        }
                        if (contract.TimeCompleted != null)
                        {
                            MEGALOG.AppendLine(contract.StationEntityId + "," + contract.TimeCompleted + "," + contract.TypeIfHauling + "/" + contract.SubType + "," + contract.AmountPaid + "," + contract.amountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.type);
                        }
                        else
                        {
                            MEGALOG.AppendLine(contract.StationEntityId + "," + DateTime.MinValue.ToString() + "," + contract.TypeIfHauling + "/" + contract.SubType + "," + contract.AmountPaid + "," + contract.amountToMineOrDeliver + "," + contract.PlayerSteamId + "," + contract.type);
                        }
                        output += contract.AmountPaid;

                    }
                    break;

            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("StationId,Date,SubType,Money,AmountCompleted");
            foreach (KeyValuePair<long, LogObject> station in contractLogs)
            {

                foreach (KeyValuePair<string, Dictionary<string, long>> money in station.Value.MoneyFromTypes)
                {
                    foreach (KeyValuePair<string, long> s in money.Value)
                    {
                        sb.AppendLine(station.Key + "," + money.Key + "," + s.Key + "," + s.Value + "," + station.Value.AmountFromTypes[money.Key][s.Key]);
                    }

                }

            }
            File.WriteAllText(CrunchEconCore.path + "//Logs//NotMegaLog.txt", sb.ToString());
            File.WriteAllText(CrunchEconCore.path + "//Logs//MEGALOG.txt", MEGALOG.ToString());
            Context.Respond(sb.ToString());
            return output;
        }
        [Command("reload", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            Context.Respond("reloading");
            CrunchEconCore.individualTimers.Clear();
            CrunchEconCore.LoadConfig();
            CrunchEconCore.LoadAllStations();
            CrunchEconCore.LoadAllBuyOrders();
            CrunchEconCore.LoadAllSellOffers();
            CrunchEconCore.LoadAllGridSales();
        }
        [Command("pause", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Pause()
        {
            Context.Respond("Pausing the economy refreshing.");
            CrunchEconCore.paused = true;
        }

        [Command("start", "start the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Start()
        {
            Context.Respond("Starting the economy refreshing.");
            CrunchEconCore.LoadAllStations();
            foreach (Stations station in CrunchEconCore.stations)
            {
                station.nextBuyRefresh = DateTime.Now;
                station.nextSellRefresh = DateTime.Now;
                station.nextCraftRefresh = DateTime.Now;
            }
            CrunchEconCore.paused = false;
        }


    }
}
