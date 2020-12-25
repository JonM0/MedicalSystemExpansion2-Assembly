using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

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
            ProsthesisVersion target = d.GetModExtension<TargetLimb>()?.targetLimb;

            if ( target != null )
            {
                foreach ( ThingDefCountClass thingDefCountClass in d.products )
                {
                    CompProperties_IncludedChildParts compProp = thingDefCountClass.thingDef.GetCompProperties<CompProperties_IncludedChildParts>();

                    if ( thingDefCountClass.count == 1 && compProp != null )
                    {
                        foreach ( var (includedPart, _) in target.Parts )
                        {
                            __result += includedPart.GetStatValueAbstract( StatDefOf.MarketValue );
                        }
                    }
                }
            }
        }
    }
}