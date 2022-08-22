using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchEconomy.Contracts;
using CrunchEconomy.Station_Stuff.Objects;
using CrunchEconomy.SurveyMissions;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using static CrunchEconomy.Contracts.GeneratedContract;

namespace CrunchEconomy.Commands
{
    [Category("contract")]
    public class ContractCommands : CommandModule
    {
        public static Dictionary<ulong, Dictionary<int, Guid>> Ids = new Dictionary<ulong, Dictionary<int, Guid>>();
        public static Dictionary<ulong, int> PlayerMax = new Dictionary<ulong, int>();

        [Command("fake", "make a ton of fake mining contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public async Task GenerateFile(int Amount, bool Para = false)
        {
            var watch = new Stopwatch();
            if (Para) {
            watch.Start();
           await Task.Run(() => { 
            Parallel.For(0, Amount, I =>
            {
              GenerateAndSaveContract(Context.Player.SteamUserId, "Example1");
            });
            });
            }
            else
            {
                watch.Start();
                for (var i = 0; i < Amount; i++)
                {
                    GenerateAndSaveContract(Context.Player.SteamUserId, "Example1");
                }
            }

            Context.Respond("Generated in " + watch.ElapsedMilliseconds);
        }

        public void GenerateAndSaveContract(ulong Steamid, string ContractName)
        {
            if (!ContractUtils.NewContracts.TryGetValue(ContractName, out var contract)) return;
            var temp = ContractUtils.GeneratedToPlayer(contract);
            var random = new Random();
            var locations = new List<StationDelivery>();
            var temporaryStations = new Dictionary<string, Stations>();
            foreach (var del in contract.StationsToDeliverTo)
            {
                if (!(random.Next(0, 100) <= del.Chance)) continue;
                foreach (var stat in CrunchEconCore.Stations.Where(Stat => Stat.Name.Equals(del.Name)))
                {
                    if (!temporaryStations.ContainsKey(del.Name))
                    {
                        temporaryStations.Add(del.Name, stat);
                    }
                    locations.Add(del);
                }
            }

            temp.AmountPaid = temp.ContractPrice;
            temp.PlayerSteamId = Steamid;
            CrunchEconCore.Utils.WriteToXmlFile<Contract>(CrunchEconCore.Path + "//PlayerData//" + contract.Type + "//Completed//" + temp.ContractId + ".xml", temp);
        }

        [Command("admintest", "quit current contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public void GenerateFile()
        {
            var mission = new SurveyMission();
            mission.Configs.Add(new SurveyStage());
            mission.Configs.Add(new SurveyStage());
            mission.Configs.Add(new SurveyStage());
            CrunchEconCore.Utils.WriteToXmlFile<SurveyMission>(CrunchEconCore.Path + "//survey.xml", mission);
            var mission2 = CrunchEconCore.Utils.ReadFromXmlFile<SurveyMission>(CrunchEconCore.Path + "//survey.xml");
            foreach (var stage in mission2.Configs)
            {
                Context.Respond(stage.Id.ToString());
            }
        }

        [Command("quit", "quit current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails(int Contractnum)
        {
            if (!CrunchEconCore.PlayerStorageProvider.PlayerData.TryGetValue(Context.Player.SteamUserId,
                    out var data)) return;
            Ids.TryGetValue(Context.Player.SteamUserId, out var derp);
            if (derp == null)
            {
                Context.Respond("I dont know the ids! Use !contract info");
                return;
            }

            if (derp.ContainsKey(Contractnum))
            {
                var sb = new StringBuilder();
                Contract cancel = null;
                if (data.GetMiningContracts().ContainsKey(derp[Contractnum]))
                {
                    cancel = data.GetMiningContracts()[derp[Contractnum]];
                    data.MiningReputation -= cancel.Reputation * 2;

                    data.GetMiningContracts().Remove(derp[Contractnum]);
                    data.MiningContracts.Remove(derp[Contractnum]);

                    cancel.Status = ContractStatus.Failed;
                    sb.AppendLine("Cancelled contract");
                    sb.AppendLine("Mine " + cancel.SubType + " Ore " + $"{cancel.MinedAmount:n0}" + " / " +
                                  $"{cancel.AmountToMineOrDeliver:n0}");

                    sb.AppendLine("Reputation lowered by " + cancel.Reputation * 2);
                    sb.AppendLine();
                    sb.AppendLine("Remaining Contracts");
                    sb.AppendLine();
                }
                if (data.GetHaulingContracts().ContainsKey(derp[Contractnum]))
                {
                    cancel = data.GetHaulingContracts()[derp[Contractnum]];
                    data.HaulingReputation -= cancel.Reputation * 2;

                    data.GetHaulingContracts().Remove(derp[Contractnum]);
                    data.HaulingContracts.Remove(derp[Contractnum]);
                    cancel.Status = ContractStatus.Failed;

                    sb.AppendLine("Cancelled contract");
                    sb.AppendLine("Deliver " + cancel.SubType + " Ore " + $"{cancel.AmountToMineOrDeliver:n0}");
                    sb.AppendLine("Reward : " + $"{cancel.ContractPrice:n0}" + " SC. and " + cancel.Reputation + " reputation gain.");
                    sb.AppendLine("Distance bonus :" + $"{cancel.DistanceBonus:n0}" + " SC.");

                    sb.AppendLine("Reputation lowered by " + cancel.Reputation * 2);
                    sb.AppendLine();
                    sb.AppendLine("Remaining Contracts");
                    sb.AppendLine();
                }
                if (cancel == null)
                {
                    Context.Respond("Couldnt find the contract.");
                    return;
                }
                foreach (var c in data.GetMiningContracts().Values)
                {

                    if (c.MinedAmount >= c.AmountToMineOrDeliver)
                    {
                        sb.AppendLine("Deliver " + c.SubType + " Ore " + $"{c.AmountToMineOrDeliver:n0}");
                        sb.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");
                    }
                    else
                    {
                        sb.AppendLine("Mine " + c.SubType + " Ore " + $"{c.MinedAmount:n0}" + " / " +
                                      $"{c.AmountToMineOrDeliver:n0}");
                        sb.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");

                    }
                    sb.AppendLine("");
                }
                foreach (var c in data.GetHaulingContracts().Values)
                {

                    sb.AppendLine("Deliver " + c.SubType + " Ore " + $"{c.AmountToMineOrDeliver:n0}");
                    sb.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");
                    sb.AppendLine("Distance bonus :" + $"{c.DistanceBonus:n0}" + " SC.");


                    sb.AppendLine("");
                }

                derp.Remove(Contractnum);
                Ids[Context.Player.SteamUserId] = derp;
                var m = new DialogMessage("Contract", "Cancel", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                    
                CrunchEconCore.PlayerStorageProvider.AddContractToBeSaved(cancel, true);
                CrunchEconCore.PlayerStorageProvider.SavePlayerData(data);
            }
            else
            {
                Context.Respond("Cannot find that contract.");
            }
        }

      

        [Command("info", "view current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails()
        {
            if (CrunchEconCore.PlayerStorageProvider.PlayerData.TryGetValue(Context.Player.SteamUserId, out var data))
            {
                var num = 0;
                Ids.TryGetValue(Context.Player.SteamUserId, out var derp);
                if (derp == null)
                {
                    //  Context.Respond("ids didnt contain");
                    derp = new Dictionary<int, Guid>();
                    num = 0;
                }
                else
                {
                    num = PlayerMax[Context.Player.SteamUserId];
                }
                var temp = new Dictionary<Guid, int>();

                foreach (var pair in derp.Where(Pair => !temp.ContainsKey(Pair.Value)))
                {
                    temp.Add(pair.Value, pair.Key);
                }
                var playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Context.Player.IdentityId, playerList);
                foreach (var gps in playerList.Where(Gps => Gps.Description != null && Gps.Description.Contains("Contract Delivery Location.")))
                {
                    MyAPIGateway.Session?.GPS.RemoveGps(Context.Player.Identity.IdentityId, gps);
                }

                var contractDetails = new StringBuilder();
                contractDetails.AppendLine("Current Mining Reputation " + data.MiningReputation);
                var bonus = 0f;
                var nextBonus = 100;
                if (data.MiningReputation >= 100)
                {
                   bonus += 0.025f;
                    nextBonus = 250;
                }
                if (data.MiningReputation >= 250)
                {
                    bonus += 0.025f;
                    nextBonus = 500;
                }
                if (data.MiningReputation >= 500)
                {
                    bonus += 0.025f;
                    nextBonus = 750;
                }
                if (data.MiningReputation >= 750)
                {
                    bonus += 0.025f;
                    nextBonus = 1000;
                }
                if (data.MiningReputation >= 1000)
                {
                    bonus += 0.025f;
                    nextBonus = 2000;
                }
                if (data.MiningReputation >= 2000)
                {
                    bonus += 0.025f;
                    nextBonus = 3000;
                }
                if (data.MiningReputation >= 3000)
                {
                    bonus += 0.05f;
                }
                contractDetails.AppendLine("Mining Bonus Pay " + bonus * 100 + "%. Next Bonus at " + nextBonus + " Reputation.");
                contractDetails.AppendLine("");
                contractDetails.AppendLine("Current Hauling Reputation " + data.HaulingReputation);
                contractDetails.AppendLine("");
                foreach (var c in data.GetMiningContracts().Values)
                {
                    if (c.MinedAmount >= c.AmountToMineOrDeliver)
                    {
                        c.DoPlayerGps(Context.Player.Identity.IdentityId);
                        contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + $"{c.AmountToMineOrDeliver:n0}");
                        contractDetails.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");
                    }
                    else
                    {
                        contractDetails.AppendLine("Mine " + c.SubType + " Ore " + $"{c.MinedAmount:n0}" + " / " +
                                                   $"{c.AmountToMineOrDeliver:n0}");
                        contractDetails.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");
                    }
                    if (derp.ContainsValue(c.ContractId))
                    {
                        contractDetails.AppendLine("To quit use !contract quit " + temp[c.ContractId]);
                    }
                    else
                    {
                        num++;
                        derp.Add(num, c.ContractId);
                        contractDetails.AppendLine("To quit use !contract quit " + num);

                    }
                    contractDetails.AppendLine("");
                }

                foreach (var c in data.GetHaulingContracts().Values)
                {

                    contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + $"{c.AmountToMineOrDeliver:n0}");
                    contractDetails.AppendLine("Reward : " + $"{c.ContractPrice:n0}" + " SC. and " + c.Reputation + " reputation gain.");
                    contractDetails.AppendLine("Distance bonus :" + $"{c.DistanceBonus:n0}" + " SC.");
                    c.DoPlayerGps(Context.Player.IdentityId);
                    if (derp.ContainsValue(c.ContractId))
                    {
                        contractDetails.AppendLine("To quit use !contract quit " + temp[c.ContractId]);
                        //Context.Respond("1");
                    }
                    else
                    {
                        //  Context.Respond("2");
                        num++;
                        derp.Add(num, c.ContractId);
                        contractDetails.AppendLine("To quit use !contract quit " + num);

                    }

                    contractDetails.AppendLine("");
                }
                Ids.Remove(Context.Player.SteamUserId);
                Ids.Add(Context.Player.SteamUserId, derp);
                PlayerMax.Remove(Context.Player.SteamUserId);
                PlayerMax.Add(Context.Player.SteamUserId, num);
                var m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                ModCommunication.SendMessageTo(m2, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You don't currently have any contracts.");
            }
        }
    }
}
