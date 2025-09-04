using Sandbox.Definitions;
using System.Collections.Generic;
using VRage;
using VRage.Utils;

namespace Refined.Controller
{
    internal class RefineOre
    {
        private Dictionary<string, MyBlueprintDefinitionBase> oreToIngots = null;

        public RefineOre()
        {
            MyLog.Default.WriteLineIf(false, "RefineOre: Starting");
            oreToIngots = new Dictionary<string, MyBlueprintDefinitionBase>();
            MyBlueprintClassDefinition ingotBpClass = MyDefinitionManager.Static.GetBlueprintClass("Ingots");
            foreach (var bpc in ingotBpClass)
            {

                if (bpc.Prerequisites.Length == 0)
                {
                    MyLog.Default.WriteLineIf(false, $"RefineOre: {bpc.Id.SubtypeName} no prerequisites");
                    continue;
                }

                if (oreToIngots.ContainsKey(bpc.Prerequisites[0].Id.SubtypeName))
                    continue;
                MyLog.Default.WriteLineIf(false, $"RefineOre: Found {bpc.Prerequisites[0].Id.SubtypeName} " +
                    $"amountRatio={(float)bpc.Results[0].Amount / (float)bpc.Prerequisites[0].Amount} " +
                    $"buildTime={bpc.BaseProductionTimeInSeconds / (float)bpc.Prerequisites[0].Amount}");
                oreToIngots.Add(bpc.Prerequisites[0].Id.SubtypeName, bpc);
            }
        }

        public int OresLoaded()
        {
            return oreToIngots.Count;
        }

        public bool TryGetIngots(string oreName, out MyFixedPoint amount, out MyBlueprintDefinitionBase.Item[] ingots)
        {
            amount = 0;
            ingots = null;
            MyBlueprintDefinitionBase bpc;
            if (!oreToIngots.TryGetValue(oreName, out bpc))
            {
                return false;
            }
            amount = bpc.Prerequisites[0].Amount;
            ingots = bpc.Results;
            return true;
        }
    }
}
