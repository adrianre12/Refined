using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Refined.Code
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Refinery), false, "LargeRefinery stop")]

    internal class RefineryBlock : MyGameLogicComponent
    {
        IMyRefinery block;
        VRage.Game.ModAPI.Ingame.IMyInventory inventory;

        private MyItemType platinumId = MyItemType.MakeIngot("Platinum");
        private MyFixedPoint lastAmount;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            Log("Init");
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            block = Entity as IMyRefinery;

            inventory = block.GetInventory(1);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            Log("OnceBeforeFrame");
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            var amount = inventory.GetItemAmount(platinumId);
            Log($"Platimum Ingot={amount} delta={lastAmount - amount}");
            lastAmount = amount;
        }

        public override void UpdatingStopped()
        {
            base.UpdatingStopped();
            Log("UpdatingStopped");
        }
        private void Log(string msg)
        {
            MyLog.Default.WriteLine(msg);
        }


    }
}
