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
    [HarmonyPatch( nameof( ThingStuffPairWithQuality.MakeThing ) )]
    internal static class TSPWQMakeThing
    {
        [HarmonyPostfix]
        internal static void InitAverage ( Thing __result )
        {
            var comp = __result.TryGetComp<CompIncludedChildParts>();

            if ( comp != null )
            {
                comp.InitializeForLimb( comp.Props.installationDestinations.RandomElement() );
            }
        }
    }
}