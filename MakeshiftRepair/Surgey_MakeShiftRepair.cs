using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2
{
    class Surgey_MakeShiftRepair : Recipe_Surgery
    {
        public override void ApplyOnPawn ( Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill )
        {
            if ( billDoer != null )
            {
                if ( CheckSurgeryFail( billDoer, pawn, ingredients, part, bill ) )
                {
                    return;
                }

                TaleRecorder.RecordTale( TaleDefOf.DidSurgery, billDoer, pawn );
            }

            pawn.health.RestorePart( part );
            pawn.health.AddHediff( recipe.addsHediff, part );
            foreach ( var p in part.AllChildParts() )
            {
                pawn.health.RestorePart( p );
                pawn.health.AddHediff( recipe.addsHediff, p );
            }
        }

        public override string GetLabelWhenUsedOn ( Pawn pawn, BodyPartRecord part )
        {
            return recipe.label + " " + pawn.health.hediffSet.hediffs.Find( h => h.Part == part.parent && h is Hediff_AddedPart ).Label;
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn ( Pawn pawn, RecipeDef recipe )
        {
            return from p in pawn.health.hediffSet.GetMissingPartsCommonAncestors()
                   where pawn.health.hediffSet.AncestorHasDirectlyAddedParts( p.Part )
                   select p.Part;
        }
    }
}
