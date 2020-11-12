using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( StockGeneratorUtility ) )]
    [HarmonyPatch( nameof( StockGeneratorUtility.TryMakeForStockSingle ) )]
    internal static class RandomInitializeOnStockGeneration
    {
        [HarmonyPostfix]
        internal static void RandInit ( Thing __result )
        {
            CompIncludedChildParts comp = __result.TryGetComp<CompIncludedChildParts>();

            if ( comp != null )
            {
                comp.InitializeForLimb( comp.Props.InstallationDestinations.RandomElement() );
            }
        }
    }
}