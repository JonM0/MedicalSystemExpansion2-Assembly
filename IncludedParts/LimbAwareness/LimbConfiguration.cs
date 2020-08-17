using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MSE2
{
    public class LimbConfiguration
    {
        protected HashSet<BodyPartRecord> allRecords = new HashSet<BodyPartRecord>();
        public IReadOnlyCollection<BodyPartRecord> AllRecords => allRecords;

        protected LimbConfiguration ( BodyPartRecord bodyPartRecord )
        {
            this.TryAddRecord( bodyPartRecord );

            foreach ( var item in
                from body in DefDatabase<BodyDef>.AllDefs
                from bpr in body.AllParts
                where bpr.def == bodyPartRecord.def
                where !recordToLimb.ContainsKey( bpr )
                select bpr )
            {
                this.TryAddRecord( item );
            }

            id = this.CountSimilar();
            allLimbDefs.Add( this );

            this.lazyAllSegments = new Lazy<List<(BodyPartDef, int)>>(
                () => new List<(BodyPartDef, int)>(
                    from p in bodyPartRecord.AllChildParts().Prepend( bodyPartRecord )
                    group p by p.def into pc
                    select (pc.Key, pc.Count()) ) );
        }

        protected bool HasCompatibleStructure ( BodyPartRecord bodyPartRecord )
        {
            return allRecords.EnumerableNullOrEmpty() ||
            bodyPartRecord.HasSameStructure( allRecords.FirstOrDefault() );
        }

        protected void TryAddRecord ( BodyPartRecord recordToAdd )
        {
            if ( this.HasCompatibleStructure( recordToAdd ) )
            {
                if ( this.allRecords.Add( recordToAdd ) )
                {
                    recordToLimb.Add( recordToAdd, this );
                }
            }
        }

        public BodyPartDef PartDef => this.allRecords.FirstOrDefault()?.def;

        public IEnumerable<BodyDef> Bodies => this.allRecords.Select( r => r.body ).Distinct();

        public bool Contains ( BodyPartRecord bodyPartRecord )
        {
            return allRecords.Contains( bodyPartRecord );
        }

        private readonly int id = -1;

        public int CountSimilar ()
        {
            int res = 0;
            for ( int i = 0; i < allLimbDefs.Count; i++ )
            {
                if ( allLimbDefs[i].PartDef == this.PartDef ) res++;
            }
            return res;
        }

        public string UniqueName
        {
            get
            {
                return this.PartDef.defName + "_" + id;
            }
        }

        public BodyPartRecord RecordExample
        {
            get => this.allRecords.FirstOrDefault();
        }

        public readonly Lazy<List<(BodyPartDef, int)>> lazyAllSegments;

        public List<(BodyPartDef, int)> AllSegments
        {
            get
            {
                return lazyAllSegments.Value;
            }
        }

        public IEnumerable<LimbConfiguration> ChildLimbs
        {
            get
            {
                if ( this.allRecords.EnumerableNullOrEmpty() )
                {
                    Log.Error( "Tried to get Child limbs of incomplete limb configuration" );
                    return Enumerable.Empty<LimbConfiguration>();
                }
                else
                {
                    return from bpr in this.RecordExample.parts
                           select LimbConfigForBodyPartRecord( bpr );
                }
            }
        }

        protected static Dictionary<BodyPartRecord, LimbConfiguration> recordToLimb = new Dictionary<BodyPartRecord, LimbConfiguration>();
        protected static List<LimbConfiguration> allLimbDefs = new List<LimbConfiguration>();

        public static LimbConfiguration LimbConfigForBodyPartRecord ( BodyPartRecord bodyPartRecord )
        {
            if ( recordToLimb.TryGetValue( bodyPartRecord, out LimbConfiguration outVal ) )
            {
                return outVal;
            }
            else
            {
                return new LimbConfiguration( bodyPartRecord );
            }
        }

        public static IEnumerable<LimbConfiguration> LimbConfigsMatchingBodyAndPart ( BodyDef body, BodyPartDef partDef )
        {
            return from bpr in body.AllParts
                   where bpr.def == partDef
                   let lc = LimbConfigForBodyPartRecord( bpr )
                   group lc by lc into g
                   select g.Key;
        }

        static LimbConfiguration ()
        {
            if ( DefDatabase<BodyDef>.AllDefs.EnumerableNullOrEmpty() )
            {
                Log.Error( "[MSE2] Tried to use limbs before the BodyDef database was loaded." );
                return;
            }
        }
    }
}