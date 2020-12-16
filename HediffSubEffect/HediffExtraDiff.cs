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

        public override void PostRemoved ()
        {
            base.PostRemoved();
            diffCreator?.extraDiffs?.Remove( this );
        }

        public override void ExposeData ()
        {
            base.ExposeData();

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                if ( diffCreator == null )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): missing creator at loading, removing", this, this.pawn ) );
                    this.pawn.health.RemoveHediff( this );
                }
                else if ( !diffCreator.extraDiffs.Contains( this ) )
                {
                    Log.Error( string.Format( "[MSE2] On {0} ({1}): creator does not have this in extraDiffs at loading, should never happen wtf", this, this.pawn ) );
                    diffCreator.extraDiffs.Add( this );
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