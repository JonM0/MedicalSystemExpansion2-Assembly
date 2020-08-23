using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

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
                var compProp = __instance.thing.GetCompProperties<CompProperties_IncludedChildParts>();
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