using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Helpers;
using NLog.Fluent;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace CrunchEconomy.Station_Stuff.Logic
{
    public static class SurveyLogic
    {
        public static Dictionary<ulong, DateTime> PlayerSurveyTimes = new Dictionary<ulong, DateTime>();
        public static Dictionary<ulong, DateTime> MessageCooldowns = new Dictionary<ulong, DateTime>();
        public static void GenerateNewSurveyMission(PlayerData Data, MyPlayer Player)
        {
            try
            {
                List<IMyGps> playerList;
                if (Data.SurveyMission != Guid.Empty)
                {
                    //   Log.Info("Has survey");
                    var shouldReturn = false;
                    var mission = Data.GetLoadedMission();
                    if (mission != null)
                    {
                        //   Log.Info("not null");
                        var distance = Vector3.Distance(new Vector3(mission.CurrentPosX, mission.CurrentPosY, mission.CurrentPosZ), Player.GetPosition());
                        if (distance <= mission.GetStage(mission.CurrentStage).RadiusNearLocationToBeInside)
                        {
                            // Log.Info("within distance");
                            if (PlayerSurveyTimes.TryGetValue(Player.Id.SteamId, out var time))
                            {
                                var seconds = DateTime.Now.Subtract(time);

                                mission.GetStage(mission.CurrentStage).Progress += Convert.ToInt32(seconds.TotalSeconds);
                                //  Log.Info("progress " + mission.getStage(mission.CurrentStage).Progress);
                                if (mission.GetStage(mission.CurrentStage).Progress >= mission.GetStage(mission.CurrentStage).SecondsToStayInArea)
                                {
                                    // Log.Info("Completed");
                                    mission.GetStage(mission.CurrentStage).Completed = true;
                                    //do rewards
                                    var money = mission.GetStage(mission.CurrentStage).CreditReward;
                                    if (CrunchEconCore.AlliancePluginEnabled)
                                    {
                                        //patch into alliances and process the payment there
                                        //contract.AmountPaid = contract.contractPrice;
                                        try
                                        {
                                            var methodInput = new object[] { Player.Id.SteamId, money, "Survey", Player.Character.PositionComp.GetPosition() };
                                            money = (long)CrunchEconCore.AllianceTaxes?.Invoke(null, methodInput);

                                        }
                                        catch (Exception ex)
                                        {
                                            CrunchEconCore.Log.Error(ex);
                                        }
                                    }
                                    if (mission.GetStage(mission.CurrentStage).DoRareItemReward && Data.SurveyReputation >= mission.GetStage(mission.CurrentStage).MinimumRepRequiredForItem)
                                    {
                                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + mission.GetStage(mission.CurrentStage).RewardItemType, mission.GetStage(mission.CurrentStage).RewardItemSubType, out var reward))
                                        {

                                            var rand = new Random();
                                            var chance = rand.NextDouble();
                                            if (chance <= mission.GetStage(mission.CurrentStage).ItemRewardChance)
                                            {

                                                var itemType = new MyInventoryItemFilter(reward.TypeId + "/" + reward.SubtypeName).ItemType;
                                                if (Player.Character.GetInventory() != null && Player.Character.GetInventory().CanItemsBeAdded((MyFixedPoint)mission.GetStage(mission.CurrentStage).ItemRewardAmount, itemType))
                                                {
                                                    Player.Character.GetInventory().AddItems((MyFixedPoint)mission.GetStage(mission.CurrentStage).ItemRewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(reward));
                                                    CrunchEconCore.SendMessage("Survey", "Bonus item reward in character inventory.", Color.Gold, (long)Player.Id.SteamId);
                                                }
                                            }

                                        }
                                    }
                                    EconUtils.AddMoney(Player.Identity.IdentityId, money);

                                    CrunchEconCore.SendMessage("Survey", mission.GetStage(mission.CurrentStage).CompletionMessage, Color.Gold, (long)Player.Id.SteamId);
                                    Data.SurveyReputation += mission.GetStage(mission.CurrentStage).ReputationGain;
                                    if (mission.GetStage(mission.CurrentStage + 1) != null)
                                    {

                                        mission.CurrentStage += 1;
                                        if (Data.SurveyReputation >= mission.GetStage(mission.CurrentStage).MinimumReputation && Data.SurveyReputation <= mission.GetStage(mission.CurrentStage).MaximumReputation)
                                        {


                                            Data.NextSurveyMission = Data.NextSurveyMission.AddSeconds(60);


                                            if (mission.GetStage(mission.CurrentStage).FindRandomPositionAroundLocation)

                                            {
                                                var gps = GpsHelper.ParseGps(mission.GetStage(1).LocationGps);

                                                if (mission.GetStage(mission.CurrentStage).FindRandomPositionAroundLocation)
                                                {
                                                    var negative = System.Math.Abs(mission.GetStage(mission.CurrentStage).RadiusToPickRandom) * (-1);
                                                    var positive = mission.GetStage(mission.CurrentStage).RadiusToPickRandom;

                                                    var rand = new Random();
                                                    var x = rand.Next(negative, positive);
                                                    var y = rand.Next(negative, positive);
                                                    var z = rand.Next(negative, positive);
                                                    var offset = new Vector3(x, y, z);
                                                    gps.Coords += offset;
                                                }

                                                mission.CurrentPosX = gps.Coords.X;
                                                mission.CurrentPosY = gps.Coords.Y;
                                                mission.CurrentPosZ = gps.Coords.Z;
                                                var sb = new StringBuilder();
                                                sb.AppendLine(mission.GetStage(mission.CurrentStage).GpsDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.GetStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.GetStage(mission.CurrentStage).GpsName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGpsRequest(Player.Identity.IdentityId, ref gps);
                                            }
                                            else
                                            {
                                                var gps = GpsHelper.ParseGps(mission.GetStage(mission.CurrentStage).LocationGps);
                                                mission.CurrentPosX = gps.Coords.X;
                                                mission.CurrentPosY = gps.Coords.Y;
                                                mission.CurrentPosZ = gps.Coords.Z;
                                                var sb = new StringBuilder();
                                                sb.AppendLine(mission.GetStage(mission.CurrentStage).GpsDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.GetStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.GetStage(mission.CurrentStage).GpsName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGpsRequest(Player.Identity.IdentityId, ref gps);
                                            }
                                        }
                                        else
                                        {
                                            mission.Status = ContractStatus.Completed;
                                            Data.SetLoadedSurvey(null);
                                            Data.SurveyMission = Guid.Empty;
                                        }

                                        CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                                        CrunchEconCore.PlayerStorageProvider.SavePlayerData(Data);

                                    }
                                    else
                                    {
                                        mission.Status = ContractStatus.Completed;
                                        Data.SetLoadedSurvey(null);
                                        Data.SurveyMission = Guid.Empty;
                                        CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                                        CrunchEconCore.PlayerStorageProvider.SavePlayerData(Data);
                                    }
                                    PlayerSurveyTimes.Remove(Player.Id.SteamId);
                                    playerList = new List<IMyGps>();
                                    MySession.Static.Gpss.GetGpsList(Player.Identity.IdentityId, playerList);
                                    foreach (var gps in playerList.Where(Gps => Gps.Description != null && Gps.Description.Contains("SURVEY LOCATION.")))
                                    {
                                        MyAPIGateway.Session?.GPS.RemoveGps(Player.Identity.IdentityId, gps);
                                    }


                                    return;
                                }

                                if (MessageCooldowns.TryGetValue(Player.Id.SteamId, out var time2))
                                {
                                    if (DateTime.Now >= time2)
                                    {
                                        var message2 = new NotificationMessage();

                                        message2 = new NotificationMessage("Progress " + mission.GetStage(mission.CurrentStage).Progress + "/" + mission.GetStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                        ModCommunication.SendMessageTo(message2, Player.Id.SteamId);


                                        // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                        MessageCooldowns[Player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                    }
                                }
                                else
                                {
                                    var message2 = new NotificationMessage();

                                    message2 = new NotificationMessage("Progress " + mission.GetStage(mission.CurrentStage).Progress + "/" + mission.GetStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                    ModCommunication.SendMessageTo(message2, Player.Id.SteamId);


                                    // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                    MessageCooldowns[Player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                }
                                Data.NextSurveyMission = Data.NextSurveyMission.AddSeconds(60);
                                Data.SetLoadedSurvey(mission);
                                CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                                PlayerSurveyTimes[Player.Id.SteamId] = DateTime.Now;
                                shouldReturn = true;

                            }
                            else
                            {
                                PlayerSurveyTimes.Add(Player.Id.SteamId, DateTime.Now);
                                shouldReturn = true;
                            }
                            Data.SetLoadedSurvey(mission);
                            Data.NextSurveyMission = DateTime.Now.AddSeconds(60);
                            CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission);
                            CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                            // utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                        }
                    }
                    if (shouldReturn)
                    {
                        return;
                    }
                }

                if (DateTime.Now < Data.NextSurveyMission) return;

                var newSurvey = ContractUtils.GetNewMission(Data);
                if (newSurvey == null) return;

                playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Player.Identity.IdentityId, playerList);
                foreach (var gps in playerList.Where(Gps => Gps.Description != null && Gps.Description.Contains("SURVEY LOCATION.")))
                {
                    MyAPIGateway.Session?.GPS.RemoveGps(Player.Identity.IdentityId, gps);
                }

                Data.SurveyMission = newSurvey.Id;
                if (newSurvey.GetStage(1).FindRandomPositionAroundLocation)

                {
                    var gps = GpsHelper.ParseGps(newSurvey.GetStage(1).LocationGps);

                    if (newSurvey.GetStage(1).FindRandomPositionAroundLocation)
                    {
                        var negative = System.Math.Abs(newSurvey.GetStage(1).RadiusToPickRandom) * (-1);
                        var positive = newSurvey.GetStage(1).RadiusToPickRandom;

                        var rand = new Random();
                        var x = rand.Next(negative, positive);
                        var y = rand.Next(negative, positive);
                        var z = rand.Next(negative, positive);
                        var offset = new Vector3(x, y, z);
                        gps.Coords += offset;
                    }

                    newSurvey.CurrentPosX = gps.Coords.X;
                    newSurvey.CurrentPosY = gps.Coords.Y;
                    newSurvey.CurrentPosZ = gps.Coords.Z;
                    var sb = new StringBuilder();
                    sb.AppendLine(newSurvey.GetStage(1).GpsDescription);
                    sb.AppendLine("");
                    sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.GetStage(1).CreditReward) + " SC.");
                    sb.AppendLine("");
                    sb.AppendLine("SURVEY LOCATION.");
                    gps.Description = sb.ToString();
                    gps.GPSColor = Color.Gold;
                    gps.Name = newSurvey.GetStage(1).GpsName;
                    gps.ShowOnHud = true;
                    gps.DiscardAt = new TimeSpan(6000);

                    var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                    gpscol.SendAddGpsRequest(Player.Identity.IdentityId, ref gps);
                }
                else
                {
                    var gps = GpsHelper.ParseGps(newSurvey.GetStage(1).LocationGps);
                    newSurvey.CurrentPosX = gps.Coords.X;
                    newSurvey.CurrentPosY = gps.Coords.Y;
                    newSurvey.CurrentPosZ = gps.Coords.Z;
                    var sb = new StringBuilder();
                    sb.AppendLine(newSurvey.GetStage(1).GpsDescription);
                    sb.AppendLine("");
                    sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.GetStage(1).CreditReward) + " SC.");
                    sb.AppendLine("");
                    sb.AppendLine("SURVEY LOCATION.");
                    gps.Description = sb.ToString();
                    gps.GPSColor = Color.Gold;
                    gps.Name = newSurvey.GetStage(1).GpsName;
                    gps.ShowOnHud = true;
                    gps.DiscardAt = new TimeSpan(6000);

                    var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                    gpscol.SendAddGpsRequest(Player.Identity.IdentityId, ref gps);
                }
                Data.SetLoadedSurvey(newSurvey);
                Data.NextSurveyMission = DateTime.Now.AddSeconds(CrunchEconCore.Config.SecondsBetweenSurveyMissions);
                CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(newSurvey);

                CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(Data);
            }
            catch (Exception ex)
            {
                CrunchEconCore.Log.Error(ex);
                Data.SetLoadedSurvey(null);
                Data.SurveyMission = Guid.Empty;
                CrunchEconCore.PlayerStorageProvider.PlayerData[Player.Id.SteamId] = Data;
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(Data); 
                throw;
            }
        }
    }
}
