using Sandbox.Game;
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
        private string keyWord;

        private CommonSettings settings = CommonSettings.Instance;

        private List<IMyInventory> inventories = new List<IMyInventory>();
        private MyInventory refinedInventory;
        private MyDefinitionId SCDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");

        private RefiningInfo refiningInfoI = RefiningInfo.Instance;
        private RefineryInfo refineryInfo;
        private int index;
        private ScreenRefined screen0;
        internal int CreditSecondsMax;
        private int creditSecondsAvailable;
        private int oresProcessed = 0;
        private int priceYieldMultiplierCount = 0;
        private float priceYieldMultiplierSum = 0;

        internal enum Result
        {
            Error,
            Success,
            NoTime,
            NotEnoughVolume,
            NotEnoughOre
        }
        internal ContainerInfo(ScreenRefined screen0, int offlineS, string keyWord, MyInventory refinedInventory)
        {
            this.screen0 = screen0;
            this.keyWord = keyWord;
            this.refinedInventory = refinedInventory;
            refineryInfo = new RefineryInfo(screen0, offlineS, keyWord);
        }

        internal bool FindContainerInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
        {
            if (Log.Debug) Log.Msg("FindContainerInventories");

            if (!refineryInfo.FindRefineriesInfo(cubeGrid))
                return false;

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(keyWord) || !container.IsFunctional)
                    continue;
                var inventory = container.GetInventory();
                if (!refinedInventory.IsConnectedTo(inventory))
                    continue;

                inventories.Add(container.GetInventory());
                if (Log.Debug) Log.Msg($"Added `{container.CustomName}`");

            }

            if (inventories.Count == 0)
            {
                if (Log.Debug) Log.Msg("No container inventories found");
                screen0.AddText("No containers found.");
                screen0.AddText($"Have you added Keyword {keyWord}");

                return false;
            }
            screen0.RunInfo.NumContainers = inventories.Count;
            screen0.Dirty = true;

            index = 0;
            return true;
        }

        internal void Refresh()
        {
            refineryInfo.Refresh();
        }
        private void CalcCreditSeconds()
        {
            var scAmount = (int)refinedInventory.GetItemAmount(SCDefId);
            CreditSecondsMax = (int)(scAmount * 3600 / settings.PricePerHour);
            creditSecondsAvailable = CreditSecondsMax;
        }

        private void ConsumeCreditSeconds()
        {
            int creditSecondsUsed = CreditSecondsMax - creditSecondsAvailable;
            MyFixedPoint removeAmount = MyFixedPoint.Min(refinedInventory.GetItemAmount(SCDefId), (MyFixedPoint)Math.Ceiling(creditSecondsUsed * settings.PricePerHour / 3600.0));
            CreditSecondsMax = creditSecondsAvailable;
            if (removeAmount > 0)
                refinedInventory.RemoveItemsOfType(removeAmount, SCDefId);
            screen0.RunInfo.SCpaid += (int)removeAmount;
            screen0.RunInfo.CreditSecondsUsed += creditSecondsUsed;
            if (Log.Debug) Log.Msg($"SC removeAmount={removeAmount}");
        }

        internal bool RefineNext()
        {
            if (index >= inventories.Count || index >= settings.MaxRefineries)
                return false;
            refineryInfo.DisableRefineries();
            refineryInfo.Refresh();
            CalcCreditSeconds();
            Result result = RefineContainer(inventories[index]);
            refineryInfo.ConsumeRefinaryTime();
            ConsumeCreditSeconds();
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
            screen0.RunInfo.OresProcessed = oresProcessed;
            screen0.RunInfo.AvgPercentCharge = priceYieldMultiplierCount == 0 ? 0 : 100 - (100 * priceYieldMultiplierSum / priceYieldMultiplierCount);
        }

        private Result RefineContainer(IMyInventory inventory)
        {
            foreach (var oreItemId in refiningInfoI.OrderedOreList)
            {
                if (refineryInfo.RemainingRefiningTime == 0)
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

            float priceYieldMultiplier;
            bpRuns = ConsumeRefinaryTime(info.ProductionTime, bpRuns, out priceYieldMultiplier);
            if (bpRuns == 0)
                return Result.NoTime;
            if (Log.Debug) Log.Msg($"After time check bpRuns={bpRuns}");

            inventory.RemoveItemsOfType(bpRuns * info.Amount, info.ItemId);

            oresProcessed += bpRuns * (int)info.Amount;
            priceYieldMultiplierSum += priceYieldMultiplier;
            ++priceYieldMultiplierCount;

            foreach (var ingot in info.Ingots)
            {
                MyFixedPoint ingotAmount = ingot.Amount * (refineryInfo.AvgYieldMultiplier * bpRuns * priceYieldMultiplier);
                inventory.AddItems(ingotAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(ingot.ItemId));
            }
            return Result.Success;
        }

        private int ConsumeRefinaryTime(float productionTime, int bpRuns, out float priceYieldMultiplier)
        {
            int neededTime = (int)(productionTime * bpRuns / refineryInfo.TotalSpeed);

            if (neededTime <= refineryInfo.RemainingRefiningTime)
            {
                refineryInfo.RemainingRefiningTime -= neededTime;
            }
            else
            {

                bpRuns = (int)(refineryInfo.RemainingRefiningTime / productionTime);
                refineryInfo.RemainingRefiningTime = 0;
                neededTime = (int)(productionTime * bpRuns / refineryInfo.TotalSpeed);
            }

            if (Log.Debug) Log.Msg($"creditSecondsAvailable={creditSecondsAvailable}");
            if (creditSecondsAvailable <= 0)
            { // run out of credit
                priceYieldMultiplier = settings.PriceYieldMultiplier;
            }
            else if (neededTime <= creditSecondsAvailable)
            {
                priceYieldMultiplier = 1;
                creditSecondsAvailable -= neededTime;
            }
            else
            {
                //lerp
                priceYieldMultiplier = settings.PriceYieldMultiplier + creditSecondsAvailable / neededTime * (1 - settings.PriceYieldMultiplier);
                creditSecondsAvailable = 0;
            }
            if (Log.Debug) Log.Msg($"creditSecondsAvailable={creditSecondsAvailable} priceYieldMultiplier={priceYieldMultiplier}");

            return bpRuns;
        }

        internal void ReFillInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
        {
            Log.Msg("ReFillInventories");

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(keyWord) || !container.IsFunctional)
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
