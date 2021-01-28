using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using HarmonyLib;

using UnityEngine;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnCapacityUtility ) )]
    [HarmonyPatch( nameof( PawnCapacityUtility.CalculateLimbEfficiency ) )]
    internal static class LimbEfficiencyFix
    {
        // replace the inside of the foreach loop with a call to SingleLimbCalc

        /*
        public static float CalculateLimbEfficiency(HediffSet diffSet, BodyPartTagDef limbCoreTag, BodyPartTagDef limbSegmentTag, BodyPartTagDef limbDigitTag, float appendageWeight, out float functionalPercentage, List<PawnCapacityUtility.CapacityImpactor> impactors)
		{
			BodyDef body = diffSet.pawn.RaceProps.body;
			float num = 0f;
			int num2 = 0;
			int num3 = 0;
			Func<BodyPartRecord, float> <>9__0;
			foreach (BodyPartRecord part in body.GetPartsWithTag(limbCoreTag))
			{				
                // ++++++++++ INSERT CALL TO SingleLimbCalc

                // ========== REMOVE FROM HERE
                
                float a = PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, part, impactors);
				foreach (BodyPartRecord connectedPart in part.GetConnectedParts(limbSegmentTag))
				{
					a *= PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, connectedPart, impactors);
				}
				if (part.HasChildParts(limbDigitTag))
				{
					float a2 = a;
					float num4 = a;
					IEnumerable<BodyPartRecord> childParts = part.GetChildParts(limbDigitTag);
					Func<BodyPartRecord, float> selector;
					if ((selector = <>9__0) == null)
					{
						selector = (<>9__0 = ((BodyPartRecord digitPart) => PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, digitPart, impactors)));
					}
					a = Mathf.Lerp(a2, num4 * childParts.Average(selector), appendageWeight);
				}
				num += a;
				num2++;
				if (a > 0f)
				{
					num3++;
				}

                // ========== REMOVE TO HERE

			}
			if (num2 == 0)
			{
				functionalPercentage = 0f;
				return 0f;
			}
			functionalPercentage = (float)num3 / (float)num2;
			return num / (float)num2;
		}         
         */

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> ReplaceLimbCalculation ( IEnumerable<CodeInstruction> instructions )
        {
            List<CodeInstruction> instList = instructions.ToList();

            // find instruction before foreach loop starts
            int foreachPartSetIndex = instList.FindIndex( i =>
                i.opcode == OpCodes.Stloc_S &&
                (i.operand as LocalBuilder).LocalIndex == 5 &&
                (i.operand as LocalBuilder).LocalType == typeof( BodyPartRecord ) );
            if ( foreachPartSetIndex == -1 ) throw new ApplicationException( "[MSE2] could not locate foreachPartSetIndex." );
            int foreachStartInstructionIndex = foreachPartSetIndex + 1;

            // find where the foreach loop ends
            int foreachEndBranchIndex = instList.FindLastIndex( foreachPartSetIndex, i => i.opcode == OpCodes.Br );
            Label foreachEnd = (Label)instList[foreachEndBranchIndex].operand;
            int foreachEndInstructionIndex = instList.FindIndex( i => i.labels.Contains( foreachEnd ) );
            if ( foreachEndInstructionIndex == -1 ) throw new ApplicationException( "[MSE2] could not locate foreachEndInstructionIndex." );

            // remove unneeded instructions
            instList.RemoveRange( foreachStartInstructionIndex, foreachEndInstructionIndex - foreachStartInstructionIndex );

            // insert new function call
            instList.InsertRange( foreachStartInstructionIndex, SLCCall() );

            return instList;
        }

        private static IEnumerable<CodeInstruction> SLCCall ()
        {
            yield return new CodeInstruction( OpCodes.Ldloc_S, 5 ); // load limbCore
            yield return new CodeInstruction( OpCodes.Ldarg_0 ); // load diffSet
            yield return new CodeInstruction( OpCodes.Ldarg_2 ); // load limbSegmentTag
            yield return new CodeInstruction( OpCodes.Ldarg_3 ); // load limbDigitTag
            yield return new CodeInstruction( OpCodes.Ldarg_S, 4 ); // load appendageWeight
            yield return new CodeInstruction( OpCodes.Ldarg_S, 6 ); // load impactors
            yield return new CodeInstruction( OpCodes.Ldloca_S, 1 ); // load ref totLimbEff
            yield return new CodeInstruction( OpCodes.Ldloca_S, 2 ); // load ref totLimbs
            yield return new CodeInstruction( OpCodes.Ldloca_S, 3 ); // load ref functionalLimbs
            yield return new CodeInstruction( OpCodes.Call,  // call SingleLimbCalc
                typeof( LimbEfficiencyFix )
                    .GetMethod( nameof( SingleLimbCalc ), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static ) );
        }

        internal static void SingleLimbCalc ( BodyPartRecord limbCore, HediffSet diffSet, BodyPartTagDef limbSegmentTag, BodyPartTagDef limbDigitTag,
            float appendageWeight, List<PawnCapacityUtility.CapacityImpactor> impactors, ref float totLimbEff, ref int totLimbs, ref int functionalLimbs )
        {
            // segments
            List<BodyPartRecord> segments = new List<BodyPartRecord>
            {
                limbCore
            };
            segments.AddRange( limbCore.GetConnectedParts( limbSegmentTag ) );

            // remove parts to ignore
            segments.RemoveAll( diffSet.PartShouldBeIgnored );

            // segment calculations
            float limbEff = 1f;
            if ( segments.Count > 0 )
            {
                // geometric mean of segments
                for ( int i = 0; i < segments.Count; i++ )
                {
                    float segmentEff = PawnCapacityUtility.CalculatePartEfficiency( diffSet, segments[i], false, impactors );
                    limbEff *= segmentEff;
                }
                limbEff = Mathf.Pow( limbEff, 1f / segments.Count );
            }

            // digit calculations
            List<BodyPartRecord> digits = limbCore.GetChildParts( limbDigitTag ).ToList();
            digits.RemoveAll( diffSet.PartShouldBeIgnored );
            if ( digits.Count > 0 )
            {
                // avg of digit efficiency
                float digitsAvg = 0f;
                for ( int i = 0; i < digits.Count; i++ )
                {
                    digitsAvg += PawnCapacityUtility.CalculatePartEfficiency( diffSet, digits[i], false, impactors );
                }
                digitsAvg /= digits.Count;

                // Lerp it with the square root (should consider removing sqrt and reducing part efficiency of finger defs)
                limbEff *= Mathf.Lerp( 1, Mathf.Sqrt( digitsAvg ), appendageWeight );
            }

            // add limb stats to totals
            totLimbEff += limbEff;
            totLimbs++;
            if ( limbEff > 0f )
            {
                functionalLimbs++;
            }
        }
    }
}