using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace MSE2
{
    public class Recipe_RemoveModules : RecipeWorker
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn ( Pawn pawn, RecipeDef recipe )
        {
            return from m in pawn.health.hediffSet.hediffs
                   where m is Hediff_ModuleAdded
                   group m by m.Part into g
                   select g.Key;
        }

        public override void ApplyOnPawn ( Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill )
        {
            if ( billDoer != null )
            {
                TaleRecorder.RecordTale( TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                } );
                if ( this.IsViolationOnPawn( pawn, part, Faction.OfPlayer ) )
                {
                    base.ReportViolation( pawn, billDoer, pawn.HomeFaction, -70 );
                }
            }

            foreach ( Hediff_ModuleAdded module in from x in pawn.health.hediffSet.hediffs
                                                   where x is Hediff_ModuleAdded && x.Part == part
                                                   select x )
            {
                // spawn thing if possible
                if ( module.def.spawnThingOnRemoved != null && pawn?.Map != null )
                {
                    GenPlace.TryPlaceThing( ThingMaker.MakeThing( module.def.spawnThingOnRemoved ), pawn.Position, pawn.Map, ThingPlaceMode.Near );
                }

                // remove hediff
                pawn.health.RemoveHediff( module );
            }
        }
    }
}