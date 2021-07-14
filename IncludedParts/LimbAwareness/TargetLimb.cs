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
        [Unsaved]
        public readonly ProsthesisVersion targetLimb;

        public TargetLimb ( ProsthesisVersion targetLimb )
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
        }

        public override string ToString ()
        {
            return "TargetLimb=" + this.targetLimb?.Label;
        }
    }
}