using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( MedicalRecipesUtility ) )]
    [HarmonyPatch( nameof( MedicalRecipesUtility.GetFixedPartsToApplyOn ) )]
    internal static class MedicalRecipesUtility_GetFixedPartsToApplyOn
    {
        [HarmonyPostfix]
        internal static void RemoveWrongTargetLimb ( ref IEnumerable<BodyPartRecord> __result, RecipeDef recipe )
        {
            var recipeTargetLimb = recipe.GetModExtension<RestrictTargetLimb>();

            if ( recipeTargetLimb != null )
            {
                __result = __result.Where( recipeTargetLimb.IsValidPart );
            }
        }
    }
}