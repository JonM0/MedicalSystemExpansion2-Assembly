using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MSE2
{
    /// <summary>
    /// Automatically added to hediffs at load time, takes info from the thingcomp
    /// </summary>
    internal class MSE_cachedStandardParents : DefModExtension
    {
        private readonly List<HediffDef> standardParents = new();

        public override IEnumerable<string> ConfigErrors ()
        {
            foreach ( string ce in base.ConfigErrors() ) yield return ce;

            if ( this.standardParents.NullOrEmpty() ) yield return "[MSE2] standardParents null or empty";
        }

        public void Add ( HediffDef parent )
        {
            this.standardParents.AddDistinct( parent );
        }

        public bool Contains ( HediffDef parent )
        {
            return this.standardParents.Contains( parent );
        }
    }
}