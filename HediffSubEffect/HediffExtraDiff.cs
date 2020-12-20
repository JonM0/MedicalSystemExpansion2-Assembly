using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    public class HediffExtraDiff : HediffWithComps
    {
        internal HediffComp_ExtraDiffCreator diffCreator;
        internal int distance = -1;

        public override void PostRemoved ()
        {
            base.PostRemoved();
            this.diffCreator?.extraDiffs?.Remove( this );
        }

        public override void ExposeData ()
        {
            base.ExposeData();

            Scribe_Values.Look( ref this.distance, nameof( this.distance ), -1 );

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                if ( this.diffCreator == null )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): missing creator at loading, removing", this, this.pawn ) );
                    this.pawn.health.RemoveHediff( this );
                }
                else if ( !this.diffCreator.extraDiffs.Contains( this ) )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): creator does not have this in extraDiffs at loading, should never happen wtf", this, this.pawn ) );
                    this.diffCreator.extraDiffs.Add( this );
                }

                if ( this.distance == -1 )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): failed to load distance", this, this.pawn ) );
                }
            }
        }

        public override bool Visible => false;

        public override bool TryMergeWith ( Hediff other )
        {
            return false;
        }
    }
}