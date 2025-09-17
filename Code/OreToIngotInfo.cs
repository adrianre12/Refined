using Sandbox.Definitions;
using System.Collections.Generic;
using VRage;
using VRage.Game;

namespace Catopia.Refined
{
    internal class OreToIngotInfo
    {
        public struct ItemInfo
        {
            public MyDefinitionId ItemId;
            public MyFixedPoint Amount;

            public ItemInfo(MyBlueprintDefinitionBase.Item item)
            {
                ItemId = item.Id;
                Amount = item.Amount;
            }
        }

        public MyDefinitionId ItemId;
        public MyFixedPoint Amount;
        public float Volume;
        public List<ItemInfo> Ingots = new List<ItemInfo>();
        public float IngotsVolume;
        public float ProductionTime;

        public OreToIngotInfo(MyBlueprintDefinitionBase bluePrintClass)
        {
            if (bluePrintClass.Prerequisites.Length == 0)
            {
                Log.Msg($"OreToIngotInfo: {bluePrintClass.Id.SubtypeName} no prerequisites");
                return;
            }
            var prereq = bluePrintClass.Prerequisites[0];
            ItemId = prereq.Id;
            Amount = prereq.Amount;
            Volume = MyDefinitionManager.Static.GetPhysicalItemDefinition(prereq.Id).Volume;
            IngotsVolume = bluePrintClass.OutputVolume;
            ProductionTime = bluePrintClass.BaseProductionTimeInSeconds;

            foreach (var item in bluePrintClass.Results)
            {
                Ingots.Add(new ItemInfo(item));
            }
            if (Log.Debug) Log.Msg($"OreToIngotInfo: {bluePrintClass.Id.SubtypeName} Amount={Amount} Volume={Volume} IngotsVolume={IngotsVolume} ProductionTimeNorm={ProductionTime} Ingots.Count={Ingots.Count}");
        }


    }
}
