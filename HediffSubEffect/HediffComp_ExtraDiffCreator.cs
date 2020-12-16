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

            if ( Scribe.mode == LoadSaveMode.LoadingVars )
            {
                if ( this.extraDiffs == null )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): HediffComp_ExtraDiffCreator.extraDiffs null after load", this.parent, this.Pawn ) );
                    this.extraDiffs = new List<HediffExtraDiff>();
                }
                if ( this.extraDiffs.RemoveAll( d => d == null ) != 0 )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): HediffComp_ExtraDiffCreator.extraDiffs null after load", this.parent, this.Pawn ) );
                }
                foreach ( HediffExtraDiff diff in this.extraDiffs )
                {
                    if ( diff.diffCreator != null )
                    {
                        Log.Error( string.Format( "[MSE2] On {0} ({1}): extraDiff {2} had a creator at loading", this.parent, this.Pawn, diff ) );
                    }
                    diff.diffCreator = this;
                }
            }
        }

        public override void CompPostMerged ( Hediff other )
        {
            base.CompPostMerged( other );
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
                if ( this.Props.maxDistance >= 0 && dist <= this.Props.maxDistance && this.ShouldAddExtraToPart( part ) )
                {
                    this.AddExtraToPart( part );
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
            tmpPartsDone = partsDone;
            tmpPartsDone.Clear();
            tmpPartsToDo = partsToDo;
            tmpPartsToDo.Clear();
        }

        private bool ShouldAddExtraToPart ( BodyPartRecord bodyPart )
        {
            return bodyPart != null
                && this.Pawn.health.hediffSet.hediffs.Find( h => h.Part == bodyPart && h is Hediff_AddedPart ) != null;
        }

        private void AddExtraToPart ( BodyPartRecord bodyPart )
        {
            HediffExtraDiff diff = (HediffExtraDiff)HediffMaker.MakeHediff( this.Props.extraDiffDef, this.Pawn, bodyPart );
            diff.diffCreator = this;
            this.Pawn.health.AddHediff( diff );
            this.extraDiffs.Add( diff );
        }
    }
}