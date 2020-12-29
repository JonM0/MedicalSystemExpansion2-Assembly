using System.Linq;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    // part is clean only if all its children are clean

    [HarmonyPatch( typeof( MedicalRecipesUtility ) )]
    [HarmonyPatch( nameof( MedicalRecipesUtility.IsClean ) )]
    public static class IsClean_Patch
    {
        [HarmonyPostfix]
        public static void PostFix ( ref bool __result, Pawn pawn, BodyPartRecord part )
        {
            __result = __result && CleanRecurse( pawn, part );
        }

        private static bool CleanRecurse ( Pawn pawn, BodyPartRecord part )
        {
            bool result = true;
            for ( int i = 0; result && i < part.parts.Count; i++ )
            {
                result = MedicalRecipesUtility.IsClean( pawn, part.parts[i] ) && CleanRecurse( pawn, part.parts[i] );
            }
            return result;
        }
    }
}