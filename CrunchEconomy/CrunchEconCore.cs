using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using Torch.API.Managers;
using System.IO;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Torch.Mod.Messages;
using Torch.Mod;
using Torch.Managers.ChatManager;
using Torch.Managers;
using Torch.API.Plugins;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;
using Sandbox.Game.Screens.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using NLog;
using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using static CrunchEconomy.Station_Stuff.Objects.Stations;
using static CrunchEconomy.Contracts.GeneratedContract;
using static CrunchEconomy.Station_Stuff.Objects.RepConfig;
using System.Threading.Tasks;
using CrunchEconomy.Station_Stuff;
using CrunchEconomy.Station_Stuff.Logic;
using CrunchEconomy.Station_Stuff.Objects;
using CrunchEconomy.Storage;
using CrunchEconomy.Storage.Interfaces;
using static CrunchEconomy.Station_Stuff.Objects.WhitelistFile;
using Sandbox.Definitions;
using IMyInventoryItem = VRage.Game.ModAPI.IMyInventoryItem;

namespace CrunchEconomy
{
    public class CrunchEconCore : TorchPluginBase
    {

        public static TorchSessionState TorchState;
        public static ITorchBase TorchBase;

        public static Logger Log = LogManager.GetLogger("CrunchEcon");

        public static Config config;

        private TorchSessionManager sessionManager;
        public static string path;
        public static string basePath;

        public static bool paused = false;

        public static IPlayerDataProvider PlayerStorageProvider { get; set; }
        public static IConfigProvider ConfigProvider { get; set; }

        int ticks = 0;
        public static void SendMessage(string author, string message, Color color, long steamID)
        {
            var _chatLog = LogManager.GetLogger("Chat");
            var scriptedChatMsg1 = new ScriptedChatMsg
            {
                Author = author,
                Text = message,
                Font = "White",
                Color = color,
                Target = Sync.Players.TryGetIdentityId((ulong)steamID)
            };
            var scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }

        //got bored, this being async probably doesnt matter at all 
        public static async void DoFactionShit(IPlayer p)
        {

            var iden = GetIdentityByNameOrId(p.SteamId.ToString());
            if (iden == null) return;
            var player = MySession.Static.Factions.TryGetPlayerFaction(iden.IdentityId) as MyFaction;
            await Task.Run(() =>
            {
                foreach (var item in ConfigProvider.RepConfig.RepConfigs)
                {
                    if (!item.Enabled) continue;
                    var target = MySession.Static.Factions.TryGetFactionByTag(item.FactionTag);
                    if (target == null) continue;
                    MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(iden.IdentityId, target.FactionId, item.PlayerToFactionRep);
                    if (player != null)
                    {
                        MySession.Static.Factions.SetReputationBetweenFactions(player.FactionId, target.FactionId, item.FactionToFactionRep);
                    }
                }
            });
        }

        public static void Login(IPlayer player)
        {
            if (CrunchEconCore.config != null && !CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (player == null)
            {
                return;
            }

            DoFactionShit(player);

            var data = PlayerStorageProvider.GetPlayerData(player.SteamId);
            data.GetHaulingContracts();
            data.GetMiningContracts();
            data.GetMission();
            var id = MySession.Static.Players.TryGetIdentityId(player.SteamId);
            if (id == 0)
            {
                return;
            }
            var playerList = new List<IMyGps>();
            MySession.Static.Gpss.GetGpsList(id, playerList);
            foreach (var gps in playerList.Where(Gps => Gps.Description != null && Gps.Description.Contains("Contract Delivery Location.")))
            {
                MyAPIGateway.Session?.GPS.RemoveGps(id, gps);
            }

            foreach (var c in data.GetMiningContracts().Values.Where(c => c.minedAmount >= c.amountToMineOrDeliver))
            {
                c.DoPlayerGps(id);
            }

            foreach (var c in data.GetHaulingContracts().Values)
            {
                c.DoPlayerGps(id);
            }
            var iden = GetIdentityByNameOrId(player.SteamId.ToString());
            if (iden == null) return;

            var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

            //this as linq no work
            foreach (var gps in from stat in stations where stat.GiveGPSOnLogin where stat.getGPS() != null select stat.getGPS())
            {
                var myGps = gps;
                myGps.DiscardAt = new TimeSpan(6000);
                gpscol.SendAddGpsRequest(iden.IdentityId, ref myGps);
            }
        }

        public static bool AlliancePluginEnabled = false;

        public static string GetPlayerName(ulong steamId)
        {
            var id = GetIdentityByNameOrId(steamId.ToString());
            return id?.DisplayName ?? steamId.ToString();
        }
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (!ulong.TryParse(playerNameOrSteamId, out var steamId)) continue;
                var id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                if (id == steamId)
                    return identity;
                if (identity.IdentityId == (long)steamId)
                    return identity;

            }
            return null;
        }
        public void RefreshWhitelists()
        {
            CrunchEconCore.Log.Info("Redoing station whitelists"); 
            Task.Run(() =>
            {
                foreach (var station in stations)
                {
                    StoresLogic.RefreshWhitelists(station);
                }
            });
        }

