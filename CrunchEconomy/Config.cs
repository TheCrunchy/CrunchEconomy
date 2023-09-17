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
        public Boolean PluginEnabled = true;
        public Boolean PatchesEnabled = true;
        public Boolean SetMinPricesTo1 = false;
        public Boolean DoCombine = false;
        public Boolean RefreshPlayerStoresOnLoad = false;
        public int MinutesBetweenDave = 15;
        public string ApiKey = "HELLOIMAKEY";
        public bool DoWebUI = false;
        public string UIURL = "https://localhost:7116/";
        public double SecondsBetweenEventChecks = 1;
    }
}
