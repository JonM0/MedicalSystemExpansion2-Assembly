﻿using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace MSE2
{
    internal class StatsReport_ValueOfSubParts : StatPart
    {
        // This stat part impacts market value adding the value of included subparts

        protected float ValueOfChildParts ( CompIncludedChildParts comp )
        {
            var list = comp.IncludedParts;
            float tot = 0;

            for ( int i = 0; i < list.Count; i++ )
            {
                tot += list[i].GetStatValue( StatDefOf.MarketValue );
            }

            return tot;
        }

        public override string ExplanationPart ( StatRequest req )
        {
            if ( req.HasThing )
            {
                var comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null )
                {
                    var list = comp.IncludedParts;
                    var builder = new StringBuilder();

                    for ( int i = 0; i < list.Count; i++ )
                    {
                        var part = list[i];

                        builder.AppendLine( "StatsReport_ValueOfSubParts".Translate( part.Label, part.GetStatValue( StatDefOf.MarketValue ).ToStringMoneyOffset() ) );
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
                var comp = req.Thing.TryGetComp<CompIncludedChildParts>();

                if ( comp != null )
                {
                    val += ValueOfChildParts( comp );
                }
            }
        }
    }
}