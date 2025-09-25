using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;


namespace Catopia.Refined
{
    internal class RefineryInfo
    {
        private string keyWord;

        private ReactorInfo reactorInfo;

        internal float TotalPower;
        internal float TotalSpeed;
        internal float AvgYieldMultiplier;

        internal int MaxRefiningTime;
        internal int RemainingRefiningTime;
        private int remainingOfflineTime;

        private List<IMyRefinery> refineryList = new List<IMyRefinery>();
        private ScreenRefined screen0;
        private bool refinariesDisabled;

        public RefineryInfo(ScreenRefined screen0, int offlineS, string keyWord)
        {
            this.remainingOfflineTime = offlineS;
            this.screen0 = screen0;
            this.keyWord = keyWord;
            reactorInfo = new ReactorInfo(screen0);
        }

        internal bool FindRefineriesInfo(IMyCubeGrid cubeGrid)
        {
            if (Log.Debug) Log.Msg("FindRefineriesInfo");
            if (!reactorInfo.FindReactorInfo(cubeGrid))
                return false;

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
                if (!block.CustomName.Contains(keyWord) || !block.Enabled || !block.IsFunctional)
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
                screen0.AddText("No Refineries found.");
                screen0.AddText($"Have you added Keyword {keyWord}");
                return false;
            }
            if (TotalPower > reactorInfo.MaxPower)
            {
                if (Log.Debug) Log.Msg("Not enough reactor power.");
                screen0.AddText("Not enough reactor power.");
                return false;
            }
            CalcMaxRefiningTime();
            if (MaxRefiningTime < 1)
            {
                if (Log.Debug) Log.Msg("Not enough refinary process time.");
                screen0.AddText("Not enough refinary process time.");
                return false;
            }

            screen0.RunInfo.NumRefineries = refineryList.Count;
            screen0.RunInfo.TotalPower = TotalPower;
            screen0.RunInfo.TotalSpeed = TotalSpeed;
            screen0.RunInfo.AvgYieldMultiplier = AvgYieldMultiplier;
            screen0.Dirty = true;

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
            MaxRefiningTime = (int)Math.Min(reactorInfo.MWseconds / TotalPower, remainingOfflineTime);
            RemainingRefiningTime = MaxRefiningTime;
            screen0.RunInfo.MaxRefiningTime = MaxRefiningTime;
            screen0.RunInfo.RemainingRefiningTime = RemainingRefiningTime;
            screen0.Dirty |= true;
        }

        internal void ConsumeRefinaryTime()
        {
            var elapsedTime = MaxRefiningTime - RemainingRefiningTime;
            remainingOfflineTime -= elapsedTime;
            screen0.RunInfo.TotalRefiningTime += elapsedTime;
            reactorInfo.ConsumeUranium(elapsedTime * TotalPower);
        }

        internal void Refresh()
        {
            reactorInfo.Refresh();
            CalcMaxRefiningTime();
            if (Log.Debug) Log.Msg($"MaxRefiningUnits={MaxRefiningTime} RemainingRefiningUnits={RemainingRefiningTime}");

        }
    }
}
