using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using HarmonyLib;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnCapacityUtility ) )]
    [HarmonyPatch( nameof( PawnCapacityUtility.CalculatePartEfficiency ) )]
    internal static class CalculatePartEfficiency_Patch
    {
        // remove the copying of the efficiency of a parent added part

        /*
        public static float CalculatePartEfficiency(HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts = false, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
		{
            // ========== REMOVE FROM HERE

		    BodyPartRecord rec;
		    Func<Hediff_AddedPart, bool> <>9__0;
		    for (rec = part.parent; rec != null; rec = rec.parent)
		    {
			    if (diffSet.HasDirectlyAddedPartFor(rec))
			    {
				    IEnumerable<Hediff_AddedPart> hediffs = diffSet.GetHediffs<Hediff_AddedPart>();
				    Func<Hediff_AddedPart, bool> predicate;
				    if ((predicate = <>9__0) == null)
				    {
					    predicate = (<>9__0 = ((Hediff_AddedPart x) => x.Part == rec));
				    }
				    Hediff_AddedPart hediffAddedPart = hediffs.Where(predicate).First<Hediff_AddedPart>();
				    if (impactors != null)
				    {
					    impactors.Add(new PawnCapacityUtility.CapacityImpactorHediff
					    {
						    hediff = hediffAddedPart
					    });
				    }
				    return hediffAddedPart.def.addedPartProps.partEfficiency;
			    }
		    }
        
            // ========== REMOVE TO HERE

		    if (part.parent != null && diffSet.PartIsMissing(part.parent))
		    {
			    return 0f;
		    }         

            // .......... REST IS THE SAME   V    

         */


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
        public static bool ParentWhenShouldIgnore ( ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts, List<PawnCapacityUtility.CapacityImpactor> impactors )
        {
            if ( diffSet.PartShouldBeIgnored( part ) )
            {
                // PartShouldBeIgnored returns false on null part.parent
                __result = PawnCapacityUtility.CalculatePartEfficiency( diffSet, part.parent, ignoreAddedParts, impactors );
                return false;
            }
            return true;
        }

        // patch to make MultiplyByParent ModExtension work

        [HarmonyPostfix]
        public static void CheckForMultiplyByParent ( ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts, List<PawnCapacityUtility.CapacityImpactor> impactors )
        {
            if ( MultiplyByParent.anyExist && !ignoreAddedParts && part.parent != null )
            {
                Hediff hd = diffSet.hediffs.Find( h => h.Part == part && h.def.HasModExtension<MultiplyByParent>() );
                if ( hd != null )
                {
                    // multiply result by efficiency of parent part
                    float parentEff = PawnCapacityUtility.CalculatePartEfficiency( diffSet, part.parent, ignoreAddedParts, null );
                    __result *= parentEff;
                    // add current part as impactor if parent had different eff
                    if ( impactors != null && parentEff != 1f && impactors.Find( i => (i as PawnCapacityUtility.CapacityImpactorHediff)?.hediff == hd ) == null )
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