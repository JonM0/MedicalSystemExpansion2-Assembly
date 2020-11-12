﻿using System.Collections.Generic;

using Verse;

namespace MSE2
{
    public class HediffCompProperties_ModuleHolder : HediffCompProperties
    {
        public HediffCompProperties_ModuleHolder ()
        {
            this.compClass = typeof( HediffComp_ModuleHolder );
        }

        public override IEnumerable<string> ConfigErrors ( HediffDef parentDef )
        {
            foreach ( string ce in base.ConfigErrors( parentDef ) ) yield return ce;

            if ( this.maxModules <= 0 )
            {
                cancraftsegment helper func, defdatabase null check yield return "[MSE2] Part has negative or no module slots";
            }

            yield break;
        }

        public int maxModules = 1;
    }
}