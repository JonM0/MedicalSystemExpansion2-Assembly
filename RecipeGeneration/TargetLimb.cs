using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2
{
    public class TargetLimb : DefModExtension
    {
        public readonly LimbConfiguration targetLimb;

        public TargetLimb ( LimbConfiguration targetLimb )
        {
            this.targetLimb = targetLimb;
        }

        public override IEnumerable<string> ConfigErrors ()
        {
            foreach ( string error in base.ConfigErrors() ) yield return error;

            if ( targetLimb == null )
            {
                yield return "targetLimb is null";
            }
            else if ( targetLimb.RecordExample == null )
            {
                yield return "targetLimb contains no records";
            }
        }

        public bool IsValidThingComp ( CompIncludedChildParts comp )
        {
            return comp.CompatibleLimbs.Contains( this.targetLimb );
        }

        public bool IsValidPart ( BodyPartRecord bodyPartRecord )
        {
            return this.targetLimb.Contains( bodyPartRecord );
        }

        public override string ToString ()
        {
            return targetLimb.Label;
        }
    }
}