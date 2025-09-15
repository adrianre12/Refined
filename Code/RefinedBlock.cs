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

        private bool run = false;
        private int updateCounter = PollPeriod;

        // private List<Ingame.MyInventoryItem> inventory = new List<Ingame.MyInventoryItem>();



        private RefiningInfo refiningInfoI;
        private ReactorInfo reactors = new ReactorInfo();
        private RefineryInfo refineries = new RefineryInfo();
        private ContainerInfo containers;



        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {

            myRefinedBlock = Entity as IMyCargoContainer;

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (Entity.Storage == null)
                Entity.Storage = new MyModStorageComponent();


            run = true;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Log.Msg("Loaded...");
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            refiningInfoI = RefiningInfo.Instance;
            refiningInfoI.NewOreOrderList();

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

            reactors.FindReactorInfo(myRefinedBlock.CubeGrid);

            if (reactors.AvaialbleUranium == 0)
            {
                Log.Msg($"Not enough reactor Uranium");
                return;
            }

            refineries.FindRefineriesInfo(myRefinedBlock.CubeGrid);

            if (refineries.TotalPower > reactors.MaxPower)
            {
                Log.Msg($"Not enough reactor power for refineries");
                return;
            }

            refineries.CalcRefinarySeconds(reactors.MWseconds);

            if (refineries.AvailableSeconds < 1)
            {
                Log.Msg($"Not enough refinary time");
                return;
            }

            containers = new ContainerInfo(refineries);

            containers.FindContainerInventories(myRefinedBlock.GetInventory(), myRefinedBlock.CubeGrid);



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

    }
}