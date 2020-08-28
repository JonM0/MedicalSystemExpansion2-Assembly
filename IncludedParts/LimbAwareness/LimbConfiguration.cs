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

        protected LimbConfiguration ()
        {
        }

        private LimbConfiguration ( BodyPartRecord bodyPartRecord )
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
            allLimbConfigs.Add( this );
        }

        private bool HasCompatibleStructure ( BodyPartRecord bodyPartRecord )
        {
            return allRecords.EnumerableNullOrEmpty() ||
            bodyPartRecord.HasSameStructure( allRecords.FirstOrDefault() );
        }

        private void TryAddRecord ( BodyPartRecord recordToAdd )
        {
            if ( this.HasCompatibleStructure( recordToAdd ) )
            {
                if ( this.allRecords.Add( recordToAdd ) )
                {
                    recordToLimb.Add( recordToAdd, this );
                }
            }
        }

        public BodyPartDef PartDef => 
            this.allRecords.FirstOrDefault()?.def;

        public IEnumerable<BodyDef> Bodies => 
            this.allRecords.Select( r => r.body ).Distinct();

        public bool Contains ( BodyPartRecord bodyPartRecord )
        {
            return allRecords.Contains( bodyPartRecord );
        }

        public readonly int id = -1;

        public int CountSimilar ()
        {
            int res = 0;
            for ( int i = 0; i < allLimbConfigs.Count; i++ )
            {
                if ( allLimbConfigs[i].PartDef == this.PartDef ) res++;
            }
            return res;
        }

        public /*virtual*/ string UniqueName =>
            this.PartDef.defName + "_" + id;

        public BodyPartRecord RecordExample =>
            this.allRecords.FirstOrDefault();

        protected /*virtual*/ IEnumerable<(BodyPartDef, int)> CalculateAllSegments =>
            from p in this.RecordExample.AllChildParts().Prepend( this.RecordExample )
            group p by p.def into pc
            select (pc.Key, pc.Count());

        private List<(BodyPartDef, int)> cachedAllSegments;

        public List<(BodyPartDef, int)> AllSegments =>
            cachedAllSegments ?? (cachedAllSegments = this.CalculateAllSegments.ToList());

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
                    return this.RecordExample.parts.Select( LimbConfigForBodyPartRecord );
                }
            }
        }

        protected static Dictionary<BodyPartRecord, LimbConfiguration> recordToLimb = new Dictionary<BodyPartRecord, LimbConfiguration>();
        protected static List<LimbConfiguration> allLimbConfigs = new List<LimbConfiguration>();

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

        public override string ToString ()
        {
            return string.Format( "{0} ( parts: {1}; def: {2}; races: {3} )", this.UniqueName, this.allRecords.Count, this.PartDef.defName, string.Join( ", ", this.Bodies ) );
        }
    }
}