using System.Text;

using RimWorld;

using UnityEngine;

using Verse;

namespace MSE2
{
    internal class StatsReport_MassOfSubParts : StatPart
    {
        // This stat part impacts mass adding the mass of included subparts

        protected float MassOfChildParts ( CompIncludedChildParts comp )
        {
            System.Collections.Generic.List<Thing> list = comp.IncludedParts;
            float tot = 0;

            for ( int i = 0; i < list.Count; i++ )
            {
                tot += list[i].GetStatValue( StatDefOf.Mass );
            }

            return tot;
        }

        public override string ExplanationPart ( StatRequest req )
        {
            if ( req.HasThing )
            {
                CompIncludedChildParts comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null )
                {
                    System.Collections.Generic.List<Thing> list = comp.IncludedParts;
                    StringBuilder builder = new StringBuilder();

                    for ( int i = 0; i < list.Count; i++ )
                    {
                        Thing part = list[i];

                        builder.AppendLine( "StatsReport_MassOfSubParts".Translate( part.Label, part.GetStatValue( StatDefOf.Mass ).ToStringMassOffset() ) );
                    }

                    return builder.ToString();
                }
            }

            return null;
        }

        public override void TransformValue ( StatRequest req, ref float val )
        {
            if ( req.HasThing )
            {
                CompIncludedChildParts comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null )
                {
                    val += this.MassOfChildParts( comp );
                }
            }
        }
    }
}