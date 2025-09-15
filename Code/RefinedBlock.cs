using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Catopia.Refined
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), false, "LargeBlockRefined")]
    public class RefinedGameLogic : MyGameLogicComponent
    {
        public const int DefaultMinOffline = 30 * 60; //seconds
        public const int DefaultMaxOffline = 168 * 3600; //seconds
        public const int PollPeriod = 3; //4.8s

        private IMyCargoContainer myRefinedBlock;
        private Guid LastTimeKey = new Guid("0a1db65e-a169-4cf2-9a83-8903add9ca26");

        private int updateCounter = PollPeriod;

        private RefiningInfo refiningInfoI;
        private int offlineS;
        private ContainerInfo containers;

        private enum RunState
        {
            Stop,
            Monitoring,
            Detected,
            Processing
        }

        private RunState runState;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Log.Msg("Loaded...");
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!MyAPIGateway.Session.IsServer)
                return;

            refiningInfoI = RefiningInfo.Instance;

            runState = RunState.Monitoring;
            updateCounter = 0;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            var start = DateTime.Now;
            Log.Msg($"Runstate={runState}");

            switch (runState)
            {
                case RunState.Stop:
                    {
                        break; ;
                    }
                case RunState.Monitoring:
                    {
                        if (--updateCounter > 0)
                            return;
                        updateCounter = PollPeriod;

                        if (Paused())
                            runState = RunState.Detected;
                        break;
                    }
                case RunState.Detected:
                    {
                        Log.Msg("Detected...");

                        containers = new ContainerInfo(offlineS);

                        if (!containers.FindContainerInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid))
                        {
                            Log.Msg("Abandon Processing");
                            runState = RunState.Monitoring;

                        }
                        runState = RunState.Processing;
                        break;
                    }
                case RunState.Processing:
                    {
                        Log.Msg("Processing...");

                        if (!containers.RefineNext())
                            runState = RunState.Monitoring;
                        break;
                    }
            }


            Log.Msg($"Elapsed = {(DateTime.Now - start).TotalMilliseconds}");


        }

        bool oneTime = false;
        private bool Paused()
        {
            /*            offlineS = 1000;
                        if (oneTime)
                            return false;
                        oneTime = true;
                        return true;*/

            long nowS = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            offlineS = 0;
            string lastTimeStr;
            if (Entity.Storage.TryGetValue(LastTimeKey, out lastTimeStr))
            {
                long lastS = Convert.ToInt64(lastTimeStr);
                if (lastS != 0)
                    offlineS = (int)Math.Min(DefaultMaxOffline, nowS - lastS);

                Log.Msg($"nowS={nowS} lastS={lastS} deltaTimeS={offlineS}");
            }
            else
            {
                Log.Msg($"LastTimeKey not loaded {LastTimeKey}");
            }

            Entity.Storage[LastTimeKey] = nowS.ToString();

            Log.Msg($"deltaTimeS = {offlineS}");

            return offlineS > DefaultMinOffline;
        }

    }
}