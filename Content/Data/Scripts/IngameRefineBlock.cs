using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
//using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Refined.Controller
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false, "LargeBlockRefined")]
    public class RefinedGameLogic : MyGameLogicComponent
    {
        public const long DefaultMinOffline = 1;
        private IMyCargoContainer myRefinedBlock;
        public static Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");
        //MyIni config = new MyIni();

        private bool run = false;
        private int UpdateCounter = 0;
        private int refreshCounterLimit = 3; //4.8s
        private bool debugLog = true;

        private string lastTimeStr;
        private long deltaTimeS; //seconds
        private long nowS;
        private long lastS;
        private long minOffline = DefaultMinOffline;
        private float baseRefineSpeed;
        private float baseMaterialEfficiency;
        private float baseOperationalPowerConsumption;
        private float totalOreRate = 0;
        private float totalPower = 0;

        private List<MyInventoryItem> inventory = new List<MyInventoryItem>();
        private MyItemType uraniumId = MyItemType.MakeIngot("Uranium");
        private MyInventoryItem uraniumItem;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();

            MyRefineryDefinition baseRefinaryDefinition = (MyRefineryDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(MyDefinitionId.Parse("MyObjectBuilder_Refinery/LargeRefinery"));
            baseRefineSpeed = baseRefinaryDefinition.RefineSpeed;
            baseMaterialEfficiency = baseRefinaryDefinition.MaterialEfficiency;
            baseOperationalPowerConsumption = baseRefinaryDefinition.OperationalPowerConsumption;
            Log(debugLog, $"baseRefineSpeed={baseRefineSpeed} baseMaterialEfficiency={baseMaterialEfficiency} baseOperationalPowerConsumption={baseOperationalPowerConsumption}");

            Log(false, "Loaded...");

            run = true;
        }

        public override void UpdateAfterSimulation100()
        {
            if (!run)
                return;

            if (!MyAPIGateway.Session.IsServer)
                return;

            UpdateCounter++;

            if (UpdateCounter <= refreshCounterLimit)
                return;

            Log(debugLog, "Processing...");

            nowS = DateTime.Now.Ticks / 1000; //TimeSpan.TicksPerSecond;
            deltaTimeS = 0;

            if (Entity.Storage.TryGetValue(LastTimeKey, out lastTimeStr))
            {
                lastS = Convert.ToInt64(lastTimeStr);
                if (lastS != 0)
                    deltaTimeS = nowS - lastS;

                Log(debugLog, $"nowS={nowS} lastS={lastS} deltaTimeS={deltaTimeS}");
            }
            else
            {
                Log(false, $"LastTimeKey not loaded {LastTimeKey}");
            }

            Entity.Storage[LastTimeKey] = nowS.ToString();

            Log(debugLog, $"deltaTimeS = {deltaTimeS}");

            if (deltaTimeS < minOffline)
                return;


            //ProcessInventory();

            ExamineRefineries();




        }

        private void ExamineRefineries()
        {
            float productivity;
            float effectiveness;
            float powerEfficiency;
            totalOreRate = 0;
            totalPower = 0;

            foreach (var block in myRefinedBlock.CubeGrid.GetFatBlocks<Sandbox.ModAPI.IMyRefinery>())
            {
                Log(debugLog, $"FatBlock = {block.BlockDefinition.TypeId} {block.BlockDefinition.SubtypeName} {block.DetailedInfo}");

                productivity = block.UpgradeValues["Productivity"];
                effectiveness = block.UpgradeValues["Effectiveness"];
                powerEfficiency = block.UpgradeValues["PowerEfficiency"];
                Log(debugLog, $"Productivity={productivity} Effectiveness={effectiveness} PowerEfficiency={powerEfficiency}");

                totalOreRate += (baseRefineSpeed + productivity) * baseMaterialEfficiency * effectiveness;
                totalPower += baseOperationalPowerConsumption / powerEfficiency * (1 + productivity);

                //var tmp = (MyRefineryDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition);
                //Log(debugLog, $"Speed={tmp.RefineSpeed} Effectivness={tmp.MaterialEfficiency} power={tmp.OperationalPowerConsumption}");
            }
            Log(debugLog, $"totalOreRate={totalOreRate} totalPower={totalPower}");
        }

        private void ProcessInventory()
        {
            inventory.Clear();
            myRefinedBlock.GetInventory().GetItems(inventory);
            if (inventory == null)
            {
                Log(debugLog, "No Inventory Items");
                return;
            }


            foreach (var item in inventory)
            {
                if (item.Type == uraniumId)
                {
                    Log(debugLog, "Found uranium ingots");
                    uraniumItem = item;
                    break;
                }
            }

            foreach (var item in inventory)
            {
                MyFixedPoint invAmountCent = (MyFixedPoint)(Math.Truncate(((double)item.Amount) / 100.0) * 100);
                Log(debugLog, $"{item.Type.TypeId} - {item.ItemId.ToString()} - {item.Type.ToString()} = {item.Amount} cent={invAmountCent}");
            }


            MyFixedPoint amountReq;

            MyBlueprintDefinitionBase.Item[] ingots;
            //MyObjectBuilder_Ore/Iron

            if (RefineOre.TryGetIngots("Iron", out amountReq, out ingots))
            {

                foreach (var ingot in ingots)
                {
                    Log(debugLog, $"Ingot={ingot.Id.SubtypeName}  AmountReq={amountReq}");
                }
            }
            else
            {
                Log(debugLog, "Nothing found");
            }
        }

        private void Log(bool debug, string msg)
        {
            MyLog.Default.WriteLineIf(debug, msg);
        }
    }
}