using CrunchEconomy.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.SurveyMissions
{
    public class SurveyMission
    {
        public Guid id;
        public ContractStatus status = ContractStatus.InProgress;
        public Boolean enabled = false;
        public ulong steamId;
        public double CurrentPosX = 0;
        public double CurrentPosY = 0;
        public double CurrentPosZ = 0;
        public int CurrentStage = 1;
        private Dictionary<int, SurveyStage> missions = new Dictionary<int, SurveyStage>();
        public double chance = 1;
        public int ReputationRequired = 0;
        public List<SurveyStage> configs = new List<SurveyStage>();

        public SurveyStage getStage(int stagenum)
        {
            if (missions.ContainsKey(stagenum))
            {
                return missions[stagenum];
            }
            else
            {
                return null;
            }
        }

        public void SetupMissionList()
        {
            foreach (SurveyStage stage in configs)
            {
                if (!missions.ContainsKey(stage.StageNum))
                {
                    missions.Add(stage.StageNum, stage);
                }
            }
        }
        
    }
}
