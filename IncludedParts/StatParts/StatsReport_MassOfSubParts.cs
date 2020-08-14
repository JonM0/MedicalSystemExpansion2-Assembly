using RimWorld;
using UnityEngine;
using Verse;

namespace MSE2
{
    internal class StatsReport_MassOfSubParts : StatPart
    {
        // This stat part impacts mass adding the mass of included subparts

        public override string ExplanationPart ( StatRequest req )
        {
            if ( req.HasThing )
            {
                var comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null && comp.ValueOfChildParts != 0f )
                {
                    return "StatsReport_MassOfSubParts".Translate( comp.MassOfChildParts.ToStringMassOffset() );
                }
            }

            return null;
        }

        public override void TransformValue ( StatRequest req, ref float val )
        {
            if ( req.HasThing )
            {
                var comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null )
                {
                    val += comp.MassOfChildParts;
                }
            }
        }
    }
}