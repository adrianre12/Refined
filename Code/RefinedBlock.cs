using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Ingame = VRage.Game.ModAPI.Ingame;

namespace Catopia.Refined
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false, "LargeBlockRefined")]
    public class RefinedGameLogic : MyGameLogicComponent
    {
        public const int DefaultMinOffline = 30 * 60; //seconds
        public const int DefaultMaxOffline = 168 * 3600; //seconds
        public const int PollPeriod = 3; //4.8s
        public const float MWsPerU = 1.0f * 3600;

        private IMyCargoContainer myRefinedBlock;
        private Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private bool run = false;
        private int updateCounter = PollPeriod;
        private string keyWord = "Rfnd";
        private Inventories inventories;

        private List<Ingame.MyInventoryItem> inventory = new List<Ingame.MyInventoryItem>();
        private Ingame.MyItemType uraniumId = Ingame.MyItemType.MakeIngot("Uranium");
        //private MyDefinitionId UDefId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Uranium");

        private float reactorMaxPower;
        private int reactorAvaialbleUranium;
        private int reactorReserveUranium = 50;
        private float reactorConsumedUranium;

        private float refineriesTotalPower;
        private float refineriesTotalSpeed;
        private float avgYieldMultiplier;

        private float maxRefinerySeconds;
        private float availableRefinerySeconds;

        private RefineInfo refineInfoI;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();


            inventories = new Inventories(myRefinedBlock);

            Log.Msg("Loaded...");

            run = true;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            refineInfoI = RefineInfo.Instance;
            refineInfoI.NewProcessOrderList();

            //            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!run)
                return;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (--updateCounter > 0)
                return;
            updateCounter = PollPeriod;

            long offlineS;
            /*if (NotPaused(out offlineS))
                return;*/

            Log.Msg("Processing...");

            offlineS = 1000;
            inventories.Clear();

            FindReactorInfo();

            FindRefineriesInfo();

            maxRefinerySeconds = reactorAvaialbleUranium * MWsPerU / refineriesTotalPower * refineriesTotalSpeed;
            availableRefinerySeconds = maxRefinerySeconds;

            inventories.FindContainerInventories(keyWord);

            /*            int maxRuntimeS = (int)Math.Min(offlineS, 3600.0 * reactorUranium * MWhPerU / refineriesTotalPower);
                        Log.Msg($"reactorMaxPower={reactorMaxPower} reactorUranium={reactorUranium} refineriesTotalPower={refineriesTotalPower} powerRuntimeS={3600 * reactorUranium * MWhPerU / refineriesTotalPower}");

                        int refinaryTimeS = (int)(maxRuntimeS * refineriesTotalSpeed);
                        Log.Msg($" maxRuntimeS={maxRuntimeS} refinaryTimeS={refinaryTimeS}");

                        var refineOre = RefineOre.Instance;
                        ProcessInventory();
            */


            //run = false;


        }

        private bool NotPaused(out long offlineS)
        {
            long nowS = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            offlineS = 0;
            string lastTimeStr;
            if (Entity.Storage.TryGetValue(LastTimeKey, out lastTimeStr))
            {
                long lastS = Convert.ToInt64(lastTimeStr);
                if (lastS != 0)
                    offlineS = Math.Min(DefaultMaxOffline, nowS - lastS);

                Log.Msg($"nowS={nowS} lastS={lastS} deltaTimeS={offlineS}");
            }
            else
            {
                Log.Msg($"LastTimeKey not loaded {LastTimeKey}");
            }

            Entity.Storage[LastTimeKey] = nowS.ToString();

            Log.Msg($"deltaTimeS = {offlineS}");

            return offlineS < DefaultMinOffline;
        }

        private void FindReactorInfo()
        {
            reactorMaxPower = 0;
            reactorAvaialbleUranium = 0;
            reactorConsumedUranium = 0;
            int amountU = 0;
            Log.Msg("FindReactorInfo");
            foreach (var block in myRefinedBlock.CubeGrid.GetFatBlocks<IMyReactor>())
            {
                if (!block.Enabled || !block.IsFunctional)
                    return;
                amountU = (int)block.GetInventory().GetItemAmount(uraniumId) - reactorReserveUranium;
                if (amountU <= 0)
                    continue;
                reactorAvaialbleUranium += amountU;
                reactorMaxPower += block.MaxOutput;
                //Log.Msg($"Reactor {block.CustomName} maxOutput={block.MaxOutput} amountU={amountU}");
            }
        }

        internal void FindRefineriesInfo()
        {
            float productivity;
            float effectiveness;
            float powerEfficiency;

            MyRefineryDefinition baseRefinaryDefinition = (MyRefineryDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(MyDefinitionId.Parse("MyObjectBuilder_Refinery/LargeRefinery"));
            float baseRefineSpeed = baseRefinaryDefinition.RefineSpeed;
            float baseMaterialEfficiency = baseRefinaryDefinition.MaterialEfficiency;
            float baseOperationalPowerConsumption = baseRefinaryDefinition.OperationalPowerConsumption;
            float refinerySpeedMultiplier = MyAPIGateway.Session.RefinerySpeedMultiplier;
            //Log($"baseRefineSpeed={baseRefineSpeed} baseMaterialEfficiency={baseMaterialEfficiency} baseOperationalPowerConsumption={baseOperationalPowerConsumption} refinerySpeedMultiplier={refinerySpeedMultiplier}");

            avgYieldMultiplier = 0;
            refineriesTotalSpeed = 0;
            refineriesTotalPower = 0;
            int refinaryCount = 0;
            float sumYieldMultiplier = 0;
            foreach (var block in myRefinedBlock.CubeGrid.GetFatBlocks<IMyRefinery>())
            {
                //Log($"FatBlock={block.BlockDefinition.TypeId} {block.BlockDefinition.SubtypeName} {block.DetailedInfo} Enabled={block.Enabled} IsFunctional={block.IsFunctional}");

                if (!block.CustomName.Contains(keyWord) || !block.Enabled || !block.IsFunctional)
                    return;

                //if (!inventories.AddRefineryInventories(block))
                //    return;

                productivity = block.UpgradeValues["Productivity"];
                effectiveness = block.UpgradeValues["Effectiveness"];
                powerEfficiency = block.UpgradeValues["PowerEfficiency"];
                //Log($"Productivity={productivity} Effectiveness={effectiveness} PowerEfficiency={powerEfficiency}");

                refinaryCount++;
                sumYieldMultiplier += effectiveness;
                refineriesTotalSpeed += (float)Math.Round((baseRefineSpeed + productivity) * refinerySpeedMultiplier);
                refineriesTotalPower += baseOperationalPowerConsumption / powerEfficiency * (1 + productivity);

            }
            avgYieldMultiplier = sumYieldMultiplier / refinaryCount;
            Log.Msg($"avgYieldMultiplier={avgYieldMultiplier} refineriesTotalSpeed ={refineriesTotalSpeed} refineriesTotalPower={refineriesTotalPower}");
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
            float deltaVolume = info.IngotsVolume * avgYieldMultiplier - (info.Volume * (float)info.Amount);

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
                MyFixedPoint ingotAmount = ingot.Amount * (avgYieldMultiplier * bpRuns);
                inventory.AddItems(ingotAmount, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(ingot.ItemId));
            }

        }

        private int ConsumeRefinaryTime(float productionTime, int bpRuns)
        {
            var neededTime = productionTime * bpRuns;
            if (neededTime <= availableRefinerySeconds)
            {
                availableRefinerySeconds -= neededTime;
                return bpRuns;
            }

            availableRefinerySeconds = 0;
            return (int)(availableRefinerySeconds / productionTime);
        }

        private void ProcessInventory()
        {

        }

    }
}