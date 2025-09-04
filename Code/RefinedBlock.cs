using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Catopia.Refined
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false, "LargeBlockRefined")]
    public class RefinedGameLogic : MyGameLogicComponent
    {
        public const long DefaultMinOffline = 30 * 60; //seconds
        public const int PollPeriod = 3; //4.8s

        private IMyCargoContainer myRefinedBlock;
        private Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private bool run = false;
        private int updateCounter = PollPeriod;
        private string keyWord = "Rfnd";
        private RefineOre refineOre;
        private Inventories inventories;

        private float totalPower;
        private long totalRefineryS;
        private float avgYieldMultiplier;

        private List<MyInventoryItem> inventory = new List<MyInventoryItem>();
        private MyItemType uraniumId = MyItemType.MakeIngot("Uranium");
        private MyInventoryItem uraniumItem;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();


            refineOre = new RefineOre();
            inventories = new Inventories(myRefinedBlock);

            Log.Msg("Loaded...");

            run = true;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
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
            FindRefineries(offlineS);

            inventories.FindContainerInventories(keyWord);

            Log.Msg($"Uranium={inventories.ItemAmount(uraniumId)}");

            //ProcessInventory();



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
                    offlineS = nowS - lastS;

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

        private void FindRefineries(long offlineS)
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
            totalRefineryS = 0;
            totalPower = 0;
            int refinaryCount = 0;
            float sumYieldMultiplier = 0;
            foreach (var block in myRefinedBlock.CubeGrid.GetFatBlocks<IMyRefinery>())
            {
                //Log($"FatBlock={block.BlockDefinition.TypeId} {block.BlockDefinition.SubtypeName} {block.DetailedInfo} Enabled={block.Enabled} IsFunctional={block.IsFunctional}");

                if (!block.CustomName.Contains(keyWord) || !block.Enabled || !block.IsFunctional)
                    return;

                if (!inventories.AddRefineryInventories(block))
                    return;

                productivity = block.UpgradeValues["Productivity"];
                effectiveness = block.UpgradeValues["Effectiveness"];
                powerEfficiency = block.UpgradeValues["PowerEfficiency"];
                //Log($"Productivity={productivity} Effectiveness={effectiveness} PowerEfficiency={powerEfficiency}");

                refinaryCount++;
                sumYieldMultiplier += effectiveness;
                totalRefineryS += (long)Math.Round((baseRefineSpeed + productivity) * refinerySpeedMultiplier * offlineS);
                totalPower += baseOperationalPowerConsumption / powerEfficiency * (1 + productivity);

            }
            avgYieldMultiplier = sumYieldMultiplier / refinaryCount;
            Log.Msg($"avgYieldMultiplier={avgYieldMultiplier} totalRefineryS ={totalRefineryS} totalPower={totalPower}");
        }

        private void ProcessInventory()
        {
            inventory.Clear();
            myRefinedBlock.GetInventory().GetItems(inventory);
            if (inventory == null)
            {
                Log.Msg("No Inventory Items");
                return;
            }


            foreach (var item in inventory)
            {
                if (item.Type == uraniumId)
                {
                    Log.Msg("Found uranium ingots");
                    uraniumItem = item;
                    break;
                }
            }

            MyFixedPoint amountReq;
            MyBlueprintDefinitionBase.Item[] ingots;

            foreach (var item in inventory)
            {
                if (item.Type.TypeId != "Ore")
                {
                    Log.Msg($"TypeId = {item.Type.TypeId}");
                    return;
                }
                MyFixedPoint invAmountCent = (MyFixedPoint)(Math.Truncate(((double)item.Amount) / 100.0) * 100);
                Log.Msg($"{item.Type.TypeId} - {item.ItemId.ToString()} - {item.Type.ToString()} = {item.Amount} cent={invAmountCent}");

                if (refineOre.TryGetIngots(item.Type.SubtypeId, out amountReq, out ingots))
                {

                }
            }




            //MyObjectBuilder_Ore/Iron

            /*            if (RefineOre.TryGetIngots("Iron", out amountReq, out ingots))
                        {

                            foreach (var ingot in ingots)
                            {
                                Log(debugLog, $"Ingot={ingot.Id.SubtypeName}  AmountReq={amountReq}");
                            }
                        }
                        else
                        {
                            Log(debugLog, "Nothing found");
                        }*/
        }

    }
}