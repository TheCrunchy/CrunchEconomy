using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace CrunchEconomy.Helpers
{
    public class FacUtils
    {
        public static IMyFaction GetPlayersFaction(long PlayerId)
        {
            return MySession.Static.Factions.TryGetPlayerFaction(PlayerId);
        }

        public static bool InSameFaction(long Player1, long Player2)
        {
            var faction1 = GetPlayersFaction(Player1);
            var faction2 = GetPlayersFaction(Player2);
            return faction1 == faction2;
        }

        public static string GetFactionTag(long PlayerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(PlayerId);

            return faction == null ? "" : faction.Tag;
        }
        public static long GetOwner(MyCubeGrid Grid)
        {

            var gridOwnerList = Grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                return gridOwnerList[0];
            else if (ownerCnt > 1)
                return gridOwnerList[1];

            return gridOwner;
        }

        public static bool IsOwnerOrFactionOwned(MyCubeGrid Grid, long PlayerId, bool DoFactionCheck)
        {
            if (Grid.BigOwners.Contains(PlayerId))
            {
                return true;
            }
            else
            {
                if (!DoFactionCheck)
                {
                    return false;
                }
                var ownerId = GetOwner(Grid);
                //check if the owner is a faction member, i honestly dont know the difference between grid.BigOwners and grid.SmallOwners
                return FacUtils.InSameFaction(PlayerId, ownerId);
            }
        }

    }
}
