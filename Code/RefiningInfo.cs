using Sandbox.Definitions;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;


namespace Catopia.Refined
{
    internal class RefiningInfo
    {
        private static RefiningInfo instance;


        internal Dictionary<MyDefinitionId, OreToIngotInfo> OreToIngots;

        public struct ProcessOrderItem
        {
            public MyDefinitionId ItemId;
            public float VolumeRatio;
            public float ProductionTimeNorm;

            public ProcessOrderItem(OreToIngotInfo info)
            {
                ItemId = info.ItemId;
                VolumeRatio = info.IngotsVolume / (info.Volume * (float)info.Amount); //correct for prereq amount
                ProductionTimeNorm = info.ProductionTime;
            }
        }

        //private List<ProcessOrderItem> processOrder = new List<ProcessOrderItem>();
        internal List<MyDefinitionId> OrderedOreList { get; private set; }

        public RefiningInfo() { }

        public static RefiningInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RefiningInfo();
                    instance.Setup();
                }
                return instance;
            }
        }

        private void Setup()
        {
            Log.Msg("RefineInfo: Starting");

            OreToIngots = new Dictionary<MyDefinitionId, OreToIngotInfo>();
            OreToIngotInfo info;
            List<ProcessOrderItem> processOrder = new List<ProcessOrderItem>();

            MyBlueprintClassDefinition ingotBpClass = MyDefinitionManager.Static.GetBlueprintClass("Ingots");
            foreach (var bpc in ingotBpClass)
            {

                if (bpc.Prerequisites.Length == 0)
                {
                    Log.Msg($"RefineOre: {bpc.Id.SubtypeName} no prerequisites");
                    continue;
                }

                /*Log.Msg($"RefineInfo: Found {bpc.Prerequisites[0].Id.SubtypeName} " +
                $"amountRatio={(float)bpc.Results[0].Amount / (float)bpc.Prerequisites[0].Amount} " +
                $"buildTime={bpc.BaseProductionTimeInSeconds / (float)bpc.Prerequisites[0].Amount}");*/

                info = new OreToIngotInfo(bpc);
                OreToIngots.Add(bpc.Prerequisites[0].Id, info);
                processOrder.Add(new ProcessOrderItem(info));
            }
            // sort to reduce volume
            processOrder = processOrder.OrderBy(x => x.VolumeRatio).ThenByDescending(x => x.ProductionTimeNorm).ToList();

            OrderedOreList = new List<MyDefinitionId>();
            foreach (var item in processOrder)
            {
                //Log.Msg($"{item.ItemId} {item.VolumeRatio} {item.ProductionTimeNorm}");
                OrderedOreList.Add(item.ItemId);
            }
        }

    }
}
