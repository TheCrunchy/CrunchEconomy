using CrunchEconomy.Contracts;
using CrunchEconomy.SurveyMissions;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
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
using static CrunchEconomy.Contracts.GeneratedContract;

namespace CrunchEconomy
{
    [Category("contract")]
    public class ContractCommands : CommandModule
    {
        public static Dictionary<ulong, Dictionary<int, Guid>> ids = new Dictionary<ulong, Dictionary<int, Guid>>();
        public static Dictionary<ulong, int> playerMax = new Dictionary<ulong, int>();

        [Command("fake", "make a ton of fake mining contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public async Task GenerateFile(int amount, bool para = false)
        {
            Stopwatch watch = new Stopwatch();
            if (para) {
            watch.Start();
           await Task.Run(() => { 
            Parallel.For(0, amount, i =>
            {
              GenerateAndSaveContract(Context.Player.SteamUserId, "Example1");
            });
            });
            }
            else
            {
                watch.Start();
                for (int i = 0; i < amount; i++)
                {
                    GenerateAndSaveContract(Context.Player.SteamUserId, "Example1");
                }
            }

            Context.Respond("Generated in " + watch.ElapsedMilliseconds);
        }

        public void GenerateAndSaveContract(ulong steamid, string contractName)
        {
            if (ContractUtils.newContracts.TryGetValue(contractName, out GeneratedContract contract))
            {


                Contract temp = ContractUtils.GeneratedToPlayer(contract);
                Random random = new Random();
                List<StationDelivery> locations = new List<StationDelivery>();
                Dictionary<string, Stations> temporaryStations = new Dictionary<string, Stations>();
                bool picked = false;
                foreach (StationDelivery del in contract.StationsToDeliverTo)
                {


                        if (random.Next(0, 100) <= del.chance)
                        {
                            foreach (Stations stat in CrunchEconCore.stations)
                            {
                                if (stat.Name.Equals(del.Name))
                                {
                                    if (!temporaryStations.ContainsKey(del.Name))
                                    {
                                        temporaryStations.Add(del.Name, stat);
                                    }
                                    locations.Add(del);
                                }
                            }

                        }
                }

                temp.AmountPaid = temp.contractPrice;
                temp.PlayerSteamId = steamid;
                CrunchEconCore.utils.WriteToXmlFile<Contract>(CrunchEconCore.path + "//PlayerData//" + contract.type + "//Completed//" + temp.ContractId + ".xml", temp);
            }
        }

        [Command("admintest", "quit current contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public void GenerateFile()
        {
            SurveyMission mission = new SurveyMission();
            mission.configs.Add(new SurveyStage());
            mission.configs.Add(new SurveyStage());
            mission.configs.Add(new SurveyStage());
            CrunchEconCore.utils.WriteToXmlFile<SurveyMission>(CrunchEconCore.path + "//survey.xml", mission);
            SurveyMission mission2 = CrunchEconCore.utils.ReadFromXmlFile<SurveyMission>(CrunchEconCore.path + "//survey.xml");
            foreach (SurveyStage stage in mission2.configs)
            {
                Context.Respond(stage.id.ToString());
            }
        }

        [Command("quit", "quit current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails(int contractnum)
        {
            if (CrunchEconCore.playerData.TryGetValue(Context.Player.SteamUserId, out PlayerData data))
            {
                ids.TryGetValue(Context.Player.SteamUserId, out Dictionary<int, Guid> derp);
                if (derp == null)
                {
                    Context.Respond("I dont know the ids! Use !contract info");
                    return;
                }

                if (derp.ContainsKey(contractnum))
                {
                    StringBuilder sb = new StringBuilder();
                    Contract cancel = null;
                    if (data.getMiningContracts().ContainsKey(derp[contractnum]))
                    {
                        cancel = data.getMiningContracts()[derp[contractnum]];
                        data.MiningReputation -= cancel.reputation * 2;

                        data.getMiningContracts().Remove(derp[contractnum]);
                        data.MiningContracts.Remove(derp[contractnum]);

                        cancel.status = ContractStatus.Failed;
                        sb.AppendLine("Cancelled contract");
                        sb.AppendLine("Mine " + cancel.SubType + " Ore " + String.Format("{0:n0}", cancel.minedAmount) + " / " + String.Format("{0:n0}", cancel.amountToMineOrDeliver));

                        sb.AppendLine("Reputation lowered by " + cancel.reputation * 2);
                        sb.AppendLine();
                        sb.AppendLine("Remaining Contracts");
                        sb.AppendLine();
                    }
                    if (data.getHaulingContracts().ContainsKey(derp[contractnum]))
                    {
                        cancel = data.getHaulingContracts()[derp[contractnum]];
                        data.HaulingReputation -= cancel.reputation * 2;

                        data.getHaulingContracts().Remove(derp[contractnum]);
                        data.HaulingContracts.Remove(derp[contractnum]);
                        cancel.status = ContractStatus.Failed;

                        sb.AppendLine("Cancelled contract");
                        sb.AppendLine("Deliver " + cancel.SubType + " Ore " + String.Format("{0:n0}", cancel.amountToMineOrDeliver));
                        sb.AppendLine("Reward : " + String.Format("{0:n0}", cancel.contractPrice) + " SC. and " + cancel.reputation + " reputation gain.");
                        sb.AppendLine("Distance bonus :" + String.Format("{0:n0}", cancel.DistanceBonus) + " SC.");

                        sb.AppendLine("Reputation lowered by " + cancel.reputation * 2);
                        sb.AppendLine();
                        sb.AppendLine("Remaining Contracts");
                        sb.AppendLine();
                    }
                    if (cancel == null)
                    {
                        Context.Respond("Couldnt find the contract.");
                        return;
                    }
                    foreach (Contract c in data.getMiningContracts().Values)
                    {

                        if (c.minedAmount >= c.amountToMineOrDeliver)
                        {
                            sb.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                            sb.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                        }
                        else
                        {
                            sb.AppendLine("Mine " + c.SubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                            sb.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");

                        }
                        sb.AppendLine("");
                    }
                    foreach (Contract c in data.getHaulingContracts().Values)
                    {

                        sb.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                        sb.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                        sb.AppendLine("Distance bonus :" + String.Format("{0:n0}", c.DistanceBonus) + " SC.");


                        sb.AppendLine("");
                    }

                    derp.Remove(contractnum);
                    ids[Context.Player.SteamUserId] = derp;
                    DialogMessage m = new DialogMessage("Contract", "Cancel", sb.ToString());
                    ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
                  
                    File.Delete(CrunchEconCore.path + "//PlayerData//Mining//InProgress//" + cancel.ContractId + ".xml");
                    CrunchEconCore.ContractSave.Remove(cancel.ContractId);
                    CrunchEconCore.ContractSave.Add(cancel.ContractId, cancel);
                    CrunchEconCore.utils.WriteToJsonFile<PlayerData>(CrunchEconCore.path + "//PlayerData//Data//" + data.steamId + ".json", data);
                }
                else
                {
                    Context.Respond("Cannot find that contract.");
                }
            }
        }

      

        [Command("info", "view current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails()
        {
            if (CrunchEconCore.playerData.TryGetValue(Context.Player.SteamUserId, out PlayerData data))
            {
                int num = 0;
                ids.TryGetValue(Context.Player.SteamUserId, out Dictionary<int, Guid> derp);
                if (derp == null)
                {
                    //  Context.Respond("ids didnt contain");
                    derp = new Dictionary<int, Guid>();
                    num = 0;
                }
                else
                {
                    num = playerMax[Context.Player.SteamUserId];
                }
                Dictionary<Guid, int> temp = new Dictionary<Guid, int>();

                foreach (KeyValuePair<int, Guid> pair in derp)
                {
                    if (!temp.ContainsKey(pair.Value))
                    {
                        temp.Add(pair.Value, pair.Key);
                    }
                }
                List<IMyGps> playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Context.Player.IdentityId, playerList);
                foreach (IMyGps gps in playerList)
                {
                    if (gps.Description != null && gps.Description.Contains("Contract Delivery Location."))
                    {
                        MyAPIGateway.Session?.GPS.RemoveGps(Context.Player.Identity.IdentityId, gps);
                    }
                }

                StringBuilder contractDetails = new StringBuilder();
                contractDetails.AppendLine("Current Mining Reputation " + data.MiningReputation);
                float bonus = 0f;
                int NextBonus = 100;
                if (data.MiningReputation >= 100)
                {
                   bonus += 0.025f;
                    NextBonus = 250;
                }
                if (data.MiningReputation >= 250)
                {
                    bonus += 0.025f;
                    NextBonus = 500;
                }
                if (data.MiningReputation >= 500)
                {
                    bonus += 0.025f;
                    NextBonus = 750;
                }
                if (data.MiningReputation >= 750)
                {
                    bonus += 0.025f;
                    NextBonus = 1000;
                }
                if (data.MiningReputation >= 1000)
                {
                    bonus += 0.025f;
                    NextBonus = 2000;
                }
                if (data.MiningReputation >= 2000)
                {
                    bonus += 0.025f;
                    NextBonus = 3000;
                }
                if (data.MiningReputation >= 3000)
                {
                    bonus += 0.05f;
                }
                contractDetails.AppendLine("Mining Bonus Pay " + bonus * 100 + "%. Next Bonus at " + NextBonus + " Reputation.");
                contractDetails.AppendLine("");
                contractDetails.AppendLine("Current Hauling Reputation " + data.HaulingReputation);
                contractDetails.AppendLine("");
                foreach (Contract c in data.getMiningContracts().Values)
                {
                    
                    if (c.minedAmount >= c.amountToMineOrDeliver)
                    {
                        c.DoPlayerGps(Context.Player.Identity.IdentityId);
                        contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");

                    }
                    else
                    {
                        contractDetails.AppendLine("Mine " + c.SubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                    }
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

                foreach (Contract c in data.getHaulingContracts().Values)
                {

                    contractDetails.AppendLine("Deliver " + c.SubType + " Ore " + String.Format("{0:n0}", c.amountToMineOrDeliver));
                    contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                    contractDetails.AppendLine("Distance bonus :" + String.Format("{0:n0}", c.DistanceBonus) + " SC.");
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
                ids.Remove(Context.Player.SteamUserId);
                ids.Add(Context.Player.SteamUserId, derp);
                playerMax.Remove(Context.Player.SteamUserId);
                playerMax.Add(Context.Player.SteamUserId, num);
                DialogMessage m2 = new DialogMessage("Contract Details", "Instructions", contractDetails.ToString());
                ModCommunication.SendMessageTo(m2, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You dont currently have any contracts.");
            }
        }
    }
}
