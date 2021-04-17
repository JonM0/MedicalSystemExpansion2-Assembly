using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal static class EmpireTitheGeneration
    {
        [HarmonyTargetMethods]
        internal static IEnumerable<MethodBase> TargetMethods ()
        {
            yield return AppDomain.CurrentDomain.GetAssemblies().Reverse()
                .Select( d => d.GetType( "FactionColonies.SettlementWindowFc" ) )
                .First( t => t != null ).GetMethod( "DrawProductionHeaderLower" );

            yield return AppDomain.CurrentDomain.GetAssemblies().Reverse()
                .Select( d => d.GetType( "FactionColonies.ResourceFC" ) )
                .First( t => t != null ).GetMethod( "returnLowestCost" );
        }

        [HarmonyPrepare]
        internal static bool Prepare( MethodBase original )
        {
            return ModLister.AllInstalledMods.Any( m => m.PackageId.EqualsIgnoreCase( "Saakra.Empire" ) && m.Active );
        }

        // replace cost for prosthetics with the average complete value

        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            foreach ( CodeInstruction instruction in instructions )
            {
                if ( instruction.Calls( AccessTools.PropertyGetter( typeof( ThingDef ), nameof( ThingDef.BaseMarketValue ) ) ) )
                {
                    yield return new CodeInstruction( OpCodes.Call, AccessTools.Method( typeof( EmpireTitheGeneration ), nameof( AvgMarketValue ) ) );
                }
                else
                {
                    yield return instruction;
                }
            }
        }
        internal static float AvgMarketValue ( ThingDef thingDef )
        {
            return thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.AverageValue ?? thingDef.BaseMarketValue;
        }
    }
}
