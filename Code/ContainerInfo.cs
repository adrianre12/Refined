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
        internal string KeyWord = "Rfnd";
        internal int MaxRefineries = 10;

        private List<IMyInventory> inventories = new List<IMyInventory>();

        private RefiningInfo refiningInfoI = RefiningInfo.Instance;
        private RefineryInfo refineryInfo;
        private int index;

        internal enum Result
        {
            Error,
            Success,
            NoTime,
            NotEnoughVolume,
            NotEnoughOre
        }
        internal ContainerInfo(int offlineS)
        {
            refineryInfo = new RefineryInfo(offlineS);
        }

        internal bool FindContainerInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
        {
            if (Log.Debug) Log.Msg("FindContainerInventories");

            if (!refineryInfo.FindRefineriesInfo(cubeGrid))
                return false;

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(KeyWord) || !container.IsFunctional)
                    continue;
                var inventory = container.GetInventory();
                if (!refinedInventory.IsConnectedTo(inventory))
                    continue;

                inventories.Add(container.GetInventory());
                if (Log.Debug) Log.Msg($"Added `{container.CustomName}`");

            }

            if (inventories.Count == 0)
            {
                Log.Msg("No container inventorries found");
                return false;
            }
            index = 0;
            return true;
        }

        internal void Refresh()
        {
            refineryInfo.Refresh();
        }

        internal bool RefineNext()
        {
            if (index >= inventories.Count || index >= MaxRefineries)
                return false;

            refineryInfo.Refresh();

            Result result = RefineContainer(inventories[index]);
            refineryInfo.ConsumeRefinarySeconds();
            if (Log.Debug) Log.Msg($"RefineContainer result={result}");

            switch (result)
            {
                case Result.Success:
                    {
                        index++;
                        break;
                    }

                default:
                    {
                        return false;
                    }
            }

            return index < inventories.Count;

        }

        internal void RefineEnd()
        {
            refineryInfo.EnableRefineries();
        }

        private Result RefineContainer(IMyInventory inventory)
        {
            foreach (var oreItemId in refiningInfoI.OrderedOreList)
            {
                if (refineryInfo.AvailableSeconds == 0)
                    return Result.NoTime;

                OreToIngotInfo info = null;
                if (!refiningInfoI.OreToIngots.TryGetValue(oreItemId, out info))
                {
                    Log.Msg($"Failed to get info for {oreItemId}");
                    return Result.Error;
                }

                Result result = RefineInventoryOre(inventory, info);
                if (Log.Debug) Log.Msg($"RefineInventoryOre result={result}");
                switch (result)
                {
                    case Result.Success:
                        break;
                    case Result.NotEnoughOre:
                        break;
                    default:
                        {
                            return result;
                        }
                }
            }
            return Result.Success;
        }

        private Result RefineInventoryOre(IMyInventory inventory, OreToIngotInfo info)
        {
            if (Log.Debug) Log.Msg($"Refining {info.ItemId.SubtypeName}");

            int oreAmount = (int)inventory.GetItemAmount(info.ItemId);
            if (oreAmount == 0)
                return Result.NotEnoughOre;

            double prereqAmount = (double)info.Amount;
            int bpRuns = (int)Math.Truncate(oreAmount / prereqAmount);
            if (bpRuns == 0)
                return Result.NotEnoughOre;
            if (Log.Debug) Log.Msg($"Found {info.ItemId.SubtypeName}={oreAmount} bpRuns={bpRuns}");

            int freeVolume = (int)(inventory.MaxVolume - inventory.CurrentVolume);
            float deltaVolume = info.IngotsVolume * refineryInfo.AvgYieldMultiplier - (info.Volume * (float)info.Amount);

            if (deltaVolume > 0)
            {
                bpRuns = (int)Math.Min(freeVolume / deltaVolume, bpRuns);
                if (bpRuns < 1)
                    return Result.NotEnoughVolume;
            }
            if (Log.Debug) Log.Msg($"After volume check bpRuns={bpRuns}");
            //power check.
            bpRuns = ConsumeRefinaryTime(info.ProductionTime, bpRuns);
            if (bpRuns == 0)
                return Result.NoTime;
            if (Log.Debug) Log.Msg($"After time check bpRuns={bpRuns}");

            inventory.RemoveItemsOfType(bpRuns * info.Amount, info.ItemId);

            foreach (var ingot in info.Ingots)
            {
                MyFixedPoint ingotAmount = ingot.Amount * (refineryInfo.AvgYieldMultiplier * bpRuns);
                inventory.AddItems(ingotAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(ingot.ItemId));
            }
            return Result.Success;
        }

        private int ConsumeRefinaryTime(float productionTime, int bpRuns)
        {
            var neededTime = productionTime * bpRuns;
            if (neededTime <= refineryInfo.AvailableSeconds)
            {
                refineryInfo.AvailableSeconds -= neededTime;
                return bpRuns;
            }

            bpRuns = (int)(refineryInfo.AvailableSeconds / productionTime);
            refineryInfo.AvailableSeconds = 0;
            return bpRuns;
        }

        internal void ReFillInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
        {
            Log.Msg("ReFillInventories");

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(KeyWord) || !container.IsFunctional)
                    continue;
                var inventory = container.GetInventory();
                if (!refinedInventory.IsConnectedTo(inventory))
                    continue;

                inventory.Clear();
                MyFixedPoint amount = 1000;
                foreach (var oreItemId in refiningInfoI.OrderedOreList)
                {
                    inventory.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(oreItemId));
                }

                Log.Msg($"Refilled `{container.CustomName}`");

            }
        }
    }
}
