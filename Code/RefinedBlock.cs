using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;

namespace Catopia.Refined
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, "RefinedBlock")]

    public class RefinedBlock : MyGameLogicComponent
    {
        public const int DefaultMinOffline = 30 * 60; //seconds
        public const int DefaultMaxOffline = 168 * 3600; //seconds
        public const int PollPeriod = 3; //4.8s

        private static Dictionary<long, long> blockRegister = new Dictionary<long, long>();

        private IMyTextPanel myRefinedBlock;

        private Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private int updateCounter = PollPeriod;

        private int offlineS;
        private ContainerInfo containers;

        private Stopwatch stopWatch = new Stopwatch();

        internal MySync<bool, SyncDirection.BothWays> testButtonState;

        private ScreenRefined screen0;

        private enum RunState
        {
            Monitoring,
            Detected,
            Processing
        }

        private RunState runState;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyTextPanel;
            testButtonState.Value = false;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();

            Log.Msg("Loaded...");
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            TerminalControls.DoOnce(ModContext);

            if (!MyAPIGateway.Session.IsServer)
                return;

            var refiningInfoI = RefiningInfo.Instance; // create instance now.

            runState = RunState.Monitoring;
            updateCounter = 0;
            testButtonState.ValueChanged += TestButtonState_ValueChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            myRefinedBlock.EnabledChanged += MyRefinedBlock_EnabledChanged;

            Log.Debug = true;

            screen0 = new ScreenRefined((IMyTextSurfaceProvider)myRefinedBlock, 0);
            screen0.ScreenText("Booting ...");
        }

        private void MyRefinedBlock_EnabledChanged(IMyTerminalBlock obj)
        {
            if (!myRefinedBlock.Enabled)
                Entity.Storage[LastTimeKey] = "0";
        }

        private void TestButtonState_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            containers = new ContainerInfo(0);
            updateCounter = PollPeriod;
            containers.ReFillInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid);
            long thenS = (DateTime.Now.Ticks / TimeSpan.TicksPerSecond) - 86400;
            Entity.Storage[LastTimeKey] = thenS.ToString();
        }

        public override void UpdateAfterSimulation100()
        {
            if (!myRefinedBlock.Enabled)
                return;

            stopWatch.Restart();

            if (--updateCounter > 0)
                return;
            updateCounter = PollPeriod;

            if (CheckDuplicate())
            {
                if (Log.Debug) Log.Msg($"Duplicate {myRefinedBlock.CustomName}");
                screen0.ScreenText($"Duplicate Refined Block");
                return;
            }
            if (Log.Debug) Log.Msg($"Runstate={runState}");

            switch (runState)
            {
                case RunState.Monitoring:
                    {
                        if (Paused())
                            runState = RunState.Detected;
                        break;
                    }
                case RunState.Detected:
                    {
                        if (Log.Debug) Log.Msg("Detected...");

                        containers = new ContainerInfo(offlineS);

                        if (!containers.FindContainerInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid))
                        {
                            if (Log.Debug) Log.Msg("Abandon Processing");
                            screen0.ScreenText($"Abandon Processing");

                            runState = RunState.Monitoring;

                        }
                        runState = RunState.Processing;
                        break;
                    }
                case RunState.Processing:
                    {
                        if (Log.Debug) Log.Msg("Processing...");

                        if (!containers.RefineNext())
                        {
                            containers.RefineEnd();
                            runState = RunState.Monitoring;
                        }
                        break;
                    }
            }


            Log.Msg($"Elapsed stopWatchticks={stopWatch.ElapsedTicks}");
        }

        private bool CheckDuplicate()
        {
            var gridId = myRefinedBlock.CubeGrid.EntityId;
            long id;
            if (!blockRegister.TryGetValue(gridId, out id))
            {
                blockRegister[gridId] = myRefinedBlock.EntityId;
                return false;
            }
            if (id == myRefinedBlock.EntityId)
                return false;
            return true;
        }

        internal void TestButtonToggle()
        {
            testButtonState.Value = !testButtonState.Value;
        }

        public override void Close()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            var gridId = myRefinedBlock.CubeGrid.EntityId;
            long id;
            if (!blockRegister.TryGetValue(gridId, out id))
                return;
            if (id == myRefinedBlock.EntityId)
                blockRegister.Remove(gridId);
            testButtonState.ValueChanged -= TestButtonState_ValueChanged;
            myRefinedBlock.EnabledChanged -= MyRefinedBlock_EnabledChanged;
        }

        private bool Paused()
        {
            long nowS = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            offlineS = 0;
            string lastTimeStr;
            if (Entity.Storage.TryGetValue(LastTimeKey, out lastTimeStr))
            {
                long lastS = Convert.ToInt64(lastTimeStr);
                if (lastS != 0)
                    offlineS = (int)Math.Min(DefaultMaxOffline, nowS - lastS);
            }
            else
            {
                Log.Msg($"LastTimeKey not loaded {LastTimeKey}");
            }

            Entity.Storage[LastTimeKey] = nowS.ToString();

            if (Log.Debug) Log.Msg($"deltaTimeS = {offlineS}");

            return offlineS > DefaultMinOffline;
        }

    }
}