using Sandbox.Definitions;
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

namespace SeRefined.Controller
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false, "LargeBlockRefined")]
    public class RefinedGameLogic : MyGameLogicComponent
    {
        IMyCargoContainer myRefinedBlock;
        //MyIni config = new MyIni();

        int UpdateCounter = 0;
        int refreshCounterLimit = 3; //4.8s
        bool debugLog = true;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            Log(false, "Loaded...");
        }

        public override void UpdateAfterSimulation100()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            UpdateCounter++;

            if (UpdateCounter <= refreshCounterLimit)
                return;

            Log(debugLog, "Processing...");

            DateTime now = DateTime.Now;
            Log(false, now.ToString());

            Entity.Storage

            Log(debugLog, "Inventory " + myRefinedBlock.InventoryCount);
            VRage.Game.ModAPI.IMyInventory inv;

            inv = myRefinedBlock.GetInventory();
            if (inv == null)
            {
                Log(debugLog, "No Inventory");
                return;
            }

            List<MyInventoryItem> inventory = new List<MyInventoryItem>();
            inv.GetItems(inventory);
            if (inventory == null)
            {
                Log(debugLog, "No Inventory Items");
                return;
            }

            foreach (var invItem in inventory)
            {
                MyFixedPoint invAmountCent = (MyFixedPoint)(Math.Truncate(((double)invItem.Amount) / 100.0) * 100);
                Log(debugLog, $"{invItem.Type.TypeId} - {invItem.ItemId.ToString()} - {invItem.Type.ToString()} = {invItem.Amount} cent={invAmountCent}");
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