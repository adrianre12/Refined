using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Catopia.Refined
{
    internal class ContainerInfo
    {
        private CommonSettings settings = CommonSettings.Instance;

        private List<IMyInventory> inventories = new List<IMyInventory>();
        private MyDefinitionId SCDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");

        private RefiningInfo refiningInfoI = RefiningInfo.Instance;
        private RefineryInfo refineryInfo;
        private int index;
        private RefinedBlock refined;
        internal int CreditUnitsMax;
        private int creditUnitsAvailable;
        private int oresProcessed = 0;
        private int priceYieldMultiplierCount = 0;
        private float priceYieldMultiplierSum = 0;
        private Stopwatch stopwatch = new Stopwatch();

        internal enum Result
        {
            Error,
            Success,
            NoTime,
            NotEnoughVolume,
            NotEnoughOre
        }

        internal ContainerInfo() { }

        internal ContainerInfo(RefinedBlock refined, int offlineS)
        {
            this.refined = refined;
            refineryInfo = new RefineryInfo(refined, offlineS);
        }

        internal bool FindContainerInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid, bool preLoad = false)
        {
            if (preLoad) return false;

            if (Log.Debug) Log.Msg("FindContainerInventories");
            if (settings.EnableTiming) stopwatch.Restart();



            if (!refineryInfo.FindRefineriesInfo(cubeGrid))
                return false;

            if (settings.EnableTiming) Log.Msg($"FindInventories Elapsed after refineries {stopwatch.ElapsedTicks / 10.0} uS");

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(RefinedBlock.KeyWord) || !container.IsFunctional)
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
                refined.screen0.AddText("No containers found.");
                refined.screen0.AddText($"Have you added Keyword {RefinedBlock.KeyWord}");

                return false;
            }
            refined.screen0.RunInfo.NumContainers = inventories.Count;
            refined.screen0.Dirty = true;

            index = 0;

            if (settings.EnableTiming) Log.Msg($"FindInventories Elapsed total {stopwatch.ElapsedTicks / 10.0} uS");

            return true;
        }

        internal void Refresh()
        {
            refineryInfo.Refresh();
        }

        /*        private void CalcCreditUnits()
                {
                    if (settings.PricePerUnit <= 0)
                    {
                        CreditUnitsMax = 0;
                        creditUnitsAvailable = 0;
                        return;
                    }

                    var scAmount = (int)refined.RefinedInventory.GetItemAmount(SCDefId);

                    CreditUnitsMax = (scAmount * 3600) / settings.PricePerUnit;

                    creditUnitsAvailable = CreditUnitsMax;
                    if (Log.Debug) Log.Msg($"CalcCreditUnits CreditUnitsMax={CreditUnitsMax}");
                }*/

        internal void ConsumeCreditUnits(bool preLoad = false)
        {
            if (preLoad) return;

            if (settings.PricePerUnit <= 0)
                return;

            int creditSecondsUsed = CreditUnitsMax - creditUnitsAvailable;
            MyFixedPoint removeAmount = MyFixedPoint.Min(refined.RefinedInventory.GetItemAmount(SCDefId), (MyFixedPoint)Math.Ceiling(creditSecondsUsed * settings.PricePerUnit * (1 / 3600.0)));
            CreditUnitsMax = creditUnitsAvailable;
            if (removeAmount > 0)
                refined.RefinedInventory.RemoveItemsOfType(removeAmount, SCDefId);
            refined.screen0.RunInfo.SCpaid += (int)removeAmount;
            refined.screen0.RunInfo.CreditUnitsUsed += creditSecondsUsed;
            if (Log.Debug) Log.Msg($"ConsumeCreditUnits ConsumeCreditUnits={creditSecondsUsed} SC removeAmount={removeAmount}");
        }

        internal bool RefineNext(bool preLoad = false)
        {
            if (preLoad) return false;

            //if (settings.EnableTiming) stopwatch.Restart();

            if (index >= inventories.Count || index >= settings.MaxRefineries)
                return false;

            refineryInfo.DisableRefineries();

            refineryInfo.Refresh();

            //CalcCreditUnits(); // Much faster inlining it.
            {
                if (settings.PricePerUnit <= 0)
                {
                    CreditUnitsMax = 0;
                    creditUnitsAvailable = 0;

                }
                else
                {

                    var scAmount = (int)refined.RefinedInventory.GetItemAmount(SCDefId);

                    CreditUnitsMax = (scAmount * 3600) / settings.PricePerUnit;

                    creditUnitsAvailable = CreditUnitsMax;
                    if (Log.Debug) Log.Msg($"CalcCreditUnits CreditUnitsMax={CreditUnitsMax}");
                }
            }
            //if (settings.EnableTiming) Log.Msg($"RefineContainer pre {stopwatch.ElapsedTicks / 10.0} uS");

            Result result = RefineContainer(inventories[index]);
            //if (settings.EnableTiming) Log.Msg($"RefineContainer post {stopwatch.ElapsedTicks / 10.0} uS");

            ConsumeRefinaryTime();
            ConsumeCreditUnits();

            if (Log.Debug) Log.Msg($"RefineContainer result={result}\n---------------------------------------");

            switch (result)
            {
                case Result.Success:
                    {
                        index++;
                        break;
                    }

                default:
                    {
                        if (settings.EnableTiming) Log.Msg($"RefineNext NotSuccess Elapsed end {stopwatch.ElapsedTicks / 10.0} uS");
                        return false;
                    }
            }
            //if (settings.EnableTiming) Log.Msg($"RefineNext Success Elapsed end {stopwatch.ElapsedTicks / 10.0} uS");
            return index < inventories.Count;
        }


        internal void RefineEnd(bool preLoad = false)
        {
            if (preLoad) return;

            refineryInfo.EnableRefineries();
            refined.screen0.RunInfo.OresProcessed = oresProcessed;
            refined.screen0.RunInfo.AvgPercentCharge = priceYieldMultiplierCount == 0 ? 0 : 100 - (100 * priceYieldMultiplierSum / priceYieldMultiplierCount);
        }

        internal Result RefineContainer(IMyInventory inventory, bool preLoad = false)
        {
            if (preLoad) return Result.Error;

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

                //if (settings.EnableTiming) Log.Msg($"RefineInventoryOre pre {stopwatch.ElapsedTicks / 10.0} uS");
                Result result = RefineInventoryOre(inventory, info);
                //if (settings.EnableTiming) Log.Msg($"RefineInventoryOre post {stopwatch.ElapsedTicks / 10.0} uS");

                if (Log.Debug) Log.Msg($"RefineInventoryOre result={result}");
                switch (result)
                {
                    case Result.Success:
                        break;
                    case Result.NotEnoughOre:
                        break;
                    default:
                        {
                            //if (settings.EnableTiming) Log.Msg($"RefineContainer NotSuccess Elapsed end {stopwatch.ElapsedTicks / 10.0} uS");
                            return result;
                        }
                }
            }
            return Result.Success;
        }

        internal Result RefineInventoryOre(IMyInventory inventory, OreToIngotInfo info, bool preLoad = false)
        {
            if (preLoad) return Result.Error;

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
            bpRuns = RefinaryTimeCheck(info.ProductionTime, bpRuns, out priceYieldMultiplier);
            if (bpRuns == 0)
                return Result.NoTime;
            if (Log.Debug) Log.Msg($"After time check bpRuns={bpRuns}");

            inventory.RemoveItemsOfType(bpRuns * info.Amount, info.ItemId);

            oresProcessed += bpRuns * (int)info.Amount;
            priceYieldMultiplierSum += priceYieldMultiplier;
            ++priceYieldMultiplierCount;

            for (int i = 0; i < info.Ingots.Count; i++)
            {
                var ingot = info.Ingots[i];
                inventory.AddItems(ingot.Amount * (refineryInfo.AvgYieldMultiplier * bpRuns * priceYieldMultiplier),
                    (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(ingot.ItemId));
            }

            return Result.Success;
        }

        internal int RefinaryTimeCheck(float productionTime, int bpRuns, out float priceYieldMultiplier, bool preLoad = false)
        {
            priceYieldMultiplier = 1;
            if (preLoad) return 0;

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

            if (settings.PaymentType == CommonSettings.PaymentMode.PerHour)
            {
                if (Log.Debug) Log.Msg($"RefinaryTimeCheck creditSecondsAvailable={creditUnitsAvailable}");

                if (creditUnitsAvailable <= 0)
                { // run out of credit
                    priceYieldMultiplier = settings.PriceYieldMultiplier;
                }
                else if (neededTime <= creditUnitsAvailable)
                {
                    creditUnitsAvailable -= neededTime;
                }
                else
                {
                    //lerp
                    priceYieldMultiplier = settings.PriceYieldMultiplier + creditUnitsAvailable / neededTime * (1 - settings.PriceYieldMultiplier);
                    creditUnitsAvailable = 0;
                }
                if (Log.Debug) Log.Msg($"creditSecondsAvailable={creditUnitsAvailable} priceYieldMultiplier={priceYieldMultiplier}");
            }

            return bpRuns;
        }

        internal void ConsumeRefinaryTime(bool preLoad = false)
        {
            if (preLoad) return;

            int elapsedTime = refineryInfo.RefinaryElapsedTime();

            int chargeableTime = (int)(elapsedTime * settings.PricePowerMultiplier);

            float powerMultiplier = 0;

            if (settings.PaymentType == CommonSettings.PaymentMode.PerMWh)
            {
                if (creditUnitsAvailable <= 0)
                { // run out of credit
                    powerMultiplier = settings.PricePowerMultiplier;

                }
                else if (chargeableTime <= creditUnitsAvailable)
                {
                    creditUnitsAvailable -= chargeableTime;
                }
                else
                {
                    //lerp
                    powerMultiplier = settings.PricePowerMultiplier - creditUnitsAvailable * settings.PricePowerMultiplier * 1.0f / chargeableTime;
                    creditUnitsAvailable = 0;
                }
                if (Log.Debug) Log.Msg($"RefinaryElapsedTime={elapsedTime} chargeableTime={chargeableTime} creditSecondsAvailable={creditUnitsAvailable} powerMultiplier={powerMultiplier}");
            }

            refineryInfo.ConsumeUranium(elapsedTime, powerMultiplier);
        }

        internal void ReFillInventories(IMyInventory refinedInventory, IMyCubeGrid cubeGrid)
        {
            Log.Msg("ReFillInventories");

            foreach (var container in cubeGrid.GetFatBlocks<IMyCargoContainer>())
            {
                if (!container.CustomName.Contains(RefinedBlock.KeyWord) || !container.IsFunctional)
                    continue;
                var inventory = container.GetInventory();
                if (!refinedInventory.IsConnectedTo(inventory))
                    continue;

                inventory.Clear();
                MyFixedPoint amount = (MyFixedPoint)(inventory.MaxVolume * (2702.0f / refiningInfoI.OrderedOreList.Count)).ToIntSafe(); //100000;
                Log.Msg($"Refill amount={amount} MaxVolume={inventory.MaxVolume}  oreCount={refiningInfoI.OrderedOreList.Count}");
                foreach (var oreItemId in refiningInfoI.OrderedOreList)
                {
                    inventory.AddItems(amount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(oreItemId));
                }

                Log.Msg($"Refilled `{container.CustomName}`");

            }
        }
    }
}
