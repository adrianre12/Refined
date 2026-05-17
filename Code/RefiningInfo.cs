using Sandbox.Definitions;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;


namespace Catopia.Refined
{
    internal class RefiningInfo
    {
        private static RefiningInfo instance;

        private HashSet<MyBlueprintClassDefinition> knownBpClasses = new HashSet<MyBlueprintClassDefinition>();

        internal Dictionary<MyDefinitionId, OreToIngotInfo> OreToIngots = new Dictionary<MyDefinitionId, OreToIngotInfo>();

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

        internal List<MyDefinitionId> OrderedOreList { get; private set; } = new List<MyDefinitionId>();

        public RefiningInfo() { }

        public static RefiningInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RefiningInfo();
                    //instance.Setup();
                }
                return instance;
            }
        }

        /*        private void Setup()
                {
                    Log.Msg("RefiningInfo: Starting");

                    OreToIngots.Clear();
                    OreToIngotInfo info;
                    List<ProcessOrderItem> processOrder = new List<ProcessOrderItem>();

                    var defs = MyDefinitionManager.Static.GetDefinitionsOfType<MyRefineryDefinition>();
                    foreach (var def in defs)
                    {
                        //if (def.Prerequisites[0].Id.TypeId.GetType() is MyObjectBuilder_Ore)
                        Log.Msg($"Def: {def} ");
                    }

                    MyBlueprintClassDefinition ingotBpClass = MyDefinitionManager.Static.GetBlueprintClass("Ingots");
                    foreach (var bpc in ingotBpClass)
                    {

                        if (bpc.Prerequisites.Length == 0)
                        {
                            Log.Msg($"RefineOre: {bpc.Id.SubtypeName} no prerequisites");
                            continue;
                        }

                        *//*Log.Msg($"RefineInfo: Found {bpc.Prerequisites[0].Id.SubtypeName} " +
                        $"amountRatio={(float)bpc.Results[0].Amount / (float)bpc.Prerequisites[0].Amount} " +
                        $"buildTime={bpc.BaseProductionTimeInSeconds / (float)bpc.Prerequisites[0].Amount}");*//*

                        info = new OreToIngotInfo(bpc);
                        OreToIngots.Add(bpc.Prerequisites[0].Id, info);
                        processOrder.Add(new ProcessOrderItem(info));
                    }
                    // sort to reduce volume
                    processOrder = processOrder.OrderBy(x => x.VolumeRatio).ThenByDescending(x => x.ProductionTimeNorm).ToList();

                    OrderedOreList = new List<MyDefinitionId>();
                    foreach (var item in processOrder)
                    {
                        Log.Msg($"{item.ItemId} {item.VolumeRatio} {item.ProductionTimeNorm}");
                        OrderedOreList.Add(item.ItemId);
                    }
                }*/

        internal void Reset()
        {
            knownBpClasses.Clear();
            OreToIngots.Clear();
            OrderedOreList.Clear();
        }

        internal void AddBlueprints(List<MyBlueprintClassDefinition> blueprintClasses)
        {
            foreach (MyBlueprintClassDefinition bpClass in blueprintClasses)
            {
                if (knownBpClasses.Contains(bpClass))
                {
                    if (Log.Debug) Log.Msg($"BlueprintClass {bpClass.DisplayNameText} already known");
                    continue;
                }
                knownBpClasses.Add(bpClass);

                if (Log.Debug) Log.Msg($"BlueprintClass {bpClass.DisplayNameText} adding");
                foreach (var bpd in bpClass)
                {
                    if (bpd.Prerequisites.Length == 0)
                    {
                        Log.Msg($"RefineOre: {bpd.Id.SubtypeName} no prerequisites");
                        continue;
                    }

                    if (OreToIngots.ContainsKey(bpd.Prerequisites[0].Id))
                    {
                        if (Log.Debug) Log.Msg($"OreToIngots already containes {bpd.Prerequisites[0].Id}");
                        continue;
                    }
                    OreToIngots.Add(bpd.Prerequisites[0].Id, new OreToIngotInfo(bpd));
                }

            }
        }

        internal void UpdateOrderedOreList()
        {
            List<ProcessOrderItem> processOrder = new List<ProcessOrderItem>();

            foreach (OreToIngotInfo info in OreToIngots.Values.ToList<OreToIngotInfo>())
            {
                processOrder.Add(new ProcessOrderItem(info));

            }
            // sort to reduce inventory volume
            processOrder = processOrder.OrderBy(x => x.VolumeRatio).ThenByDescending(x => x.ProductionTimeNorm).ToList();

            OrderedOreList = new List<MyDefinitionId>();
            foreach (var item in processOrder)
            {
                if (Log.Debug) Log.Msg($"{item.ItemId} {item.VolumeRatio} {item.ProductionTimeNorm}");
                OrderedOreList.Add(item.ItemId);
            }
        }
    }
}
