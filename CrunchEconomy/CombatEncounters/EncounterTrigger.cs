using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace CrunchEconomy.CombatEncounters
{
    public class EncounterTrigger
    {
        public Triggers TriggerType = Triggers.Grid_Damage;
        public bool TriggerAtLocation = true;
        public Vector3D Location;
        public string NPCTag = "Example";
        public int Radius = 10000;
        public int CooldownInMinutes = 10;
    }
}
