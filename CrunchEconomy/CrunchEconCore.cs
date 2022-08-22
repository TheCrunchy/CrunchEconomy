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
using CrunchEconomy.Helpers;
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

        public static Config Config;

        private TorchSessionManager _sessionManager;
        public static string Path;
        public static string BasePath;

        public static bool Paused = false;

        public static IPlayerDataProvider PlayerStorageProvider { get; set; }
        public static IConfigProvider ConfigProvider { get; set; }

        int _ticks = 0;
        public static void SendMessage(string Author, string Message, Color Color, long SteamId)
        {
            var chatLog = LogManager.GetLogger("Chat");
            var scriptedChatMsg1 = new ScriptedChatMsg
            {
                Author = Author,
                Text = Message,
                Font = "White",
                Color = Color,
                Target = Sync.Players.TryGetIdentityId((ulong)SteamId)
            };
            var scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }

        //got bored, this being async probably doesnt matter at all 
        public static async void DoFactionShit(IPlayer P)
        {

            var iden = GetIdentityByNameOrId(P.SteamId.ToString());
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

        public static void Login(IPlayer Player)
        {
            if (CrunchEconCore.Config != null && !CrunchEconCore.Config.PluginEnabled)
            {
                return;
            }
            if (Player == null)
            {
                return;
            }

            DoFactionShit(Player);

            var data = PlayerStorageProvider.GetPlayerData(Player.SteamId);
            data.GetHaulingContracts();
            data.GetMiningContracts();
            data.GetMission();
            var id = MySession.Static.Players.TryGetIdentityId(Player.SteamId);
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

            foreach (var c in data.GetMiningContracts().Values.Where(C => C.MinedAmount >= C.AmountToMineOrDeliver))
            {
                c.DoPlayerGps(id);
            }

            foreach (var c in data.GetHaulingContracts().Values)
            {
                c.DoPlayerGps(id);
            }
            var iden = GetIdentityByNameOrId(Player.SteamId.ToString());
            if (iden == null) return;

            var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

            //this as linq no work
            foreach (var gps in from stat in Stations where stat.GiveGpsOnLogin where stat.GetGps() != null select stat.GetGps())
            {
                var myGps = gps;
                myGps.DiscardAt = new TimeSpan(6000);
                gpscol.SendAddGpsRequest(iden.IdentityId, ref myGps);
            }
        }

        public static bool AlliancePluginEnabled = false;

        public static string GetPlayerName(ulong SteamId)
        {
            var id = GetIdentityByNameOrId(SteamId.ToString());
            return id?.DisplayName ?? SteamId.ToString();
        }
        public static MyIdentity GetIdentityByNameOrId(string PlayerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == PlayerNameOrSteamId)
                    return identity;
                if (!ulong.TryParse(PlayerNameOrSteamId, out var steamId)) continue;
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
                foreach (var station in Stations)
                {
                    StoresLogic.RefreshWhitelists(station);
                }
            });
        }

        public static Random Rnd = new Random();
        private DateTime _nextWhitelist = DateTime.Now.AddMinutes(15);
        public override void Update()
        {
            _ticks++;
            if (Paused)
            {
                return;
            }
            if (CrunchEconCore.Config == null)
            {
                return;
            }
            if (!CrunchEconCore.Config.PluginEnabled)
            {
                return;
            }

            if (_ticks % 256 == 0 && TorchState == TorchSessionState.Loaded)
            {

                foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (Config.MiningContractsEnabled || Config.HaulingContractsEnabled)
                    {
                        if (DateTime.Now >= ContractUtils.Chat)
                        {
                            ContractUtils.Chat = DateTime.Now.AddMinutes(10);
                            ContractLogic.DoContractDelivery(player, true);
                        }
                        else
                        {
                            ContractLogic.DoContractDelivery(player, false);
                        }
                    }

                    if (Config == null || !Config.SurveyContractsEnabled) continue;
                    var data = PlayerStorageProvider.GetPlayerData(player.Id.SteamId);
                    SurveyLogic.GenerateNewSurveyMission(data, player);
                }

            }

            if (_ticks % 64 != 0 || TorchState != TorchSessionState.Loaded) return;

            PlayerStorageProvider.SaveContracts();
            var now = DateTime.Now;
            foreach (var station in Stations)
            {
                //first check if its any, then we can load the grid to do the editing
                CraftingLogic.DoCrafting(station, now);
                StoresLogic.DoStationRefresh(station, now);
                if (now < _nextWhitelist) continue;
                RefreshWhitelists();
                _nextWhitelist = _nextWhitelist.AddMinutes(15);
            }

        }

        public void SaveStation(Stations Station)
        {
            ConfigProvider.SaveStation(Station);
        }

        public static List<Stations> Stations = new List<Stations>();


        public static FileUtils Utils = new FileUtils();

        private void SessionChanged(ITorchSession Session, TorchSessionState State)
        {
            TorchState = State;
            if (!CrunchEconCore.Config.PluginEnabled && Config != null)
            {
                return;
            }

            if (State != TorchSessionState.Loaded) return;


            ConfigProvider = new XmlConfigProvider(Path);
            PlayerStorageProvider = new JsonPlayerDataProvider(Path);

            if (Config.SetMinPricesTo1)
            {
                _sessionManager.AddOverrideMod(2825413709);
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
            Session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += Login;

            if (Session.Managers.GetManager<PluginManager>().Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"), out var all))
            {
                var alli = all.GetType().Assembly.GetType("AlliancesPlugin.AlliancePlugin");
                try
                {
                    AllianceTaxes = all.GetType().GetMethod("AddToTaxes", BindingFlags.Public | BindingFlags.Static, null, new Type[4] { typeof(ulong), typeof(long), typeof(string), typeof(Vector3D) }, null);
                    //    BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                }
                catch (Exception ex)
                {
                    Log.Error("Error getting alliance taxes");

                }
                Alliance = all;
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

        public override void Init(ITorchBase Torch)
        {

            base.Init(Torch);
            _sessionManager = base.Torch.Managers.GetManager<TorchSessionManager>();

            if (_sessionManager != null)
            {
                _sessionManager.SessionStateChanged += SessionChanged;
            }
            BasePath = StoragePath;
            SetupConfig();
            Path = CreatePath();
            if (!CrunchEconCore.Config.PluginEnabled)
            {
                return;
            }
            if (!Directory.Exists(Path + "//Logs//"))
            {
                Directory.CreateDirectory(Path + "//Logs//");
            }
            TorchBase = base.Torch;
        }

        public static MethodInfo AllianceTaxes;

        public static ITorchPlugin Alliance;

        public void SetupConfig()
        {

            Path = StoragePath;

            if (File.Exists(StoragePath + "\\CrunchEconomy.xml"))
            {
                Config = Utils.ReadFromXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml");
                Utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", Config, false);
            }
            else
            {
                Config = new Config();
                Utils.WriteToXmlFile<Config>(StoragePath + "\\CrunchEconomy.xml", Config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            folder = Config.StoragePath.Equals("default") ? System.IO.Path.Combine(StoragePath + "//CrunchEcon//") : Config.StoragePath;
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = System.IO.Path.Combine(StoragePath + "//CrunchEcon//");
            Directory.CreateDirectory(folder2);
            if (Config.StoragePath.Equals("default"))
            {
                folder2 = System.IO.Path.Combine(StoragePath + "//CrunchEcon//");
            }
            else
            {
                folder2 = Config.StoragePath + "//CrunchEcon//";
            }

            Directory.CreateDirectory(folder2);


            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            var utils = new FileUtils();

            Config = utils.ReadFromXmlFile<Config>(BasePath + "\\CrunchEconomy.xml");
            return Config;
        }
    }
}
