using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy.SurveyMissions
{
    public class SurveyStage
    {
        public Boolean enabled = false;
        public Guid id;
        public int StageNum = 1;
        public int MinimumReputation = 0;
        public int MaximumReputation = 500;
        public long CreditReward = 5000000;
        public int ReputationGain = 1;
        public Boolean Completed = false;
        public int SecondsToAddToRefreshTimerOnComplete = 1200;
        public int SecondsToStayInArea = 60;
        public int Progress = 0;
        public Boolean ContributeToGoal = false;
        public String CompletionMessage = "SURVEY COMPLETED. THANK YOU FOR CHOOSING A.C.M.E. A.C.M.E, TOGETHER FOR A BETTER TOMRROW!";
        public string GoalName = "Example";
        public string LocationGPS = "put gps here";
        public string GPSDescription = "Survey Location.";
        public string GPSName = "Easy Survey Location";
        public int RadiusNearLocationToBeInside = 25000;
        public Boolean FindRandomPositionAroundLocation;
        public int RadiusToPickRandom = 25000;

        public Boolean DoRareItemReward = false;
        public double ItemRewardChance = 1;
        public string RewardItemType = "Ingot";
        public string RewardItemSubType = "Uranium";
        public double ItemRewardAmount = 5;
        public int MinimumRepRequiredForItem = 50;
    }
}
