using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using HarmonyLib;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnCapacityUtility ) )]
    [HarmonyPatch( nameof( PawnCapacityUtility.CalculatePartEfficiency ) )]
    internal class CalculatePartEfficiency_Patch
    {
        // remove the copying of the efficiency of a parent added part

        [HarmonyTranspiler]
        [HarmonyPriority( Priority.Low )]
        public static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            // determine all instructions belonging to the first for

            Label l = (Label)instructions.FirstOrDefault( ( CodeInstruction i ) => i.opcode == OpCodes.Br_S ).operand; // target of the first branch in the first for loop

            int firstBranchJump = instructions.FirstIndexOf( ( CodeInstruction i ) => i.labels.Contains( l ) ); // the location of that instruction
            firstBranchJump += 3; // the for ends 3 instructions later

            // skip them
            return instructions.Skip( firstBranchJump );
        }

        // return parent if part should be ignored

        [HarmonyPrefix]
        public static bool Prefix ( ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts, List<PawnCapacityUtility.CapacityImpactor> impactors )
        {
            if ( diffSet.PartShouldBeIgnored( part ) )
            {
                // PartShouldBeIgnored returns false on null part.parent
                __result = PawnCapacityUtility.CalculatePartEfficiency( diffSet, part.parent, ignoreAddedParts, impactors );
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        public static void CheckForMultiplyByParent ( ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts, List<PawnCapacityUtility.CapacityImpactor> impactors )
        {
            if ( MultiplyByParent.anyExist && !ignoreAddedParts && part.parent != null )
            {
                var hd = diffSet.hediffs.Find( h => h.Part == part && h.def.HasModExtension<MultiplyByParent>() );
                if ( hd != null )
                {
                    __result *= PawnCapacityUtility.CalculatePartEfficiency( diffSet, part.parent, ignoreAddedParts, null );
                    if ( impactors != null && impactors.Find( i => (i as PawnCapacityUtility.CapacityImpactorHediff)?.hediff == hd ) == null )
                    {
                        impactors.Add( new PawnCapacityUtility.CapacityImpactorHediff
                        {
                            hediff = hd,
                        } );
                    }
                }
            }
        }
    }
}