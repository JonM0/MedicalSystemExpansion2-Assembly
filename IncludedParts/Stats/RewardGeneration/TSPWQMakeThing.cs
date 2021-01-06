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
    [HarmonyPatch( nameof( ThingStuffPairWithQuality.MakeThing ) )]
    internal static class TSPWQMakeThing
    {
        [HarmonyPostfix]
        internal static void InitAverage ( Thing __result )
        {
            CompIncludedChildParts comp = __result.TryGetComp<CompIncludedChildParts>();

            if ( comp != null )
            {
                comp.InitializeForVersion( comp.Props.SupportedVersionsNoSegment.RandomElementWithFallback(comp.Props.SegmentVersion) );
            }
        }
    }
}