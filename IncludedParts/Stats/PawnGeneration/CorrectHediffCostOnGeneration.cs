using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( PawnTechHediffsGenerator ) )]
    [HarmonyPatch( nameof( PawnTechHediffsGenerator.GenerateTechHediffsFor ) )]
    internal class CorrectHediffCostOnGeneration
    {
        // makes pawn generation count hediff value correctly (using average of possibilities for pawn instead of just segment value)

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            foreach ( CodeInstruction instruction in instructions )
            {
                if ( instruction.Calls( AccessTools.PropertyGetter( typeof( ThingDef ), nameof( ThingDef.BaseMarketValue ) ) ) )
                {
                    yield return new CodeInstruction( OpCodes.Ldarg_0 );
                    yield return new CodeInstruction( OpCodes.Call, AccessTools.Method( typeof( CorrectHediffCostOnGeneration ), nameof( CorrectHediffCostOnGeneration.AvgMarketValueForPawn ) ) );
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        internal static float AvgMarketValueForPawn ( ThingDef thingDef, Pawn pawn )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.AverageMarketValueForPawn( pawn ) ?? thingDef.BaseMarketValue;
        }
    }
}