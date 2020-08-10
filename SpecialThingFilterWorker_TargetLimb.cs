﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MSE2
{
    internal class SpecialThingFilterWorker_TargetLimb : SpecialThingFilterWorker
    {
        private LimbConfiguration target;

        public SpecialThingFilterWorker_TargetLimb ( LimbConfiguration target )
        {
            this.target = target;
        }

        public override bool AlwaysMatches ( ThingDef def )
        {
            return base.AlwaysMatches( def );
        }

        public override bool CanEverMatch ( ThingDef def )
        {
            return def.GetCompProperties<CompProperties_IncludedChildParts>() != null;
        }

        public override bool Matches ( Thing t )
        {
            return target != null && t.TryGetComp<CompIncludedChildParts>().TargetLimb == target;
        }
    }
}