using System.Collections.Generic;
using System.Linq;

using RimWorld;

using Verse;

namespace MSE2
{
    // This class gets patched into Defs/RecipeDef[@Name="SurgeryInstallBodyPartArtificialBase"]/workerClass

    public class Recipe_InstallArtificialBodyPartWithChildren : Recipe_InstallArtificialBodyPart
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn ( Pawn pawn, RecipeDef recipe )
        {
            return MedicalRecipesUtility.GetFixedPartsToApplyOn( recipe, pawn, delegate ( BodyPartRecord record )
            {
                IEnumerable<Hediff> alreadyPresent = from x in pawn.health.hediffSet.hediffs
                                                     where x.Part == record
                                                     where x.def == recipe.addsHediff
                                                     select x;

                return
                    // hediff not already present
                    !alreadyPresent.Any()
                    // has something to attach to
                    && (record.parent == null || pawn.health.hediffSet.GetNotMissingParts( BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null ).Contains( record.parent ))
                    // is compatible with parent
                    && !recipe.HasRestrictionsForPart( record, pawn.health.hediffSet )
                    // part shouldn't be ignored
                    && !pawn.health.hediffSet.PartShouldBeIgnored( record );
            } );
        }

        public override void ConsumeIngredient ( Thing ingredient, RecipeDef recipe, Map map )
        {
            if ( ingredient.TryGetComp<CompIncludedChildParts>() == null )
            {
                base.ConsumeIngredient( ingredient, recipe, map );
            }
        }

        public override void ApplyOnPawn ( Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill )
        {
            if ( billDoer != null )
            {
                bool partIsClean = MedicalRecipesUtility.IsClean( pawn, part );
                bool isViolation = !PawnGenerator.IsBeingGenerated( pawn ) && this.IsViolationOnPawn( pawn, part, Faction.OfPlayer );

                if ( base.CheckSurgeryFail( billDoer, pawn, ingredients, part, bill ) )
                {
                    foreach ( var ingredient in ingredients )
                        if ( !ingredient.Destroyed ) ingredient.Destroy();
                    return;
                }
                TaleRecorder.RecordTale( TaleDefOf.DidSurgery, new object[] { billDoer, pawn } );
                MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts( pawn, part, billDoer.Position, billDoer.Map );
                if ( partIsClean && isViolation && part.def.spawnThingOnRemoved != null )
                {
                    ThoughtUtility.GiveThoughtsForPawnOrganHarvested( pawn );
                }
                if ( isViolation )
                {
                    base.ReportViolation( pawn, billDoer, pawn.FactionOrExtraMiniOrHomeFaction, -70, "GoodwillChangedReason_NeedlesslyInstalledWorseBodyPart".Translate( this.recipe.addsHediff.label ) );
                }
            }
            else if ( pawn.Map != null )
            {
                MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts( pawn, part, pawn.Position, pawn.Map );
            }
            else
            {
                pawn.health.RestorePart( part );
            }
            pawn.health.AddHediff( this.recipe.addsHediff, part );


            if ( !ingredients.NullOrEmpty() )
            {
                // iterate over non null CompIncludedChildParts in ingredients
                foreach ( CompIncludedChildParts compChildParts in
                    from x in ingredients
                    let comp = x.TryGetComp<CompIncludedChildParts>() // take the comp
                    where comp?.IncludedParts != null
                    select comp )
                {
                    compChildParts.RecursiveInstallation( pawn, part );
                }
            }
            else
            {
                foreach ( CompProperties_IncludedChildParts compProp in
                    (this.recipe.fixedIngredientFilter?.AllowedThingDefs ?? Enumerable.Empty<ThingDef>())
                    .Select( td => td.GetCompProperties<CompProperties_IncludedChildParts>() )
                    .Where( c => c != null ) )
                {
                    compProp.RecursiveDefInstallation( pawn, part );
                }
            }

            foreach ( var ingredient in ingredients )
                if ( !ingredient.Destroyed ) ingredient.Destroy();
        }
    }
}