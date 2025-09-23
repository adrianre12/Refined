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
        private ScreenRefined screen0;

        public ReactorInfo(ScreenRefined screen0)
        {
            this.screen0 = screen0;
        }

        internal bool FindReactorInfo(IMyCubeGrid cubeGrid)
        {
            inventories.Clear();
            MaxPower = 0;
            AvaialbleUranium = 0;
            int amountU = 0;
            if (Log.Debug) Log.Msg("FindReactorInfo");
            foreach (var block in cubeGrid.GetFatBlocks<IMyReactor>())
            {
                if (!block.Enabled || !block.IsFunctional)
                    continue;
                MyInventory inv = (MyInventory)block.GetInventory();
                inventories.Add(inv);
                amountU = (int)inv.GetItemAmount(UDefId) - ReserveUranium;
                if (amountU <= 0)
                    continue;
                AvaialbleUranium += amountU;
                MaxPower += block.MaxOutput;
                //Log.Msg($"Reactor {block.CustomName} maxOutput={block.MaxOutput} amountU={amountU}");
            }
            bool OK = AvaialbleUranium > 0;
            if (!OK)
                screen0.AddText("Not enough Reactor Uranium");
            return OK;
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
            if (Log.Debug) Log.Msg($"AvailableUranium={AvaialbleUranium}");
        }

        internal void ConsumeUranium(float mWseconds)
        {

            MyFixedPoint remove = (MyFixedPoint)(mWseconds / MWsPerU); //ConsumedUranium
            if (Log.Debug) Log.Msg($"ConsumeUranium MWseconds={mWseconds} remove={remove}");
            foreach (var inventory in inventories)
            {
                remove -= RemoveUraniumFromInventory(inventory, remove);
                if (remove == 0)
                    break;
            }
        }

        private MyFixedPoint RemoveUraniumFromInventory(MyInventory inventory, MyFixedPoint amount)
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
