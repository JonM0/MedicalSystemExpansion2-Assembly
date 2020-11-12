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
    [HarmonyPatch( typeof( Bill ) )]
    [HarmonyPatch( nameof( Bill.IsFixedOrAllowedIngredient ) )]
    [HarmonyPatch( new Type[] { typeof( Thing ) } )]
    internal static class Bill_IsFixedOrAllowedIngredient
    {
        // this makes the ingredient check for bill limb aware

        [HarmonyPostfix]
        internal static void CheckForTargetLimb ( ref Bill __instance, ref bool __result, Thing thing )
        {
            if ( __result )
            {
                CompIncludedChildParts comp = thing.TryGetComp<CompIncludedChildParts>();
                TargetLimb recipeTargetLimb = __instance.recipe?.GetModExtension<TargetLimb>();

                __result = comp == null || recipeTargetLimb == null || recipeTargetLimb.IsValidThingComp( comp );
            }
        }
    }
}