using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    internal class HediffCompProperties_ExtraDiffCreator : HediffCompProperties
    {
        public HediffCompProperties_ExtraDiffCreator ()
        {
            this.compClass = typeof( HediffComp_ExtraDiffCreator );
        }

        public override IEnumerable<string> ConfigErrors ( HediffDef parentDef )
        {
            IEnumerable<string> errors = base.ConfigErrors( parentDef );

            if ( parentDef.hediffClass == typeof( HediffExtraDiff ) )
            {
                errors = errors.Append( "invalid recursion, HediffExtraDiff cannot have an HediffCompProperties_ExtraDiffCreator" );
            }

            if ( this.extraDiffDef == null )
            {
                errors = errors.Append( "<extraDiffDef> cannot be null" );
            }
            else if ( !typeof( HediffExtraDiff ).IsAssignableFrom( this.extraDiffDef.hediffClass ) )
            {
                errors = errors.Append( "<extraDiffDef> needs to have <hediffClass> MSE2.HediffExtraDiff or subclass" );
            }

            return errors;
        }

        public HediffDef extraDiffDef;

        public bool addToThisPart = false;

        public int maxDistance = -1; // negative means infinite
    }
}