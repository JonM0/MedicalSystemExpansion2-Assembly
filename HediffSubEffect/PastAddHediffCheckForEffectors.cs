using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using RimWorld;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch(typeof(HediffSet))]
    [HarmonyPatch(nameof(HediffSet.AddDirect))]
    internal static class PastAddHediffCheckForEffectors
    {

        [HarmonyPostfix]
        internal static void AddPotentialEffectors(Hediff hediff)
        {
            if(hediff.Part != null && hediff is Hediff_AddedPart)
            {
                var hediffList = hediff.pawn.health.hediffSet.hediffs;
                for ( int i = 0; i < hediffList.Count; i++ )
                {
                    var hd = hediffList[i];
                    hd.TryGetComp<HediffComp_ExtraDiffCreator>()?.Notify_AddedHediffAddedPart( hediff );
                }
            }
        }
    }
}
