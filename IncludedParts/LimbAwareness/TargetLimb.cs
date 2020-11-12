using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

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

            if ( this.targetLimb == null )
            {
                yield return "[MSE2] targetLimb is null";
            }
            else if ( this.targetLimb.RecordExample == null )
            {
                yield return "[MSE2] targetLimb contains no records";
            }
        }

        /// <summary>
        /// Determines whether a <c>Thing</c> with the given <paramref name="comp"/> can be used as an ingredient
        /// </summary>
        public bool IsValidThingComp ( CompIncludedChildParts comp )
        {
            return comp.AllAlwaysIncludedPartsPresent && comp.CompatibleLimbs.Contains( this.targetLimb );
        }

        /// <summary>
        /// Determines whether a surgery with this <c>ModExtension</c> can operate on the given <paramref name="bodyPartRecord"/>
        /// </summary>
        public bool IsValidPart ( BodyPartRecord bodyPartRecord )
        {
            return this.targetLimb.Contains( bodyPartRecord );
        }

        public override string ToString ()
        {
            return "TargetLimb=" + this.targetLimb?.UniqueName;
        }
    }
}