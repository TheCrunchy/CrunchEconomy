using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace EconStuff
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        private bool isInit = false;

        private void DoWork()
        {
            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {

                if ((def as MyComponentDefinition) != null)
                {
                    (def as MyComponentDefinition).MinimalPricePerUnit = 1;
                }
                if ((def as MyPhysicalItemDefinition) != null)
                {
                    (def as MyPhysicalItemDefinition).MinimalPricePerUnit = 1;
                }
            }
        }


        public override bool UpdatedBeforeInit()
        {
            DoWork();
            return true;
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            DoWork();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!isInit && MyAPIGateway.Session == null)
            {
                DoWork();
                isInit = true;
            }
        }
    }
}
