using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchEconomy
{
    public class Config
    {
        public string StoragePath = "default";
        public Boolean MiningContractsEnabled = false;
        public Boolean HaulingContractsEnabled = false;
        public Boolean SurveyContractsEnabled = false;
        public int SecondsBetweenMiningContracts = 600;
        public int SecondsBetweenSurveyMissions = 7200;
    }
}
