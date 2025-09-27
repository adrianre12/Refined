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

        private MyDefinitionId UDefId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");

        internal float MaxPower;
        internal int MWseconds { get { return (int)(AvaialbleUranium * MWsPerU); } }
        internal int AvaialbleUranium;

        private CommonSettings settings = CommonSettings.Instance;
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
                amountU = (int)inv.GetItemAmount(UDefId) - settings.ReserveUranium;
                if (amountU <= 0)
                    continue;
                AvaialbleUranium += amountU;
                MaxPower += block.MaxOutput;
            }
            screen0.RunInfo.NumReactors = inventories.Count;
            screen0.RunInfo.ReactorPower = MaxPower;
            screen0.RunInfo.AvailableUranium = AvaialbleUranium;
            screen0.Dirty = true;

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
                amountU = (int)inv.GetItemAmount(UDefId) - settings.ReserveUranium;
                if (amountU <= 0)
                    continue;
                AvaialbleUranium += amountU;
            }
            if (Log.Debug) Log.Msg($"AvailableUranium={AvaialbleUranium}");
            screen0.RunInfo.AvailableUranium = AvaialbleUranium;
            screen0.Dirty = true;
        }

        internal void ConsumeUranium(float mWseconds)
        {
            float consumedUranium = (mWseconds * 1 / MWsPerU);
            screen0.RunInfo.UraniumUsed += consumedUranium;
            if (Log.Debug) Log.Msg($"ConsumeUranium MWseconds={mWseconds} consumedUranium ={consumedUranium}");

            MyFixedPoint removeU = (MyFixedPoint)consumedUranium;
            foreach (var inventory in inventories)
            {
                removeU -= RemoveUraniumFromInventory(inventory, removeU);
                if (removeU == 0)
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
