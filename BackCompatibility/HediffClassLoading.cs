using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Verse;

using RimWorld;



namespace MSE2.BackCompatibility
{
    [HarmonyPatch( typeof( HediffSet ), nameof( HediffSet.ExposeData ) )]
    internal static class HediffClassLoading
    {
        // make sure that hediffs are the type required by the def, reinstall them if they are not

        [HarmonyPostfix]
        internal static void ReinstallWrongClassed ( HediffSet __instance )
        {
            if ( Scribe.mode == LoadSaveMode.ResolvingCrossRefs )
            {
                try
                {
                    var brokenHediffs = __instance.hediffs.FindAll( h => h.GetType() != h.def.hediffClass && h.def.hediffClass.GetConstructors().Any( c => c.GetParameters().EnumerableNullOrEmpty() ) );
                    if ( brokenHediffs.Count > 0 )
                    {
                        Log.Warning( "[MSE2] Reinstalling Hediffs with wrong type: " + string.Join( ", ", brokenHediffs.Select( h => string.Format( "{{0}, current={1}, def={2}}", h, h.GetType().FullName, h.def.hediffClass.FullName ) ) ) );
                        __instance.hediffs.RemoveAll( brokenHediffs.Contains );

                        __instance.DirtyCache();

                        foreach ( var bh in brokenHediffs )
                        {
                            try
                            {
                                __instance.AddDirect( HediffMaker.MakeHediff( bh.def, bh.pawn, bh.Part ) );
                            }
                            catch ( Exception ex )
                            {
                                Log.Error( string.Format( "[MSE2] Exception reinstalling hediff {0}: {1}", bh, ex ) );
                            }
                        }

                        __instance.DirtyCache();
                    }

                }
                catch ( Exception ex )
                {
                    Log.Error( "[MSE2] Exception reinstalling wrongly typed hediffs." + ex );
                }
            }
        }
    }
}
