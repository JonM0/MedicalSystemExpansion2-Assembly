using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal class CorrectHediffCostOnGenerationLambda
    {
        // makes pawn generation count hediff value correctly (using average of possibilities for pawn instead of just segment value)

        [HarmonyTargetMethod]
        internal static MethodInfo TargetMethod ()
        {
            return AccessTools.FindIncludingInnerTypes<MethodInfo>( typeof( PawnTechHediffsGenerator ),
                t => t.HasAttribute<CompilerGeneratedAttribute>() // in the compiler generated subclasses
                ? t.GetMethods( BindingFlags.NonPublic | BindingFlags.Instance ).SingleOrDefault( IsTargetMethod ) // take the method that matches what i want
                : null );
        }

        internal static FieldInfo PawnField ()
        {
            // return the field of name pawn in the compiler generated class for the lambda that i am patching
            return AccessTools.FindIncludingInnerTypes<FieldInfo>( typeof( PawnTechHediffsGenerator ),
                t =>
                    t.HasAttribute<CompilerGeneratedAttribute>() // in the compiler generated subclasses
                    && t.GetMethods( BindingFlags.NonPublic | BindingFlags.Instance ).Any( IsTargetMethod ) // has my method
                    ? t.GetField( "pawn" )
                    : null );
        }

        internal static bool IsTargetMethod ( MethodInfo m )
        {
            return m.Name.Contains( nameof( PawnTechHediffsGenerator.GenerateTechHediffsFor ) ) // that has GenerateTechHediffsFor in the name
                && m.GetParameters().Single().ParameterType == typeof( ThingDef ); // and takes a single thingdef as argument
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            foreach ( CodeInstruction instruction in instructions )
            {
                if ( instruction.Calls( AccessTools.PropertyGetter( typeof( ThingDef ), nameof( ThingDef.BaseMarketValue ) ) ) )
                {
                    yield return new CodeInstruction( OpCodes.Ldarg_0 );
                    yield return new CodeInstruction( OpCodes.Ldfld, PawnField() );
                    yield return new CodeInstruction( OpCodes.Call, AccessTools.Method( typeof( CorrectHediffCostOnGeneration ), nameof( CorrectHediffCostOnGeneration.AvgMarketValueForPawn ) ) );
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}