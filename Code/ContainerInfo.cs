using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;


namespace Catopia.Refined
{
    internal class ContainerInfo
    {
        private string keyWord = "Rfnd";

        private List<IMyInventory> inventories = new List<IMyInventory>();

        private RefiningInfo refiningInfoI = RefiningInfo.Instance;
        private RefineryInfo refineries;

        internal ContainerInfo(RefineryInfo refineryInfo)
        {
            refineries = refineryInfo;
        }

        internal void FindContainerInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
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


        private bool RefineAll()
        {
            foreach (var inventory in inventories)
            {
                if (!RefineContainer(inventory))
                    return false;
            }

            return true;

        }

        internal bool RefineContainer(IMyInventory inventory)
        {

            var oreOrder = refiningInfoI.NewOreOrderList();

            foreach (var oreItemId in oreOrder)
            {
                if (refineries.AvailableSeconds == 0)
                    return false;

                OreToIngotInfo info = null;
                if (!refiningInfoI.OreToIngots.TryGetValue(oreItemId, out info))
                {
                    Log.Msg($"Failed to get info for {oreItemId}");
                    return false;
                }

                RefineInventoryOre(inventory, info);
            }
            return true;
        }

        internal void RefineInventoryOre(IMyInventory inventory, OreToIngotInfo info)
        {
            int oreAmount = (int)inventory.GetItemAmount(info.ItemId);
            if (oreAmount == 0)
                return;

            double prereqAmount = (double)info.Amount;
            int bpRuns = (int)Math.Truncate(oreAmount / prereqAmount);
            if (bpRuns == 0)
                return;

            int freeVolume = (int)(inventory.MaxVolume - inventory.CurrentVolume);
            float deltaVolume = info.IngotsVolume * refineries.AvgYieldMultiplier - (info.Volume * (float)info.Amount);

            if (deltaVolume > 0)
            {
                bpRuns = (int)Math.Min(freeVolume / deltaVolume, bpRuns);
                if (bpRuns < 1)
                    return;
            }

            //power check.
            bpRuns = ConsumeRefinaryTime(info.ProductionTime, bpRuns);

            inventory.RemoveItemsOfType(bpRuns * info.Amount, info.ItemId);

            foreach (var ingot in info.Ingots)
            {
                MyFixedPoint ingotAmount = ingot.Amount * (refineries.AvgYieldMultiplier * bpRuns);
                inventory.AddItems(ingotAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(ingot.ItemId));
            }

        }

        private int ConsumeRefinaryTime(float productionTime, int bpRuns)
        {
            var neededTime = productionTime * bpRuns;
            if (neededTime <= refineries.AvailableSeconds)
            {
                refineries.AvailableSeconds -= neededTime;
                return bpRuns;
            }

            bpRuns = (int)(refineries.AvailableSeconds / productionTime);
            refineries.AvailableSeconds = 0;
            return bpRuns;
        }


    }
}
