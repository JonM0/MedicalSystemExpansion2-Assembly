using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using UnityEngine;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnCapacityUtility ) )]
    [HarmonyPatch( nameof( PawnCapacityUtility.CalculateLimbEfficiency ) )]
    internal class CalculateLimbEfficiency_Patch
    {
        // replace
        // should prob implement a transpiler

        [HarmonyPrefix]
        [HarmonyPriority( Priority.Last )]
        public static bool Replace ( ref float __result, HediffSet diffSet, BodyPartTagDef limbCoreTag, BodyPartTagDef limbSegmentTag, BodyPartTagDef limbDigitTag, float appendageWeight, out float functionalPercentage, List<PawnCapacityUtility.CapacityImpactor> impactors )
        {
            BodyDef body = diffSet.pawn.RaceProps.body;
            float totLimbEff = 0f;
            int totLimbs = 0;
            int functionalLimbs = 0;

            foreach ( BodyPartRecord limbCore in body.GetPartsWithTag( limbCoreTag ) )
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
                var digits = limbCore.GetChildParts( limbDigitTag ).ToList();
                digits.RemoveAll( diffSet.PartShouldBeIgnored );
                if ( digits.Count > 0 )
                {
                    // avg of digit efficiency
                    var digitsAvg = 0f;
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

            if ( totLimbs == 0 )
            {
                functionalPercentage = 0f;
                __result = 0f;
            }
            else
            {
                functionalPercentage = (float)functionalLimbs / (float)totLimbs;
                __result = totLimbEff / (float)totLimbs;
            }

            return false;
        }
    }
}