using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

namespace MSE2
{
    [HarmonyPatch(typeof(StatsReportUtility))]
    [HarmonyPatch( "FinalizeCachedDrawEntries" )]
    internal static class RemoveStatDrawDuplicates
    {
        // removes the duplicate entries caused by the multiple recipes to install the same hediff
        [HarmonyPostfix]
        internal static void RemoveDupes( List<StatDrawEntry> ___cachedDrawEntries )
        {
            int i = 1;
            while ( i < ___cachedDrawEntries.Count )
            {
                if(___cachedDrawEntries[i].LabelCap == ___cachedDrawEntries[i-1].LabelCap
                    && ___cachedDrawEntries[i].ValueString == ___cachedDrawEntries[i-1].ValueString
                    && ___cachedDrawEntries[i-1].ShouldDisplay
                    && ___cachedDrawEntries[i].stat == ___cachedDrawEntries[i-1].stat)
                {
                    ___cachedDrawEntries.RemoveAt( i );
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
