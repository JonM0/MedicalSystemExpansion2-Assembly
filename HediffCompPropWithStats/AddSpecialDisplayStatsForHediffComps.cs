using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

using HarmonyLib;

namespace MSE2
{
    [HarmonyPatch( typeof( HediffDef ) )]
    [HarmonyPatch( nameof( HediffDef.SpecialDisplayStats ) )]
    internal static class AddSpecialDisplayStatsForHediffComps
    {
        [HarmonyPostfix]
        internal static void AddCompStats ( HediffDef __instance, ref IEnumerable<StatDrawEntry> __result, StatRequest req )
        {
            var comps = __instance.comps;
            if ( comps != null )
            {
                for ( int i = 0; i < comps.Count; i++ )
                {
                    if ( comps[i] is IHediffCompPropsWithStats compWithStats )
                    {
                        __result = __result.Concat( compWithStats.SpecialDisplayStats( req ) );
                    }
                }
            }
        }
    }
}