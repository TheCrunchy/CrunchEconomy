using System;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace CrunchEconomy.ModifierFluctuations
{
    [Category("stationmodifier")]
    public class FluctuationCommands : CommandModule
    {
        [Command("random", "randomise the modifiers of this name between these values")]
        [Permission(MyPromoteLevel.Admin)]
        public void ApplyModifiers(string ModifierName, string Type, double Min, double Max, int ApplyTo = 500, Boolean LocalInstance = true)
        {
          
        }
        [Command("set", "set the modifiers of this name between these values")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetModifiers(string ModifierName, double NewAmount, Boolean LocalInstance = true)
        {

        }
    }
}

    
