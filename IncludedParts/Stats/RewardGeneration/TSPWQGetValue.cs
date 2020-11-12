using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( ThingStuffPairWithQuality ) )]
    [HarmonyPatch( nameof( ThingStuffPairWithQuality.GetStatValue ) )]
    internal static class TSPWQGetValue
    {
        [HarmonyPrefix]
        internal static bool MaxOfConfiguration ( ref float __result, ref ThingStuffPairWithQuality __instance, StatDef stat )
        {
            if ( stat == StatDefOf.MarketValue )
            {
                CompProperties_IncludedChildParts compProp = __instance.thing.GetCompProperties<CompProperties_IncludedChildParts>();
                if ( compProp != null )
                {
                    __result = compProp.AverageValue;
                    return false;
                }
            }
            return true;
        }
    }
}