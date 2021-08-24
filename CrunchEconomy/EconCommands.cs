using CrunchEconomy.Contracts;
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
    [Category("crunchecon")]
    public class EconCommands : CommandModule
    {
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
            CrunchEconCore.NextFileRefresh = DateTime.Now;
            CrunchEconCore.paused = false;
        }

        [Command("contracts", "view current contracts")]
        [Permission(MyPromoteLevel.None)]
        public void DoContractDetails()
        {
           if (CrunchEconCore.playerData.TryGetValue(Context.Player.SteamUserId, out PlayerData data))
            {
                StringBuilder contractDetails = new StringBuilder();
                foreach (MiningContract c in data.getMiningContracts().Values)
                {

                    if (c.minedAmount >= c.amountToMine)
                    {
                        c.DoPlayerGps(Context.Player.Identity.IdentityId);
                        contractDetails.AppendLine("Deliver " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC.");
                    }
                    else
                    {
                        contractDetails.AppendLine("Mine " + c.OreSubType + " Ore " + String.Format("{0:n0}", c.minedAmount) + " / " + String.Format("{0:n0}", c.amountToMine));
                        contractDetails.AppendLine("Reward : " + String.Format("{0:n0}", c.contractPrice) + " SC.");
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
