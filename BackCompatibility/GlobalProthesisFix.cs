using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2.BackCompatibility
{
    public static class GlobalProthesisFix
    {
        public static void Apply ( bool messageResult = false )
        {
            try
            {
                Log.Message( "[MSE2] Starting global prosthesis fix operation" );

                int countFixedParts = 0;
                int countFixedPawns = 0;

                foreach ( Pawn pawn in Find.WorldPawns.AllPawnsAlive.Where( p => p != null ) )
                {
                    int c = RestorePawnProstheses( pawn );
                    if ( c > 0 )
                    {
                        countFixedPawns++;
                        countFixedParts += c;
                    }
                }
                foreach ( Map map in Find.Maps )
                {
                    foreach ( Pawn pawn in map.mapPawns.AllPawns.Where( p => p != null ) )
                    {
                        int c = RestorePawnProstheses( pawn );
                        if ( c > 0 )
                        {
                            countFixedPawns++;
                            countFixedParts += c;
                        }
                    }
                }

                var result = "Global prosthesis fix operation complete: fixed " + countFixedParts + " in " + countFixedPawns + " pawns.";

                Log.Message( result );
                if ( messageResult ) Messages.Message( new Message( result, MessageTypeDefOf.NeutralEvent ), false );
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Exception while applying global prosthesis fix operation: " + ex );
            }
        }

        private static int RestorePawnProstheses ( Pawn pawn )
        {
            int countFixedParts = 0;

            foreach ( Hediff hediff in pawn.health.hediffSet.GetHediffs<Hediff_AddedPart>() )
            {
                if ( hediff.Part.parts.Any( p => !pawn.health.hediffSet.HasDirectlyAddedPartFor( p ) && !pawn.health.hediffSet.PartShouldBeIgnored( p ) ) )
                {
                    RecipeDef recipeDef = DefDatabase<RecipeDef>.AllDefsListForReading.Find( r => r.IsSurgery && r.addsHediff == hediff.def );

                    if ( recipeDef != null )
                    {
                        Log.Message( "Fixing " + hediff.Label + " on " + pawn.Name );

                        BodyPartRecord part = hediff.Part;
                        pawn.health.RestorePart( part );

                        recipeDef.Worker.ApplyOnPawn( pawn, part, null, null, null );

                        countFixedParts++;
                    }
                    else
                    {
                        Log.Warning( "Could not find a recipe to fix " + hediff.Label + " on " + pawn.Name );
                    }
                }
            }

            return countFixedParts;
        }
    }
}
