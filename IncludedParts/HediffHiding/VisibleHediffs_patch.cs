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
    internal static class VisibleHediffs_patch
    {
        private static MethodBase TargetMethod ()
        {
            return AccessTools.Method( typeof( HealthCardUtility ), "VisibleHediffs" );
        }

        [HarmonyPostfix]
        private static void Postfix ( ref IEnumerable<Hediff> __result, Pawn pawn, bool ___showAllHediffs )
        {
            if ( !___showAllHediffs && MedicalSystemExpansion.Instance.HediffHideModeSetting != MedicalSystemExpansion.HediffHideMode.Never )
            {
                __result = from h in __result
                           where
                            h is not Hediff_AddedPart
                            || !h.IsParentStandard()
                            || (MedicalSystemExpansion.Instance.HediffHideModeSetting == MedicalSystemExpansion.HediffHideMode.Clean 
                                && pawn.health.hediffSet.hediffs.Where( x => x.Part == h.Part && x.Visible ).Except( h ).Any())
                           select h;
            }
        }
    }
}