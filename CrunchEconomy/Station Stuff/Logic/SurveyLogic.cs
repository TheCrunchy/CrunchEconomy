using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
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
        public static Dictionary<ulong, DateTime> playerSurveyTimes = new Dictionary<ulong, DateTime>();
        public static Dictionary<ulong, DateTime> MessageCooldowns = new Dictionary<ulong, DateTime>();
        public static void GenerateNewSurveyMission(PlayerData data, MyPlayer player)
        {
            try
            {
                List<IMyGps> playerList;
                if (data.surveyMission != Guid.Empty)
                {
                    //   Log.Info("Has survey");
                    var ShouldReturn = false;
                    var mission = data.GetLoadedMission();
                    if (mission != null)
                    {
                        //   Log.Info("not null");
                        var distance = Vector3.Distance(new Vector3(mission.CurrentPosX, mission.CurrentPosY, mission.CurrentPosZ), player.GetPosition());
                        if (distance <= mission.getStage(mission.CurrentStage).RadiusNearLocationToBeInside)
                        {
                            // Log.Info("within distance");
                            if (playerSurveyTimes.TryGetValue(player.Id.SteamId, out var time))
                            {
                                var seconds = DateTime.Now.Subtract(time);

                                mission.getStage(mission.CurrentStage).Progress += Convert.ToInt32(seconds.TotalSeconds);
                                //  Log.Info("progress " + mission.getStage(mission.CurrentStage).Progress);
                                if (mission.getStage(mission.CurrentStage).Progress >= mission.getStage(mission.CurrentStage).SecondsToStayInArea)
                                {
                                    // Log.Info("Completed");
                                    mission.getStage(mission.CurrentStage).Completed = true;
                                    //do rewards
                                    var money = mission.getStage(mission.CurrentStage).CreditReward;
                                    if (CrunchEconCore.AlliancePluginEnabled)
                                    {
                                        //patch into alliances and process the payment there
                                        //contract.AmountPaid = contract.contractPrice;
                                        try
                                        {
                                            var MethodInput = new object[] { player.Id.SteamId, money, "Survey", player.Character.PositionComp.GetPosition() };
                                            money = (long)CrunchEconCore.AllianceTaxes?.Invoke(null, MethodInput);

                                        }
                                        catch (Exception ex)
                                        {
                                            CrunchEconCore.Log.Error(ex);
                                        }
                                    }
                                    if (mission.getStage(mission.CurrentStage).DoRareItemReward && data.SurveyReputation >= mission.getStage(mission.CurrentStage).MinimumRepRequiredForItem)
                                    {
                                        if (MyDefinitionId.TryParse("MyObjectBuilder_" + mission.getStage(mission.CurrentStage).RewardItemType, mission.getStage(mission.CurrentStage).RewardItemSubType, out var reward))
                                        {

                                            var rand = new Random();
                                            var chance = rand.NextDouble();
                                            if (chance <= mission.getStage(mission.CurrentStage).ItemRewardChance)
                                            {

                                                var itemType = new MyInventoryItemFilter(reward.TypeId + "/" + reward.SubtypeName).ItemType;
                                                if (player.Character.GetInventory() != null && player.Character.GetInventory().CanItemsBeAdded((MyFixedPoint)mission.getStage(mission.CurrentStage).ItemRewardAmount, itemType))
                                                {
                                                    player.Character.GetInventory().AddItems((MyFixedPoint)mission.getStage(mission.CurrentStage).ItemRewardAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(reward));
                                                    CrunchEconCore.SendMessage("Survey", "Bonus item reward in character inventory.", Color.Gold, (long)player.Id.SteamId);
                                                }
                                            }

                                        }
                                    }
                                    EconUtils.addMoney(player.Identity.IdentityId, money);

                                    CrunchEconCore.SendMessage("Survey", mission.getStage(mission.CurrentStage).CompletionMessage, Color.Gold, (long)player.Id.SteamId);
                                    data.SurveyReputation += mission.getStage(mission.CurrentStage).ReputationGain;
                                    if (mission.getStage(mission.CurrentStage + 1) != null)
                                    {

                                        mission.CurrentStage += 1;
                                        if (data.SurveyReputation >= mission.getStage(mission.CurrentStage).MinimumReputation && data.SurveyReputation <= mission.getStage(mission.CurrentStage).MaximumReputation)
                                        {


                                            data.NextSurveyMission = data.NextSurveyMission.AddSeconds(60);


                                            if (mission.getStage(mission.CurrentStage).FindRandomPositionAroundLocation)

                                            {
                                                var gps = ContractUtils.ScanChat(mission.getStage(1).LocationGPS);

                                                if (mission.getStage(mission.CurrentStage).FindRandomPositionAroundLocation)
                                                {
                                                    var negative = System.Math.Abs(mission.getStage(mission.CurrentStage).RadiusToPickRandom) * (-1);
                                                    var positive = mission.getStage(mission.CurrentStage).RadiusToPickRandom;

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
                                                sb.AppendLine(mission.getStage(mission.CurrentStage).GPSDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.getStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.getStage(mission.CurrentStage).GPSName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                                            }
                                            else
                                            {
                                                var gps = ContractUtils.ScanChat(mission.getStage(mission.CurrentStage).LocationGPS);
                                                mission.CurrentPosX = gps.Coords.X;
                                                mission.CurrentPosY = gps.Coords.Y;
                                                mission.CurrentPosZ = gps.Coords.Z;
                                                var sb = new StringBuilder();
                                                sb.AppendLine(mission.getStage(mission.CurrentStage).GPSDescription);
                                                sb.AppendLine("");
                                                sb.AppendLine("Reward: " + String.Format("{0:n0}", mission.getStage(mission.CurrentStage).CreditReward) + " SC.");
                                                sb.AppendLine("");
                                                sb.AppendLine("SURVEY LOCATION.");
                                                gps.Description = sb.ToString();
                                                gps.GPSColor = Color.Gold;
                                                gps.Name = mission.getStage(mission.CurrentStage).GPSName;
                                                gps.ShowOnHud = true;
                                                gps.DiscardAt = new TimeSpan(6000);

                                                var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                                                gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                                            }
                                        }
                                        else
                                        {
                                            mission.status = ContractStatus.Completed;
                                            data.SetLoadedSurvey(null);
                                            data.surveyMission = Guid.Empty;
                                        }

                                        CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                                        CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);

                                    }
                                    else
                                    {
                                        mission.status = ContractStatus.Completed;
                                        data.SetLoadedSurvey(null);
                                        data.surveyMission = Guid.Empty;
                                        CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission, true);
                                        CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                                        CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
                                    }
                                    playerSurveyTimes.Remove(player.Id.SteamId);
                                    playerList = new List<IMyGps>();
                                    MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                                    foreach (var gps in playerList.Where(gps => gps.Description != null && gps.Description.Contains("SURVEY LOCATION.")))
                                    {
                                        MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
                                    }


                                    return;
                                }

                                if (MessageCooldowns.TryGetValue(player.Id.SteamId, out var time2))
                                {
                                    if (DateTime.Now >= time2)
                                    {
                                        var message2 = new NotificationMessage();

                                        message2 = new NotificationMessage("Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                        //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                        ModCommunication.SendMessageTo(message2, player.Id.SteamId);


                                        // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                        MessageCooldowns[player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                    }
                                }
                                else
                                {
                                    var message2 = new NotificationMessage();

                                    message2 = new NotificationMessage("Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, 2000, "Green");
                                    //this is annoying, need to figure out how to check the exact world time so a duplicate message isnt possible

                                    ModCommunication.SendMessageTo(message2, player.Id.SteamId);


                                    // SendMessage("Survey", "Progress " + mission.getStage(mission.CurrentStage).Progress + "/" + mission.getStage(mission.CurrentStage).SecondsToStayInArea, Color.Gold, (long)player.Id.SteamId);
                                    MessageCooldowns[player.Id.SteamId] = DateTime.Now.AddSeconds(1);
                                }
                                data.NextSurveyMission = data.NextSurveyMission.AddSeconds(60);
                                data.SetLoadedSurvey(mission);
                                CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                                playerSurveyTimes[player.Id.SteamId] = DateTime.Now;
                                ShouldReturn = true;

                            }
                            else
                            {
                                playerSurveyTimes.Add(player.Id.SteamId, DateTime.Now);
                                ShouldReturn = true;
                            }
                            data.SetLoadedSurvey(mission);
                            data.NextSurveyMission = DateTime.Now.AddSeconds(60);
                            CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(mission);
                            CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                            // utils.WriteToJsonFile<PlayerData>(path + "//PlayerData//Data//" + data.steamId + ".json", data);
                        }
                    }
                    if (ShouldReturn)
                    {
                        return;
                    }
                }

                if (DateTime.Now < data.NextSurveyMission) return;

                var newSurvey = ContractUtils.GetNewMission(data);
                if (newSurvey == null) return;

                playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(player.Identity.IdentityId, playerList);
                foreach (var gps in playerList.Where(gps => gps.Description != null && gps.Description.Contains("SURVEY LOCATION.")))
                {
                    MyAPIGateway.Session?.GPS.RemoveGps(player.Identity.IdentityId, gps);
                }

                data.surveyMission = newSurvey.id;
                if (newSurvey.getStage(1).FindRandomPositionAroundLocation)

                {
                    var gps = ContractUtils.ScanChat(newSurvey.getStage(1).LocationGPS);

                    if (newSurvey.getStage(1).FindRandomPositionAroundLocation)
                    {
                        var negative = System.Math.Abs(newSurvey.getStage(1).RadiusToPickRandom) * (-1);
                        var positive = newSurvey.getStage(1).RadiusToPickRandom;

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
                    sb.AppendLine(newSurvey.getStage(1).GPSDescription);
                    sb.AppendLine("");
                    sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.getStage(1).CreditReward) + " SC.");
                    sb.AppendLine("");
                    sb.AppendLine("SURVEY LOCATION.");
                    gps.Description = sb.ToString();
                    gps.GPSColor = Color.Gold;
                    gps.Name = newSurvey.getStage(1).GPSName;
                    gps.ShowOnHud = true;
                    gps.DiscardAt = new TimeSpan(6000);

                    var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                    gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                }
                else
                {
                    var gps = ContractUtils.ScanChat(newSurvey.getStage(1).LocationGPS);
                    newSurvey.CurrentPosX = gps.Coords.X;
                    newSurvey.CurrentPosY = gps.Coords.Y;
                    newSurvey.CurrentPosZ = gps.Coords.Z;
                    var sb = new StringBuilder();
                    sb.AppendLine(newSurvey.getStage(1).GPSDescription);
                    sb.AppendLine("");
                    sb.AppendLine("Reward: " + String.Format("{0:n0}", newSurvey.getStage(1).CreditReward) + " SC.");
                    sb.AppendLine("");
                    sb.AppendLine("SURVEY LOCATION.");
                    gps.Description = sb.ToString();
                    gps.GPSColor = Color.Gold;
                    gps.Name = newSurvey.getStage(1).GPSName;
                    gps.ShowOnHud = true;
                    gps.DiscardAt = new TimeSpan(6000);

                    var gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;
                    gpscol.SendAddGpsRequest(player.Identity.IdentityId, ref gps);
                }
                data.SetLoadedSurvey(newSurvey);
                data.NextSurveyMission = DateTime.Now.AddSeconds(CrunchEconCore.config.SecondsBetweenSurveyMissions);
                CrunchEconCore.PlayerStorageProvider.AddSurveyToBeSaved(newSurvey);

                CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
            }
            catch (Exception ex)
            {
                CrunchEconCore.Log.Error(ex);
                data.SetLoadedSurvey(null);
                data.surveyMission = Guid.Empty;
                CrunchEconCore.PlayerStorageProvider.playerData[player.Id.SteamId] = data;
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data); 
                throw;
            }
        }
    }
}
