using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace MSE2.DebugTools
{
    public static class DebugOutputsProsthetics
    {
        private static string StandardChildren ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.standardChildren.Select( c => c.defName ).ToCommaList() ?? "";
        }

        private static string SegmentCostList ( ThingDef thingDef )
        {
            if ( thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.CanCraftSegment ?? true )
            {
                return thingDef.costList?.Select( c => c.thingDef.defName + " x" + c.count ).ToCommaList() ?? "NULL";
            }
            else
            {
                return "No segment recipe";
            }
        }

        private static float SegmentValue ( ThingDef thingDef )
        {
            return thingDef.BaseMarketValue;
        }

        [DebugOutput]
        public static void ProstheticSegmentCosts ()
        {
            IEnumerable<ThingDef> dataSources = (from d in DefDatabase<ThingDef>.AllDefs
                                                 where d.isTechHediff && !d.costList.NullOrEmpty()
                                                 select d);

            TableDataGetter<ThingDef>[] getters = new TableDataGetter<ThingDef>[]
            {
                new TableDataGetter<ThingDef>("defName", (ThingDef d) => d.defName),
                new TableDataGetter<ThingDef>("StandardChildren", StandardChildren),
                new TableDataGetter<ThingDef>("SegmentCostList", SegmentCostList),
                new TableDataGetter<ThingDef>("SegmentValue", SegmentValue),
            };

            DebugTables.MakeTablesDialog( dataSources, getters );
        }
    }
}