        public static Random rnd = new Random();
        private DateTime NextWhitelist = DateTime.Now.AddMinutes(15);
        public override void Update()
        {
            ticks++;
            if (paused)
            {
                return;
            }
            if (CrunchEconCore.config == null)
            {
                return;
            }
            if (!CrunchEconCore.config.PluginEnabled)
            {
                return;
            }

            if (ticks % 256 == 0 && TorchState == TorchSessionState.Loaded)
            {

                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (config.MiningContractsEnabled || config.HaulingContractsEnabled)
                    {
                        if (DateTime.Now >= ContractUtils.chat)
                        {
                            ContractUtils.chat = DateTime.Now.AddMinutes(10);
                            ContractLogic.DoContractDelivery(player, true);
                        }
                        else
                        {
                            ContractLogic.DoContractDelivery(player, false);
                        }
                    }

                    if (config == null || !config.SurveyContractsEnabled) continue;
                    var data = PlayerStorageProvider.GetPlayerData(player.Id.SteamId);
                    SurveyLogic.GenerateNewSurveyMission(data, player);
                }

            }

            if (ticks % 64 != 0 || TorchState != TorchSessionState.Loaded) return;

            PlayerStorageProvider.SaveContracts();
            var now = DateTime.Now;
            foreach (var station in stations)
            {
                //first check if its any, then we can load the grid to do the editing
                CraftingLogic.DoCrafting(station, now);
                StoresLogic.DoStationRefresh(station, now);
                if (now < NextWhitelist) continue;
                RefreshWhitelists();
                NextWhitelist = NextWhitelist.AddMinutes(15);
            }

        }

        public void SaveStation(Stations Station)
        {
            ConfigProvider.SaveStation(Station);
        }

        public static List<Stations> stations = new List<Stations>();


        public static FileUtils utils = new FileUtils();

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            TorchState = state;
            if (!CrunchEconCore.config.PluginEnabled && config != null)
            {
                return;
            }

            if (state != TorchSessionState.Loaded) return;


            ConfigProvider = new XmlConfigProvider(path);
            PlayerStorageProvider = new JsonPlayerDataProvider(path);

            if (config.SetMinPricesTo1)
            {
                sessionManager.AddOverrideMod(2825413709);
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    if (def is MyComponentDefinition definition)
                    {
                        definition.MinimalPricePerUnit = 1;
                    }
                    if (def is MyPhysicalItemDefinition itemDefinition)
                    {
                        itemDefinition.MinimalPricePerUnit = 1;
                    }
                }
            }
            session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += Login;

            if (session.Managers.GetManager<PluginManager>().Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"), out var All))
            {
                var alli = All.GetType().Assembly.GetType("AlliancesPlugin.AlliancePlugin");
                try
                {
                    AllianceTaxes = All.GetType().GetMethod("AddToTaxes", BindingFlags.Public | BindingFlags.Static, null, new Type[4] { typeof(ulong), typeof(long), typeof(string), typeof(Vector3D) }, null);
                    //    BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                }
                catch (Exception ex)
                {
                    Log.Error("Error getting alliance taxes");

                }
                Alliance = All;
                AlliancePluginEnabled = true;
            }

            try
            {
                ContractUtils.LoadAllContracts();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            try
            {
                ConfigProvider.LoadAllGridSales();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading grid sales " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadAllSellOffers();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Sell offers " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadStations();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Stations " + ex.ToString());


            }
            try
            {
                ConfigProvider.LoadAllBuyOrders();
            }
            catch (Exception ex)
            {
                Log.Error("Error loading Buy Orders " + ex.ToString());


            }
        }

        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();
            if (!CrunchEconCore.config.PluginEnabled)
            {
                return;
            }
            if (!Directory.Exists(path + "//Logs//"))
            {
                Directory.CreateDirectory(path + "//Logs//");
            }
            TorchBase = Torch;
        }

        public static MethodInfo AllianceTaxes;

        public static ITorchPlugin Alliance;

        public void SetupConfig()
        {

            path = StoragePath;

            if (File.Exists(StoragePath + "\\CrunchEconomy.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            folder = config.StoragePath.Equals("default") ? Path.Combine(StoragePath + "//CrunchEcon//") : config.StoragePath;
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder2 = config.StoragePath + "//CrunchEcon//";
            }

            Directory.CreateDirectory(folder2);


            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            var utils = new FileUtils();

            config = utils.ReadFromXmlFile<Config>(basePath + "\\CrunchEconomy.xml");
            return config;
        }
    }
}
