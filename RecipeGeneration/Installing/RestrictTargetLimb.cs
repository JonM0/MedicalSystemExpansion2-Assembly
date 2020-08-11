using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2
{
    public class RestrictTargetLimb : DefModExtension
    {
        private LimbConfiguration targetLimb;

        public RestrictTargetLimb ( LimbConfiguration targetLimb )
        {
            this.targetLimb = targetLimb;
        }

        public override IEnumerable<string> ConfigErrors ()
        {
            if ( targetLimb == null )
            {
                yield return "targetLimb cannot be null";
            }
        }

        public bool IsValidThingComp ( CompIncludedChildParts comp )
        {
            return this.targetLimb == comp.TargetLimb;
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