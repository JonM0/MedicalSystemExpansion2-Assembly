using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    internal class HediffComp_ExtraDiffCreator : HediffComp
    {
        private HediffCompProperties_ExtraDiffCreator Props => (HediffCompProperties_ExtraDiffCreator)this.props;

        public override void CompPostMake ()
        {
            base.CompPostMake();
            this.extraDiffs = new List<HediffExtraDiff>();
        }

        public override void CompExposeData ()
        {
            base.CompExposeData();
            Scribe_Collections.Look( ref this.extraDiffs, "extraDiffs", LookMode.Reference );

            if ( Scribe.mode == LoadSaveMode.ResolvingCrossRefs )
            {
                if ( this.extraDiffs == null )
                {
                    Log.Warning( string.Format( "[MSE2] On {0} ({1}): HediffComp_ExtraDiffCreator.extraDiffs null after load", this.parent, this.Pawn ) );
                    this.extraDiffs = new List<HediffExtraDiff>();
                    this.AddAllExtraDiffs();
                }
                if ( this.extraDiffs.RemoveAll( d => d == null ) != 0 )
                {
                    Log.Warning( string.Format( "[MSE2] On {0} ({1}): some effectors in extraDiffs were null after load", this.parent, this.Pawn ) );
                }
                foreach ( HediffExtraDiff diff in this.extraDiffs )
                {
                    if ( diff.diffCreator != null )
                    {
                        Log.Warning( string.Format( "[MSE2] On {0} ({1}): extraDiff {2} had a creator at loading", this.parent, this.Pawn, diff ) );
                    }
                    diff.diffCreator = this;
                }
            }
        }

        public override void CompPostMerged ( Hediff other )
        {
            throw new NotImplementedException();
        }

        public override void CompPostPostAdd ( DamageInfo? dinfo )
        {
            base.CompPostPostAdd( dinfo );
            this.AddAllExtraDiffs();
        }

        public override void CompPostPostRemoved ()
        {
            base.CompPostPostRemoved();
            foreach ( HediffExtraDiff diff in this.extraDiffs.ToList() )
            {
                this.Pawn.health.RemoveHediff( diff );
            }
        }

        internal List<HediffExtraDiff> extraDiffs;

        private static HashSet<BodyPartRecord> tmpPartsDone;
        private static Queue<(BodyPartRecord, int)> tmpPartsToDo;

        private void AddAllExtraDiffs ()
        {
            // acquire tmp data structures
            HashSet<BodyPartRecord> partsDone = tmpPartsDone ?? new HashSet<BodyPartRecord>();
            tmpPartsDone = null;
            Queue<(BodyPartRecord part, int distance)> partsToDo = tmpPartsToDo ?? new Queue<(BodyPartRecord, int)>();
            tmpPartsToDo = null;

            // enque parent part or its neighbours
            if ( this.Props.addToThisPart )
            {
                partsToDo.Enqueue( (this.parent.Part, 0) );
            }
            else
            {
                partsDone.Add( this.parent.Part );

                partsToDo.Enqueue( (this.parent.Part.parent, 1) );
                for ( int i = 0; i < this.parent.Part.parts.Count; i++ )
                {
                    partsToDo.Enqueue( (this.parent.Part.parts[i], 1) );
                }
            }

            // resolve recursion
            while ( partsToDo.Count > 0 )
            {
                (BodyPartRecord part, int dist) = partsToDo.Dequeue();
                if ( this.ShouldAddExtraToPart( part, dist ) )
                {
                    this.AddExtraToPart( part, dist );

                    partsDone.Add( part );
                    // enqueue relatives
                    if ( !partsDone.Contains( part.parent ) ) partsToDo.Enqueue( (part.parent, dist + 1) );
                    for ( int i = 0; i < part.parts.Count; i++ )
                    {
                        if ( !partsDone.Contains( part.parts[i] ) ) partsToDo.Enqueue( (part.parts[i], dist + 1) );
                    }
                }
            }

            // release data structures
            partsDone.Clear();
            tmpPartsDone = partsDone;
            partsToDo.Clear();
            tmpPartsToDo = partsToDo;
        }

        private bool ShouldAddExtraToPart ( BodyPartRecord bodyPart, int distance )
        {
            return bodyPart != null
                && (this.Props.maxDistance < 0 || distance <= this.Props.maxDistance)
                && this.Pawn.health.hediffSet.hediffs.Exists( h => h.Part == bodyPart && h is Hediff_AddedPart );
        }

        private void AddExtraToPart ( BodyPartRecord bodyPart, int distance )
        {
            HediffExtraDiff diff = (HediffExtraDiff)this.Pawn.health.AddHediff( this.Props.extraDiffDef, bodyPart );

            diff.diffCreator = this;
            diff.distance = distance;
            this.extraDiffs.Add( diff );
        }

        public void Notify_AddedHediffAddedPart ( Hediff hediff )
        {
            BodyPartRecord part = hediff.Part;

            // new part is directly under module
            if ( this.parent.Part == part.parent && this.ShouldAddExtraToPart( part, 1 ) )
            {
                this.AddExtraToPart( part, 1 );
            }
            else
            {
                // new part is under other extra part
                HediffExtraDiff extraOnParentPart = this.extraDiffs.Find( h => h.Part == part.parent );
                if ( extraOnParentPart != null && this.ShouldAddExtraToPart( part, extraOnParentPart.distance + 1 ) )
                {
                    this.AddExtraToPart( part, extraOnParentPart.distance + 1 );
                }
            }
        }
    }
}