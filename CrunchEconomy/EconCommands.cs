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

  
    }
}
