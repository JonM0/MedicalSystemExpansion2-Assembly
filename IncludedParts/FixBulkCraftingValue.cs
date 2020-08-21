using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch( typeof( StatWorker_MarketValue ) )]
    [HarmonyPatch( nameof( StatWorker_MarketValue.CalculatedBaseMarketValue ) )]
    internal static class FixBulkCraftingValue
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            var instrList = instructions.ToList();

            var index = InsertionIndex( instrList );

            // remove the 1
            instrList.RemoveAt( index );
            // load buildabledef
            instrList.Insert( index,
                new CodeInstruction( OpCodes.Ldarg_0 ) );
            // call GetProductCount
            instrList.Insert( index + 1,
                new CodeInstruction( OpCodes.Call, AccessTools.Method( typeof( FixBulkCraftingValue ), nameof( FixBulkCraftingValue.GetProductCount ) ) ) );

            return instrList;
        }

        private static int InsertionIndex ( List<CodeInstruction> instructions )
        {
            bool lastInstLoaded1 = false;

            for ( int i = 0; i < instructions.Count; i++ )
            {
                var instruction = instructions[i];

                // if last pushed a 1 and current pops it into the loc3
                if ( lastInstLoaded1 && instruction.opcode == OpCodes.Stloc_3 )
                {
                    // return index before pushing the 1
                    return i - 1;
                }

                lastInstLoaded1 = instruction.opcode == OpCodes.Ldc_I4_1;
            }

            throw new ApplicationException( "MSE2 FixBulkCraftingValue failed to find an insertion index" );
        }

        private static int GetProductCount ( BuildableDef def )
        {
            return (def as ThingDef)?.recipeMaker?.productCount ?? 1;
        }
    }
}