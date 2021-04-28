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
    [HarmonyPatch( typeof( HediffUtility ) )]
    [HarmonyPatch( nameof( HediffUtility.CountAddedAndImplantedParts ) )]
    internal static class ThoughtFixCountParts
    {
        // to fix thoughts and shit, only count limbs once
        // makes the function into this

        /*
        public static int CountAddedAndImplantedParts(this HediffSet hs)
		{
            int num = 0;
            List<Hediff> hediffs = hs.hediffs;
            for ( int i = 0; i < hediffs.Count; i++ )
            {
                if ( hediffs[i].def.countsAsAddedPartOrImplant && NullOrAncestorHasNoAddedParts(hs, hediffs[i].Part ) )
                {
                    num++;
                }
            }
            return num;
        }
        */

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            List<CodeInstruction> instrList = instructions.ToList();

            int brfalseIndex = instrList.FindIndex( i => i.opcode == OpCodes.Brfalse_S );
            if ( brfalseIndex < 0 ) throw new ApplicationException( "Could not find branch false operation" );
            CodeInstruction brfalse = instrList[brfalseIndex];

            instrList.InsertRange( brfalseIndex + 1, InstructionsToInsert( brfalse.operand ) );

            return instrList;
        }


        // code for:     && !hs.AncestorHasDirectlyAddedParts( hediffs[i].Part ) 
        private static IEnumerable<CodeInstruction> InstructionsToInsert ( object branchTarget )
        {
            yield return new CodeInstruction( OpCodes.Ldarg_0 );
            yield return new CodeInstruction( OpCodes.Ldloc_1 );
            yield return new CodeInstruction( OpCodes.Ldloc_2 );

            System.Reflection.MethodInfo listGetter = typeof( List<Hediff> ).GetMethod( "get_Item" );
            if ( listGetter == null ) throw new ApplicationException( "Could not find list getter" );
            yield return new CodeInstruction( OpCodes.Callvirt, listGetter );

            System.Reflection.MethodInfo partGetter = typeof( Hediff ).GetMethod( "get_Part" );
            if ( partGetter == null ) throw new ApplicationException( "Could not find hediff part getter" );
            yield return new CodeInstruction( OpCodes.Callvirt, partGetter );

            System.Reflection.MethodInfo ancHasPart = typeof( ThoughtFixCountParts ).GetMethod( nameof( NullOrAncestorHasNoAddedParts ), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static );
            if ( ancHasPart == null ) throw new ApplicationException( "Could not find ThoughtFixCountParts.NullOrAncestorHasNoAddedParts" );
            yield return new CodeInstruction( OpCodes.Call, ancHasPart );

            yield return new CodeInstruction( OpCodes.Brfalse, branchTarget );
        }

        private static bool NullOrAncestorHasNoAddedParts(HediffSet hediffSet, BodyPartRecord bodyPartRecord)
        {
            return bodyPartRecord == null || !hediffSet.AncestorHasDirectlyAddedParts( bodyPartRecord );
        }
    }
}