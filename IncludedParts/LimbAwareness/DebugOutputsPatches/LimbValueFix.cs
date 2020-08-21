using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal static class LimbValueFix
    {
        [HarmonyTargetMethod]
        internal static MethodInfo TargetMethod ()
        {
            return AccessTools.Method( typeof( DebugOutputsEconomy ), "CheapestProductsValue" );
        }

        [HarmonyPostfix]
        internal static void AddSubpartValue ( ref float __result, RecipeDef d )
        {
            var target = d.GetModExtension<TargetLimb>()?.targetLimb;

            if ( target != null )
            {
                foreach ( ThingDefCountClass thingDefCountClass in d.products )
                {
                    var compProp = thingDefCountClass.thingDef.GetCompProperties<CompProperties_IncludedChildParts>();

                    if ( thingDefCountClass.count == 1 && compProp != null )
                    {
                        foreach ( var includedPart in compProp.AllPartsForLimb( target ) )
                        {
                            __result += includedPart.GetStatValueAbstract( StatDefOf.MarketValue );
                        }
                    }
                }
            }
        }
    }
}