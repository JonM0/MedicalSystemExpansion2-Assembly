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
    [HarmonyPatch( typeof( Bill ) )]
    [HarmonyPatch( "IsFixedOrAllowedIngredient" )]
    [HarmonyPatch( new Type[] { typeof( Thing ) } )]
    internal static class Bill_IsFixedOrAllowedIngredient
    {
        [HarmonyPostfix]
        internal static void CheckForTargetLimb ( ref Bill __instance, ref bool __result, Thing thing )
        {
            var comp = thing.TryGetComp<CompIncludedChildParts>();
            var recipeTargetLimb = __instance.recipe?.GetModExtension<RestrictTargetLimb>();

            __result = __result
                && (comp == null || recipeTargetLimb == null || recipeTargetLimb.IsValidThingComp( comp ));
        }
    }
}