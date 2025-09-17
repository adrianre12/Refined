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
        private string keyWord = "Rfnd";

        private ReactorInfo reactorInfo = new ReactorInfo();

        internal float TotalPower;
        internal float TotalSpeed;
        internal float AvgYieldMultiplier;

        internal float MaxSeconds;
        internal float AvailableSeconds;
        private int offlineS;

        private List<IMyRefinery> refineryList = new List<IMyRefinery>();

        public RefineryInfo(int offlineS)
        {
            this.offlineS = offlineS;
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
                //Log($"FatBlock={block.BlockDefinition.TypeId} {block.BlockDefinition.SubtypeName} {block.DetailedInfo} Enabled={block.Enabled} IsFunctional={block.IsFunctional}");

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
                block.Enabled = false;
            }
            AvgYieldMultiplier = sumYieldMultiplier / refinaryCount;
            if (Log.Debug) Log.Msg($"avgYieldMultiplier={AvgYieldMultiplier} refineriesTotalSpeed={TotalSpeed} refineriesTotalPower={TotalPower}");

            if (TotalPower == 0)
            {
                if (Log.Debug) Log.Msg("No Refineries found.");
                return false;
            }
            if (TotalPower > reactorInfo.MaxPower)
            {
                if (Log.Debug) Log.Msg("Not enough reactor power.");
                return false;
            }
            CalcRefinarySeconds();
            if (MaxSeconds < 1)
            {
                if (Log.Debug) Log.Msg("Not enough refinary process time.");
                return false;
            }
            return true;
        }

        internal void EnableRefineries()
        {
            foreach (var block in refineryList)
            {
                block.Enabled = true;
            }
        }

        private void CalcRefinarySeconds()
        {
            MaxSeconds = Math.Min(reactorInfo.MWseconds, offlineS) / TotalPower * TotalSpeed;
            AvailableSeconds = MaxSeconds;
        }

        internal void Refresh()
        {
            reactorInfo.Refresh();
            CalcRefinarySeconds();
            if (Log.Debug) Log.Msg($"MaxSeconds={MaxSeconds} AvialableSeconds{AvailableSeconds}");

        }

        internal void ConsumeRefinarySeconds()
        {
            reactorInfo.ConsumeUranium((MaxSeconds - AvailableSeconds) * TotalPower / TotalSpeed);
        }

    }
}
