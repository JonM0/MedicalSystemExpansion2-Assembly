using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using HarmonyLib;
using RimWorld;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( StockGeneratorUtility ) )]
    [HarmonyPatch( nameof( StockGeneratorUtility.TryMakeForStockSingle ) )]
    internal static class RandomInitializeOnStockGeneration
    {
        [HarmonyPostfix]
        internal static void RandInit ( Thing __result )
        {
            var comp = __result.TryGetComp<CompIncludedChildParts>();

            if ( comp != null )
            {
                comp.InitializeForLimb( comp.Props.installationDestinations.RandomElement() );
            }
        }
    }
}