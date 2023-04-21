using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace CrunchEconomy.CombatEncounters
{
    public class EncounterStage
    {
        public List<EncounterGrid> GridToSpawn = new List<EncounterGrid>();
        public int TimeToNextStageSeconds = 120;
        public class EncounterGrid
        {
            public string GridFileName = "ExampleGrid";
            public int AmountToSpawn = 1;
            public double ChanceToSpawn = 1;
        }

        public async Task<int> SpawnGrids(Vector3D Location)
        {

            return TimeToNextStageSeconds;
        }
    }
}
