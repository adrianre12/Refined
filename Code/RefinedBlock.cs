using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Sync;

namespace Catopia.Refined
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false, "RefinedBlock")]

    public class RefinedBlock : MyGameLogicComponent
    {
        public const int LongPollPeriod = 6; //9.6s
        public const int ShortPollPeriod = 1; //1.6s
        public const int DefaultCheckingCounter = (int)(5 * 60 / (1.6 * 6)); //5 mins in 100tick the 6 is the longPoll

        private static Dictionary<long, long> blockRegister = new Dictionary<long, long>();

        private IMyTextPanel myRefinedBlock;
        internal string KeyWord = "Rfnd";

        private Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");
        private MyDefinitionId SCDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalObject), "SpaceCredit");
        internal MyInventory RefinedInventory;

        private int updateCounter = 0;
        private long checkingCounter = DefaultCheckingCounter;

        private int offlineS;
        private ContainerInfo containers;

        private Stopwatch stopWatch = new Stopwatch();

        internal MySync<bool, SyncDirection.BothWays> TestButtonState;
        internal MySync<int, SyncDirection.BothWays> SliderReserveUranium;

        internal ScreenRefined screen0;

        private enum RunState
        {
            Stopped,
            Checking,
            Monitoring,
            Detected,
            Processing
        }

        private RunState runState;

        private CommonSettings settings = CommonSettings.Instance;

        [ProtoContract(UseProtoMembersOnly = true)]
        internal class ModStorage
        {
            [ProtoMember(1)]
            public long LastTime;
            [ProtoMember(2)]
            public int ReserveUranium;

            public ModStorage()
            {
                LastTime = 0;
                ReserveUranium = 50;
            }
        }

        internal ModStorage Storage = new ModStorage();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyTextPanel;
            TestButtonState.Value = false;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();

            LoadFromModStorage();

            Log.Msg("Loaded...");
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            RefinedInventory = myRefinedBlock.GetInventory() as MyInventory;
            if (!MyAPIGateway.Utilities.IsDedicated) //client only
            {
                TerminalControls.DoOnce(ModContext);

                RefinedInventory.ContentsChanged += RefinedInventory_ContentsChanged;
                RefinedInventory_ContentsChanged(RefinedInventory);
            }

            if (!MyAPIGateway.Session.IsServer) // server only
                return;

            var refiningInfoI = RefiningInfo.Instance; // create instance now.

            runState = RunState.Monitoring;
            updateCounter = 0;



            screen0 = new ScreenRefined((IMyTextSurfaceProvider)myRefinedBlock, 0);
            screen0.AddText("Booting ...");

            if (Log.Debug) TestButtonState.ValueChanged += TestButtonState_ValueChanged;
            SliderReserveUranium.ValueChanged += SliderReserveUraniumServer_ValueChanged;
            myRefinedBlock.EnabledChanged += MyRefinedBlock_EnabledChanged;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            Log.Debug = true;
        }

        private void MyRefinedBlock_EnabledChanged(IMyTerminalBlock obj)
        {
            if (myRefinedBlock.Enabled)
                return;
            Entity.Storage[LastTimeKey] = "0";
            screen0.ClearText();
            screen0.ClearRun();
            screen0.AddText("Restart ...");
            runState = RunState.Checking;
            updateCounter = 0;
            checkingCounter = DefaultCheckingCounter;
        }


        public override void UpdateAfterSimulation100()
        {
            if (!myRefinedBlock.Enabled)
                return;

            stopWatch.Restart();

            if (--updateCounter > 0)
                return;
            updateCounter = LongPollPeriod;

            Run();

            screen0.Refresh();
            Log.Msg($"Elapsed stopWatchticks={stopWatch.ElapsedTicks}");

        }

        public void Run()
        {
            if (CheckDuplicate())
            {
                if (Log.Debug) Log.Msg($"Duplicate {myRefinedBlock.CustomName}");
                screen0.ClearText();
                screen0.AddText($"Duplicate Refined Block");
                return;
            }

            if (Log.Debug) Log.Msg($"Runstate={runState}");

            switch (runState)
            {
                case RunState.Stopped:
                    {
                        break;
                    }
                case RunState.Checking:
                    {
                        if (Log.Debug) Log.Msg("Checking...");
                        screen0.AddText($"Checking...");
                        runState = RunState.Monitoring;
                        containers = new ContainerInfo(this, 1);

                        if (!containers.FindContainerInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid))
                        {
                            if (Log.Debug) Log.Msg("Checking failed");
                            screen0.AddText($"Checking failed. ({checkingCounter})");
                            if (--checkingCounter > 0)
                                runState = RunState.Checking;
                            else
                            {
                                screen0.AddText($"Stopping.");
                                runState = RunState.Stopped;
                            }
                            break;
                        }

                        screen0.AddText($"Checking success. ({checkingCounter})");
                        updateCounter = ShortPollPeriod;
                        break;
                    }
                case RunState.Monitoring:
                    {
                        screen0.ScreenMode = ScreenRefined.Mode.Run;
                        if (Paused())
                        {
                            runState = RunState.Detected;
                            updateCounter = ShortPollPeriod;
                            break;
                        }

                        if (containers == null)
                        { //Only happens at first start and forces a check.
                            runState = RunState.Checking;
                            screen0.AddText($"Startup...");
                        }

                        break;
                    }
                case RunState.Detected:
                    {
                        if (Log.Debug) Log.Msg("Detected...");
                        screen0.ClearRun();
                        screen0.RunInfo.LastOfflineS = offlineS;
                        containers = new ContainerInfo(this, offlineS);

                        if (!containers.FindContainerInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid))
                        {
                            if (Log.Debug) Log.Msg("Abandon Processing");
                            screen0.AddText($"Abandon Processing");

                            runState = RunState.Checking;
                            break;
                        }
                        screen0.ScreenMode = ScreenRefined.Mode.Run;
                        runState = RunState.Processing;
                        updateCounter = ShortPollPeriod;

                        break;
                    }
                case RunState.Processing:
                    {
                        if (Log.Debug) Log.Msg("Processing...");
                        if (!containers.RefineNext())
                        {
                            containers.RefineEnd();
                            runState = RunState.Monitoring;
                            break;
                        }
                        screen0.ScreenMode = ScreenRefined.Mode.Run;
                        screen0.Dirty = true;
                        updateCounter = ShortPollPeriod;
                        break;
                    }
            }
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


        private void TestButtonState_ValueChanged(MySync<bool, SyncDirection.BothWays> obj)
        {
            containers = new ContainerInfo(this, 0);
            updateCounter = ShortPollPeriod;
            containers.ReFillInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid);
            long thenS = (DateTime.Now.Ticks / TimeSpan.TicksPerSecond) - 86400;
            Entity.Storage[LastTimeKey] = thenS.ToString();
        }

        private void SliderReserveUraniumServer_ValueChanged(MySync<int, SyncDirection.BothWays> obj)
        {
            if (Log.Debug) Log.Msg($"Server SliderReserveUranium={SliderReserveUranium.Value}");
            runState = RunState.Checking;
            updateCounter = ShortPollPeriod;
            Storage.ReserveUranium = SliderReserveUranium.Value;
            SaveToModStorage();
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
            if (Log.Debug) TestButtonState.ValueChanged -= TestButtonState_ValueChanged;
            SliderReserveUranium.ValueChanged -= SliderReserveUraniumServer_ValueChanged;
            myRefinedBlock.EnabledChanged -= MyRefinedBlock_EnabledChanged;
        }

        private bool Paused()
        {
            long nowS = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            offlineS = 0;

            LoadFromModStorage();
            if (Storage.LastTime != 0)
                offlineS = (int)Math.Min(settings.MaxOfflineHours * 3600, nowS - Storage.LastTime);
            Storage.LastTime = nowS;
            SaveToModStorage();


            if (Log.Debug) Log.Msg($"deltaTimeS = {offlineS}");

            return offlineS > settings.MinOfflineMins * 60;
        }

        private void SaveToModStorage()
        {
            try
            {
                Entity.Storage[LastTimeKey] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Storage));
            }
            catch (Exception e)
            {
                Log.Msg($"Error Saving ModStorage\n {e}");
            }
        }

        private void LoadFromModStorage()
        {
            try
            {
                Storage = new ModStorage();
                string tmp;
                if (Entity.Storage.TryGetValue(LastTimeKey, out tmp))
                {
                    Storage = MyAPIGateway.Utilities.SerializeFromBinary<ModStorage>(Convert.FromBase64String(tmp));
                }
                else
                {
                    Log.Msg($"Failed to load ModStorage");
                }
            }
            catch (Exception e)
            {
                Log.Msg($"Error loading ModStorage\n {e}");
                Storage = new ModStorage();
            }
            SliderReserveUranium.SetLocalValue(Storage.ReserveUranium);
        }

        // On Client


        internal void TestButtonToggle()
        {
            TestButtonState.Value = !TestButtonState.Value;
        }


        private void RefinedInventory_ContentsChanged(MyInventoryBase refinedInventory)
        {
            var scAmount = (float)refinedInventory.GetItemAmount(SCDefId);
            try
            {
                MyEntitySubpart subpart;
                if (Entity.TryGetSubpart("SpaceCredit", out subpart)) // subpart does not exist when block is in build stage
                {
                    subpart.Render.Visible = scAmount > 0.001;
                }
            }
            catch (Exception e)
            {
                Log.Msg(e.ToString());
            }
        }
    }
}