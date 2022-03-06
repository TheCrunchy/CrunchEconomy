using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace CrunchEconomy
{
    [Category("stationmodifier")]
    public class FluctuationCommands : CommandModule
    {
        [Command("random", "randomise the modifiers of this name between these values")]
        [Permission(MyPromoteLevel.Admin)]
        public void applyModifiers(string modifierName, string type, double min, double max, int applyTo = 500, Boolean localInstance = true)
        {
          
        }
        [Command("set", "set the modifiers of this name between these values")]
        [Permission(MyPromoteLevel.Admin)]
        public void setModifiers(string modifierName, double newAmount, Boolean localInstance = true)
        {

        }
    }
}

    
