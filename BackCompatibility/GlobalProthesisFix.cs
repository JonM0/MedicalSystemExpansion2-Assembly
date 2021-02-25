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
                    c += FixPawnModules( pawn );
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
                        c += FixPawnModules( pawn );
                        if ( c > 0 )
                        {
                            countFixedPawns++;
                            countFixedParts += c;
                        }
                    }
                }

                var result = "Global prosthesis fix operation complete: fixed " + countFixedParts + " parts in total across " + countFixedPawns + " pawns.";

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

        private static int FixPawnModules ( Pawn pawn )
        {
            int countFixedModules = 0;

            var brokenModules = pawn.health.hediffSet.hediffs.FindAll( h => h is Hediff_ModuleAdded m && m.ModuleHolder == null );

            foreach ( var module in brokenModules )
            {
                BodyPartRecord part = module.Part;
                pawn.health.RestorePart( part );

                if ( DefDatabase<RecipeDef>.AllDefsListForReading.FindAll( r => // out of all recipes
                    r.IsSurgery && r.addsHediff != null  // take installation surgeries
                    && r.addsHediff.HasComp( typeof( HediffComp_ModuleHolder ) )  // for parts that support modules
                    && r.AllRecipeUsers.Contains( pawn.def )  // installable on the pawn
                    && r.Worker.GetPartsToApplyOn( pawn, r ).Contains( part ) // in the correct part
                    ).TryMinBy( r => r.addsHediff.spawnThingOnRemoved?.BaseMarketValue, out var compatibleHolderSurgery ) ) // take the least valuable
                {
                    Log.Message( "Fixing " + module.Label + " on " + pawn.Name );

                    compatibleHolderSurgery.Worker.ApplyOnPawn( pawn, part, null, null, null );

                    pawn.health.hediffSet.AddDirect( module );

                    countFixedModules++;
                }
                else
                {
                    Log.Warning( "Could not find a holder part to fix " + module.Label + " on " + pawn.Name );

                    if ( module.def.spawnThingOnRemoved != null && pawn.Map != null && pawn.IsColonistPlayerControlled )
                    {
                        GenPlace.TryPlaceThing( ThingMaker.MakeThing( module.def.spawnThingOnRemoved ), pawn.Position, pawn.Map, ThingPlaceMode.Near );
                    }

                }

            }

            return countFixedModules;

        }
    }
}
