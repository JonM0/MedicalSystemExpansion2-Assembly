using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2.DebugTools
{
    public static class DebugOutputsProsthetics
    {
        private static string DefName ( ThingDef thingDef )
        {
            return thingDef.defName;
        }

        private static string StandardChildren ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.standardChildren.Select( c => c.defName ).ToCommaList() ?? "";
        }

        private static string SegmentCostList ( ThingDef thingDef )
        {
            return thingDef.costList?.Select( c => c.thingDef.defName + " x" + c.count ).ToCommaList() ?? "NULL";
        }

        private static float SegmentValue ( ThingDef thingDef )
        {
            return thingDef.BaseMarketValue;
        }

        private static float AverageChildSegmentValue ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.standardChildren.Select( t => t.BaseMarketValue ).Average() ?? -1;
        }

        private static float AvgConfigValue ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.AverageValue ?? -1;
        }

        private static float WorkToMake ( ThingDef thingDef )
        {
            return thingDef.GetStatValueAbstract( StatDefOf.WorkToMake );
        }

        private static float AvgChildWorkToMake ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.standardChildren.Select( t => t.GetStatValueAbstract( StatDefOf.WorkToMake ) ).Average() ?? -1;
        }

        [DebugOutput( "MSE2" )]
        public static void ProstheticCosts ()
        {
            IEnumerable<ThingDef> dataSources = (from d in DefDatabase<ThingDef>.AllDefs
                                                 where d.isTechHediff && !d.costList.NullOrEmpty()
                                                 select d);

            TableDataGetter<ThingDef>[] getters = new TableDataGetter<ThingDef>[]
            {
                new TableDataGetter<ThingDef>("defName", DefName),
                new TableDataGetter<ThingDef>("StandardChildren", StandardChildren),
                new TableDataGetter<ThingDef>("SegmentCostList", SegmentCostList),
                new TableDataGetter<ThingDef>("SegmentValue", SegmentValue),
            };

            DebugTables.MakeTablesDialog( dataSources, getters );
        }

        private static IEnumerable<ThingDef> AllProstheticsAndChildren ()
        {
            HashSet<ThingDef> allThings = new HashSet<ThingDef>( from d in DefDatabase<ThingDef>.AllDefs
                                                                 where d.isTechHediff && !d.costList.NullOrEmpty()
                                                                    && d.GetCompProperties<CompProperties_IncludedChildParts>() != null
                                                                 select d );
            Queue<ThingDef> Q = new Queue<ThingDef>( allThings );

            while ( Q.Any() )
            {
                CompProperties_IncludedChildParts comp = Q.Dequeue().GetCompProperties<CompProperties_IncludedChildParts>();

                if ( comp != null )
                {
                    foreach ( ThingDef child in comp.standardChildren )
                    {
                        if ( allThings.Add( child ) )
                        {
                            Q.Enqueue( child );
                        }
                    }

                    if ( comp.alwaysInclude != null )
                    {
                        foreach ( ThingDef child in comp.alwaysInclude )
                        {
                            if ( allThings.Add( child ) )
                            {
                                Q.Enqueue( child );
                            }
                        }
                    }
                }
            }

            return allThings;
        }

        [DebugOutput( "MSE2" )]
        public static void ProstheticSegmentValues ()
        {
            TableDataGetter<ThingDef>[] getters = new TableDataGetter<ThingDef>[]
            {
                new TableDataGetter<ThingDef>("defName", DefName),
                new TableDataGetter<ThingDef>("StandardChildren", StandardChildren),
                new TableDataGetter<ThingDef>("SegmentValue", SegmentValue),
                new TableDataGetter<ThingDef>("AverageChildSegmentValue", AverageChildSegmentValue),
                new TableDataGetter<ThingDef>("AvgConfigValue", AvgConfigValue),
                new TableDataGetter<ThingDef>("WorkToMake", WorkToMake),
                new TableDataGetter<ThingDef>("AvgChildWorkToMake", AvgChildWorkToMake),
            };

            DebugTables.MakeTablesDialog( AllProstheticsAndChildren(), getters );
        }
    }
}