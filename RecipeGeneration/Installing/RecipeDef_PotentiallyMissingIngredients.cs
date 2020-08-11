﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using HarmonyLib;
using RimWorld;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Collections;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal static class RecipeDef_PotentiallyMissingIngredients
    {
        internal static MethodBase TargetMethod ()
        {
            // ty to LoonyLadle#7465, Garthor#8252 and erdelf#0001 for help with this
            return AccessTools.FirstInner( typeof( RecipeDef ), t => t.HasAttribute<CompilerGeneratedAttribute>() && t.Name.Contains( nameof( RecipeDef.PotentiallyMissingIngredients ) ) )
                .GetMethod( nameof( IEnumerator.MoveNext ), BindingFlags.NonPublic | BindingFlags.Instance );
        }

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> MissingIfWrongTargetLimb ( IEnumerable<CodeInstruction> instructions )
        {
            List<CodeInstruction> instrList = instructions.ToList();

            // where to insert the check
            int indexAt = InsertionIndex( instructions );

            // how to branch out after the check
            CodeInstruction lastBranchIfFalse = instrList.ElementAt( indexAt - 1 );

            if ( lastBranchIfFalse.opcode != OpCodes.Brfalse_S )
            {
                Log.Error( "MSE2 MissingIfWrongTargetLimb failed. lastBranchIfFalse: " + lastBranchIfFalse );
                return instrList;
            }

            instrList.InsertRange( indexAt, ExtraCheck().Append( new CodeInstruction( lastBranchIfFalse ) ) );

            return instrList;
        }

        private static int InsertionIndex ( IEnumerable<CodeInstruction> instructions )
        {
            int index = 0;
            bool lastInstLoadedFalse = false;

            foreach ( var instruction in instructions )
            {
                // if last pushed a false and current pops it into the flag
                if ( lastInstLoadedFalse && instruction.opcode == OpCodes.Stloc_3 )
                {
                    // return index before pushing the false
                    return index - 1;
                }

                lastInstLoadedFalse = instruction.opcode == OpCodes.Ldc_I4_1;

                index++;
            }

            Log.Error( "MSE2 MissingIfWrongTargetLimb failed to find an insertion index" );
            return index;
        }

        /// <summary>
        /// Leaves a bool on the stack that is the result of HasNoOrCorrectTargetLimb
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<CodeInstruction> ExtraCheck ()
        {
            // push this RecipeDef to the stack
            yield return new CodeInstruction( OpCodes.Ldloc_1 );
            // push the Thing to the stack
            yield return new CodeInstruction( OpCodes.Ldloc_S, 6 );
            // call HasNoOrCorrectTargetLimb and push result to stack
            yield return new CodeInstruction( OpCodes.Callvirt, typeof( RecipeDef_PotentiallyMissingIngredients ).GetMethod( "HasNoOrCorrectTargetLimb", BindingFlags.NonPublic | BindingFlags.Static ) );
        }

        internal static bool HasNoOrCorrectTargetLimb ( RecipeDef recipe, Thing thing )
        {
            var recipeTarget = recipe.GetModExtension<RestrictTargetLimb>();
            var thingComp = thing.TryGetComp<CompIncludedChildParts>();

            if ( recipeTarget != null && thingComp != null )
            {
                return recipeTarget.IsValidThingComp( thingComp );
            }

            return true;
        }
    }
}