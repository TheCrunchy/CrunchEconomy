using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.GameServices;
using VRageMath;

namespace CrunchEconomy.CombatEncounters
{
    public class Encounter
    {
        public Guid EncounterId = Guid.NewGuid();
        public bool Enabled = true;
        public string Name = "Example Encounter";
        public DateTime NextStageTime;
        public int StageNum = 0;
        public bool Triggered = false;
        public EncounterTrigger Trigger = new EncounterTrigger();
        private Dictionary<int, EncounterStage> OrganisedStages = new Dictionary<int, EncounterStage>();

        public void SortStages()
        {
            OrganisedStages.Clear();
            var i = 1;
            foreach (var stage in Stages)
            {
                OrganisedStages.Add(i, stage);
                i++;
            }
        }
        public List<EncounterStage> Stages = new List<EncounterStage>();

        public async Task<bool> ProgressToNextStage(Vector3D Location)
        {
            if (DateTime.Now < NextStageTime)
                return true;
            if (!OrganisedStages.TryGetValue(StageNum + 1, out var stage))
                return false;

            StageNum += 1;
            await stage.SpawnGrids(Location);
            return true;
        }
    }
}
