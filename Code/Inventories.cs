using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Catopia.Refined
{
    internal class Inventories
    {
        private IMyCubeGrid cubeGrid;
        private List<IMyInventory> inventories = new List<IMyInventory>();
        //private List<IMyInventory> InputInventories = new List<IMyInventory>();

        private IMyInventory refinedInventory;
        public Inventories(IMyCargoContainer refinedBlock)
        {
            this.cubeGrid = refinedBlock.CubeGrid;
            this.refinedInventory = refinedBlock.GetInventory();
        }

        internal void Clear()
        {
            //InputInventories.Clear();
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

        /*        /// <summary>
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

                internal long ItemAmount(MyDefinitionId itemType)
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

                internal int RemoveItemAmount(MyDefinitionId itemType, int amount)
                {
                    MyFixedPoint remove = (MyFixedPoint)amount;
                    foreach (var inventory in InputInventories)
                    {
                        remove -= RemoveItemAmountFromInventory(inventory, itemType, remove);
                        if (remove == 0)
                            break;
                    }
                    var removed = amount - remove;
                    return removed.ToIntSafe();
                }

                internal int AddItemAmount(MyDefinitionId itemType, int amount)
                {
                    RefineOre refineOre = RefineOre.Instance;
                    MyPhysicalItemDefinition physicalItem;
                    if (!RefineOre.Instance.TryGetPhysicalItem(itemType, out physicalItem))
                    {
                        Log.Msg($"Failed to find volume for {itemType.ToString()}");
                        return 0;
                    }

                    MyFixedPoint add = (MyFixedPoint)amount;
                    foreach (var inventory in InputInventories)
                    {
                        add -= AddItemAmountToInventory((MyInventory)inventory, itemType, add, physicalItem);
                        if (add == 0)
                            break;
                    }
                    var removed = amount - add;
                    return removed.ToIntSafe();
                }
*/

        /*        internal MyFixedPoint RemoveItemAmountFromInventory(IMyInventory inventory, MyDefinitionId itemType, MyFixedPoint amount)
                {
                    MyFixedPoint foundAmount = inventory.GetItemAmount(itemType);
                    if (foundAmount == 0)
                        return 0;

                    MyFixedPoint removedAmount = MyFixedPoint.Min(foundAmount, amount);
                    inventory.RemoveItemsOfType(removedAmount, itemType);

                    return removedAmount;
                }

                internal MyFixedPoint AddItemAmountToInventory(MyInventory inventory, MyDefinitionId itemType, MyFixedPoint amount, MyPhysicalItemDefinition physicalItem)
                {
                    double freeVolume = (double)(inventory.MaxVolume - inventory.CurrentVolume);
                    MyFixedPoint maxAdd = MyFixedPoint.Min(amount, inventory.ComputeAmountThatFits(itemType));

                    if (!inventory.CanItemsBeAdded(maxAdd, itemType))
                    {
                        Log.Msg($"Could not add {maxAdd.ToString()} of {itemType.ToString()}");
                        return MyFixedPoint.Zero;
                    }
                    inventory.AddItems(maxAdd, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(physicalItem.Id));
                    return maxAdd;
                }*/

    }
}
