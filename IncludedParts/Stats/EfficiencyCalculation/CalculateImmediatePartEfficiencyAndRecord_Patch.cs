using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using HarmonyLib;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnCapacityUtility ) )]
    [HarmonyPatch( nameof( PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord ) )]
    internal static class CalculateImmediatePartEfficiencyAndRecord_Patch
    {
        // this function is never called anyway, since LimbEfficiencyFix removes it from the only function that called it before (PawnCapacityUtility.CalculateLimbEfficiency)

        // don't say it has some efficiency just because it is child of added part

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            List<CodeInstruction> l = new List<CodeInstruction>( instructions );

            return instructions.Skip( l.FindLastIndex( ( CodeInstruction i ) => i.opcode == OpCodes.Ldarg_0 ) ); // skip instructions before last loadarg0
        }

        // -- original function:

        /*
        public static float CalculateImmediatePartEfficiencyAndRecord ( HediffSet diffSet, BodyPartRecord part, List<PawnCapacityUtility.CapacityImpactor> impactors = null )
        {
            // ========== REMOVE FROM HERE

            if ( diffSet.AncestorHasDirectlyAddedParts( part ) )
            {
                return 1f;
            }

            // ========== REMOVE TO HERE

            return PawnCapacityUtility.CalculatePartEfficiency( diffSet, part, false, impactors );
        }
        */
    }
}