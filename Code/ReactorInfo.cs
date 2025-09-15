using Sandbox.Game;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Catopia.Refined
{
    internal class ReactorInfo
    {
        public const float MWsPerU = 1.0f * 3600;

        //private Ingame.MyItemType uraniumId = Ingame.MyItemType.MakeIngot("Uranium");
        private MyDefinitionId UDefId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");

        internal float MaxPower;
        internal float MWseconds { get { return AvaialbleUranium * MWsPerU; } }
        internal int AvaialbleUranium;
        internal int ReserveUranium = 50;


        private List<MyInventory> inventories = new List<MyInventory>();

        internal void FindReactorInfo(IMyCubeGrid cubeGrid)
        {
            inventories.Clear();
            MaxPower = 0;
            AvaialbleUranium = 0;
            int amountU = 0;
            Log.Msg("FindReactorInfo");
            foreach (var block in cubeGrid.GetFatBlocks<IMyReactor>())
            {
                if (!block.Enabled || !block.IsFunctional)
                    return;
                MyInventory inv = (MyInventory)block.GetInventory();
                inventories.Add(inv);
                amountU = (int)inv.GetItemAmount(UDefId) - ReserveUranium;
                if (amountU <= 0)
                    continue;
                AvaialbleUranium += amountU;
                MaxPower += block.MaxOutput;
                //Log.Msg($"Reactor {block.CustomName} maxOutput={block.MaxOutput} amountU={amountU}");
            }
        }

        internal void Refresh()
        {
            AvaialbleUranium = 0;
            int amountU = 0;
            foreach (var inv in inventories)
            {
                amountU = (int)inv.GetItemAmount(UDefId) - ReserveUranium;
                if (amountU <= 0)
                    continue;
                AvaialbleUranium += amountU;
            }
        }

        internal void ConsumeUranium(float mWseconds)
        {

            MyFixedPoint remove = (MyFixedPoint)(mWseconds / MWsPerU); //ConsumedUranium
            foreach (var inventory in inventories)
            {
                remove -= RemoveUraniumFromInventory(inventory, remove);
                if (remove == 0)
                    break;
            }
        }

        internal MyFixedPoint RemoveUraniumFromInventory(MyInventory inventory, MyFixedPoint amount)
        {
            MyFixedPoint foundAmount = inventory.GetItemAmount(UDefId);
            if (foundAmount == 0)
                return 0;

            MyFixedPoint removedAmount = MyFixedPoint.Min(foundAmount, amount);
            inventory.RemoveItemsOfType(removedAmount, UDefId);

            return removedAmount;
        }
    }
}
