using CrunchEconomy.Contracts;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace CrunchEconomy
{
    [Category("contract")]
    public class ContractCommands : CommandModule
    {
        [Command("info", "view current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails()
        {
            if (CrunchEconCore.playerData.TryGetValue(Context.Player.SteamUserId, out PlayerData data))
            {
                List<IMyGps> playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Context.Player.IdentityId, playerList);
                foreach (IMyGps gps in playerList)
                {
                    if (gps.Description.Contains("Contract Delivery Location."))
                    {
                        MyAPIGateway.Session?.GPS.RemoveGps(Context.Player.Identity.IdentityId, gps);
                    }
                }

                StringBuilder contractDetails = new StringBuilder();
                contractDetails.AppendLine("Current Mining Reputation " + data.MiningReputation);
                contractDetails.AppendLine("");
                contractDetails.AppendLine("Current Hauling Reputation " + data.HaulingReputation);
                contractDetails.AppendLine("");
                foreach (MiningContract c in data.getMiningContracts().Values)
                {

                    if (c.minedAmount >= c.amountToMine)
                    {
                        c.DoPlayerGps(Context.Player.Identity.IdentityId);
                        contractDetails.AppendLine("Deliver " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                        
                    }
                    else
                    {
                        contractDetails.AppendLine("Mine " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC. and " + c.reputation + " reputation gain.");
                    }
                    contractDetails.AppendLine("");
                }

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
