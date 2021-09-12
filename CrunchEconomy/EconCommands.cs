using CrunchEconomy.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
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
