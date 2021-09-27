using CrunchEconomy.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace CrunchEconomy
{
    [Category("crunchecon")]
    public class EconCommands : CommandModule
    {
        [Command("storeids", "grab ids from store offers")]
        [Permission(MyPromoteLevel.Admin)]
        public void GrabIds()
        {

            BoundingSphereD sphere = new BoundingSphereD(Context.Player.GetPosition(), 400);

            foreach (MyStoreBlock store in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyStoreBlock>())
            {
         
                foreach (MyStoreItem item  in store.PlayerItems)
                {
                    Context.Respond(item.Item.Value.ToString());
                }

            }


        }
        [Command("moneys", "view all money added through contracts")]
        [Permission(MyPromoteLevel.Admin)]
        public async void HowMuchMoneys(String type)
        {
            Context.Respond("Doing this async, may take a while");
            if (Enum.TryParse(type, out ContractType contract))
            {
                long output = await Task.Run(() => CountMoney(contract));
                Context.Respond(String.Format("{0:n0}", output) + " SC Added to economy through " + type + " contracts.");
            }

        }
        public long CountMoney(ContractType type)
        {
            Dictionary<string, long> MoneyFromTypes = new Dictionary<string, long>();
            Dictionary<string, int> AmountCompleted = new Dictionary<string, int>();
            long output = 0;
            switch (type)
            {

                case ContractType.Mining:

                    foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//PlayerData//Mining//Completed//"))
                    {

                        Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(s);
                        if (MoneyFromTypes.ContainsKey(contract.SubType))
                        {
                            MoneyFromTypes[contract.SubType] += contract.AmountPaid;
                            AmountCompleted[contract.SubType] += 1;
                        }
                        else
                        {
                            MoneyFromTypes.Add(contract.SubType, contract.AmountPaid);
                            AmountCompleted.Add(contract.SubType, 1);
                        }

                        output += contract.AmountPaid;
                    }
                    break;
                case ContractType.Hauling:
                    foreach (String s in Directory.GetFiles(CrunchEconCore.path + "//PlayerData//Hauling//Completed//"))
                    {


                        Contract contract = CrunchEconCore.utils.ReadFromXmlFile<Contract>(s);
                        if (MoneyFromTypes.ContainsKey(contract.SubType))
                        {
                            MoneyFromTypes[contract.SubType] += contract.AmountPaid;
                            AmountCompleted[contract.SubType] += 1;
                        }
                        else
                        {
                            MoneyFromTypes.Add(contract.SubType, contract.AmountPaid);
                            AmountCompleted.Add(contract.SubType, 1);
                        }
                        output += contract.AmountPaid;
                    }
                    break;

            }

            foreach (KeyValuePair<string, long> pair in MoneyFromTypes)
            {
                Context.Respond(pair.Key + " total money " + String.Format("{0:n0}", pair.Value) + " | amount completed : " + AmountCompleted[pair.Key]);

            }
            return output;
        }
        [Command("reload", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            Context.Respond("reloading");
            CrunchEconCore.LoadConfig();
        }
        [Command("pause", "stop the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Pause()
        {
            Context.Respond("Pausing the economy refreshing.");
            CrunchEconCore.paused = true;
        }

        [Command("start", "start the economy refreshing")]
        [Permission(MyPromoteLevel.Admin)]
        public void Start()
        {
            Context.Respond("Starting the economy refreshing.");
            CrunchEconCore.LoadAllStations();
            foreach (Stations station in CrunchEconCore.stations)
            {
                station.nextBuyRefresh = DateTime.Now;
                station.nextSellRefresh = DateTime.Now;
            }
            CrunchEconCore.paused = false;
        }


    }
}
