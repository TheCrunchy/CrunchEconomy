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
        public Guid Id;
        public ContractStatus Status = ContractStatus.InProgress;
        public Boolean Enabled = false;
        public ulong SteamId;
        public double CurrentPosX = 0;
        public double CurrentPosY = 0;
        public double CurrentPosZ = 0;
        public int CurrentStage = 1;
        private Dictionary<int, SurveyStage> _missions = new Dictionary<int, SurveyStage>();
        public double Chance = 1;
        public int ReputationRequired = 0;
        public List<SurveyStage> Configs = new List<SurveyStage>();

        public SurveyStage GetStage(int Stagenum)
        {
            if (_missions.ContainsKey(Stagenum))
            {
                return _missions[Stagenum];
            }
            else
            {
                return null;
            }
        }

        public void SetupMissionList()
        {
            foreach (var stage in Configs)
            {
                if (!_missions.ContainsKey(stage.StageNum))
                {
                    _missions.Add(stage.StageNum, stage);
                }
            }
        }
        
    }
}
