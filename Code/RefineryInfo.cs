using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.ModAPI;


namespace Catopia.Refined
{
    internal class RefineryInfo
    {
        private ReactorInfo reactorInfo;

        internal float TotalPower;
        internal float TotalSpeed;
        internal float AvgYieldMultiplier;

        internal int MaxRefiningTime;
        internal int RemainingRefiningTime;
        private int remainingOfflineTime;

        private List<IMyRefinery> refineryList = new List<IMyRefinery>();
        private RefinedBlock refined;
        private bool refinariesDisabled;
        private Stopwatch stopwatch = new Stopwatch();
        private CommonSettings settings = CommonSettings.Instance;

        public RefineryInfo(RefinedBlock refined, int offlineS)
        {
            this.remainingOfflineTime = offlineS;
            this.refined = refined;
            reactorInfo = new ReactorInfo(refined);
        }

        internal bool FindRefineriesInfo(IMyCubeGrid cubeGrid)
        {
            if (Log.Debug) Log.Msg("FindRefineriesInfo");
            if (settings.EnableTiming) stopwatch.Restart();

            if (!reactorInfo.FindReactorInfo(cubeGrid))
                return false;
            if (settings.EnableTiming) Log.Msg($"FindRefinaries Elapsed after Reactors {stopwatch.ElapsedTicks / 10.0} uS");

            refineryList.Clear();

            float productivity;
            float effectiveness;
            float powerEfficiency;

            MyRefineryDefinition baseRefinaryDefinition = (MyRefineryDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(MyDefinitionId.Parse("MyObjectBuilder_Refinery/LargeRefinery"));
            float baseRefineSpeed = baseRefinaryDefinition.RefineSpeed;
            float baseMaterialEfficiency = baseRefinaryDefinition.MaterialEfficiency;
            float baseOperationalPowerConsumption = baseRefinaryDefinition.OperationalPowerConsumption;
            float refinerySpeedMultiplier = MyAPIGateway.Session.RefinerySpeedMultiplier;
            //Log($"baseRefineSpeed={baseRefineSpeed} baseMaterialEfficiency={baseMaterialEfficiency} baseOperationalPowerConsumption={baseOperationalPowerConsumption} refinerySpeedMultiplier={refinerySpeedMultiplier}");

            AvgYieldMultiplier = 0;
            TotalSpeed = 0;
            TotalPower = 0;
            int refinaryCount = 0;
            float sumYieldMultiplier = 0;
            foreach (var block in cubeGrid.GetFatBlocks<IMyRefinery>())
            {
                if (!block.CustomName.Contains(RefinedBlock.KeyWord) || !block.Enabled || !block.IsFunctional)
                    continue;

                productivity = block.UpgradeValues["Productivity"];
                effectiveness = block.UpgradeValues["Effectiveness"];
                powerEfficiency = block.UpgradeValues["PowerEfficiency"];
                if (Log.Debug) Log.Msg($"{block.CustomName} Productivity={productivity} Effectiveness={effectiveness} PowerEfficiency={powerEfficiency}");

                refinaryCount++;
                sumYieldMultiplier += effectiveness;
                TotalSpeed += (float)Math.Round((baseRefineSpeed + productivity) * refinerySpeedMultiplier);
                TotalPower += baseOperationalPowerConsumption / powerEfficiency * (1 + productivity);

                refineryList.Add(block);
            }
            AvgYieldMultiplier = sumYieldMultiplier / refinaryCount;
            if (Log.Debug) Log.Msg($"avgYieldMultiplier={AvgYieldMultiplier} refineriesTotalSpeed={TotalSpeed} refineriesTotalPower={TotalPower}");

            if (refinaryCount == 0)
            {
                if (Log.Debug) Log.Msg("No Refineries found.");
                refined.screen0.AddText("No Refineries found.");
                refined.screen0.AddText($"Have you added Keyword {RefinedBlock.KeyWord}");
                return false;
            }
            if (TotalPower > reactorInfo.MaxPower)
            {
                if (Log.Debug) Log.Msg("Not enough reactor power.");
                refined.screen0.AddText("Not enough reactor power.");
                return false;
            }
            CalcMaxRefiningTime();
            if (MaxRefiningTime < 1)
            {
                if (Log.Debug) Log.Msg("Not enough refinary process time.");
                refined.screen0.AddText("Not enough refinary process time.");
                return false;
            }

            refined.screen0.RunInfo.NumRefineries = refineryList.Count;
            refined.screen0.RunInfo.TotalPower = TotalPower;
            refined.screen0.RunInfo.TotalSpeed = TotalSpeed;
            refined.screen0.RunInfo.AvgYieldMultiplier = AvgYieldMultiplier;
            refined.screen0.Dirty = true;

            if (settings.EnableTiming) Log.Msg($"FindRefineries Elapsed total {stopwatch.ElapsedTicks / 10.0} uS");

            return true;
        }

        internal void DisableRefineries()
        {
            if (refinariesDisabled)
                return;
            refinariesDisabled = true;
            foreach (var block in refineryList)
            {
                block.Enabled = false;
            }
        }

        internal void EnableRefineries()
        {
            refinariesDisabled = false;
            foreach (var block in refineryList)
            {
                block.Enabled = true;
            }
        }

        private void CalcMaxRefiningTime()
        {
            MaxRefiningTime = (int)Math.Min(reactorInfo.MWseconds / TotalPower, remainingOfflineTime); //fudge for power cost
            RemainingRefiningTime = MaxRefiningTime;
            refined.screen0.RunInfo.MaxRefiningTime = MaxRefiningTime;
            refined.screen0.RunInfo.RemainingRefiningTime = RemainingRefiningTime;
            refined.screen0.Dirty |= true;
        }

        /*        internal void ConsumeRefinaryTime()
                {
                    var elapsedTime = MaxRefiningTime - RemainingRefiningTime;
                    remainingOfflineTime -= elapsedTime;
                    screen0.RunInfo.TotalRefiningTime += elapsedTime;

                    reactorInfo.ConsumeUranium(elapsedTime * TotalPower); //power cost uses prepaid or MWseconds
                }*/

        internal int RefinaryElapsedTime()
        {
            int elapsedTime = MaxRefiningTime - RemainingRefiningTime;
            remainingOfflineTime -= elapsedTime;
            refined.screen0.RunInfo.TotalRefiningTime += elapsedTime;

            return elapsedTime;
        }

        internal void ConsumeUranium(float elapsedTime, float powerMultiplier)
        {
            float mWseconds = elapsedTime * TotalPower;
            refined.screen0.RunInfo.MWhPayment = mWseconds * powerMultiplier * 1.0f / 3600;
            reactorInfo.ConsumeUranium(mWseconds * (1 + powerMultiplier));
        }

        internal void Refresh()
        {
            reactorInfo.Refresh();
            CalcMaxRefiningTime();
            if (Log.Debug) Log.Msg($"MaxRefiningUnits={MaxRefiningTime} RemainingRefiningUnits={RemainingRefiningTime}");

        }
    }
}
