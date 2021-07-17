using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace MSE2
{
    public class Recipe_InstallModule : RecipeWorker
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn ( Pawn pawn, RecipeDef recipe )
        {
            return
                // in the parts that the recipe can be applied to and that the pawn has a slot in
                (from slot in pawn.health.hediffSet.GetHediffs<Hediff_ModuleSlot>()
                 where recipe.appliedOnFixedBodyParts.Count == 0 || recipe.appliedOnFixedBodyParts.Contains( slot.Part.def )
                 select slot.Part)
                .Distinct();
        }

        public override void ApplyOnPawn ( Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill )
        {
            if ( billDoer != null )
            {
                bool isViolation = !PawnGenerator.IsBeingGenerated( pawn ) && this.IsViolationOnPawn( pawn, part, Faction.OfPlayer );

                TaleRecorder.RecordTale( TaleDefOf.DidSurgery, new object[]
                {
                    billDoer,
                    pawn
                } );

                if ( isViolation )
                {
                    base.ReportViolation( pawn, billDoer, pawn.FactionOrExtraMiniOrHomeFaction, -70, "GoodwillChangedReason_NeedlesslyInstalledWorseBodyPart".Translate( this.recipe.addsHediff.label ) );
                }
            }

            pawn.health.AddHediff( this.recipe.addsHediff, part );
        }
    }
}