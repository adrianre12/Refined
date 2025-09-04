using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI;
using Ingame = VRage.Game.ModAPI.Ingame;

namespace Catopia.Refined
{
    internal class Inventories
    {
        private IMyCubeGrid cubeGrid;
        private List<IMyInventory> inventories = new List<IMyInventory>();
        private List<IMyInventory> InputInventories = new List<IMyInventory>();

        private IMyInventory refinedInventory;
        public Inventories(IMyCargoContainer refinedBlock)
        {
            this.cubeGrid = refinedBlock.CubeGrid;
            this.refinedInventory = refinedBlock.GetInventory();
        }

        internal void Clear()
        {
            InputInventories.Clear();
            inventories.Clear();
        }

        internal void FindContainerInventories(string keyWord)
        {
            Log.Msg("FindContainerInventories");
            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(keyWord) || !container.IsFunctional)
                    continue;
                var inventory = container.GetInventory();
                if (!refinedInventory.IsConnectedTo(inventory))
                    continue;

                inventories.Add(container.GetInventory());
                Log.Msg($"Added `{container.CustomName}`");

            }
        }

        /// <summary>
        /// Checks if the inventory is reachable and adds it
        /// </summary>
        /// <param name="inventory"></param>
        /// <returns>IsReachable</returns>
        internal bool AddRefineryInventories(IMyRefinery refinary)
        {
            if (refinary == null || !refinedInventory.IsConnectedTo(refinary.InputInventory))
            {
                Log.Msg($"Not added {refinary.CustomName}");
                return false;
            }
            InputInventories.Add(refinary.InputInventory);
            inventories.Add(refinary.OutputInventory);
            return true;
        }

        internal long ItemAmount(Ingame.MyItemType itemType)
        {
            MyFixedPoint amount = 0;
            foreach (var inventory in inventories)
            {
                amount += inventory.GetItemAmount(itemType);
            }
            foreach (var inventory in InputInventories)
            {
                amount += inventory.GetItemAmount(itemType);
            }
            return (long)amount;
        }
    }
}
