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

                foreach ( Pawn pawn in 
                    Find.WorldPawns.AllPawnsAlive // take world pawns
                    .Concat( Find.Maps.SelectMany(m => m.mapPawns.AllPawns) ) // and pawns from all maps
                    .Where( p => p != null ).ToArray() ) // that are not null
                {
                    int c = RestorePawnProstheses( pawn );
                    c += FixPawnModules( pawn );
                    if ( c > 0 )
                    {
                        countFixedPawns++;
                        countFixedParts += c;
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
            int countFixedParts = 0, tries = 0;

            HashSet<Hediff> unfixables = new HashSet<Hediff>();
            (HediffDef, BodyPartRecord) lastTried = (null, null);

            Hediff hediff = null;
            while (
                (hediff = pawn.health.hediffSet.hediffs.Find( h =>
                  h is Hediff_AddedPart // only fix added parts
                  && h.Part.parts.Any( p => pawn.health.hediffSet.PartIsMissing( p ) && !pawn.health.hediffSet.PartShouldBeIgnored( p )) // has subparts that are missing
                  && !unfixables.Contains( h ) // has not already failed an installation
                  && (h.def, h.Part) != lastTried ) // did not try this last time
                ) != null && tries++ < 100 )
            {
                RecipeDef recipeDef = DefDatabase<RecipeDef>.AllDefsListForReading.Find( r => r.IsSurgery && r.addsHediff == hediff.def );

                pawn.health.RestorePart( hediff.Part, checkStateChange: false );

                if ( recipeDef != null )
                {
                    Log.Message( "Fixing " + hediff.Label + " on " + pawn.Name );

                    recipeDef.Worker.ApplyOnPawn( pawn, hediff.Part, null, null, null );

                    countFixedParts++;
                }
                else
                {
                    Log.Warning( "Could not find a recipe to fix " + hediff.Label + " on " + pawn.Name );

                    unfixables.Add( hediff );
                }
                lastTried = (hediff.def, hediff.Part);
            }
            if ( tries > 100 )
            {
                Log.Warning( "Reached max fix attempts on " + pawn.Name );
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

                    pawn.health.RestorePart( part, checkStateChange: false );
                }

            }

            return countFixedModules;

        }
    }
}
