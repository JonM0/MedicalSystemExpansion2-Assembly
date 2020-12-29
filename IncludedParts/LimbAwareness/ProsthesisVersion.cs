using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

using RimWorld;

namespace MSE2
{
    public class ProsthesisVersion
    {
        private CompProperties_IncludedChildParts compProp;

        public ProsthesisVersion ( CompProperties_IncludedChildParts compProp )
        {
            this.compProp = compProp;
        }

        public ProsthesisVersion ( CompProperties_IncludedChildParts compProp, LimbConfiguration limb ) : this( compProp )
        {
            this.TryAddLimbConfig( limb );
        }

        private string lazyLabel;
        public virtual string Label => lazyLabel ?? (lazyLabel = GenerateLabel().CapitalizeFirst());

        private List<(ThingDef, int)> VersionDifference ()
        {
            List<(ThingDef, int)> difference = new List<(ThingDef, int)>();

            for ( int i = 0; i < this.AllPartsCount.Count; i++ )
            {
                (ThingDef, int) item = this.AllPartsCount[i];

                if ( !this.compProp.SupportedVersions.Where( v => !(v is ProsthesisVersionSegment) ).All( v => v.AllPartsCount.Contains( item ) ) )
                {
                    difference.Add( item );
                }
            }

            return difference;
        }

        private bool IncludesOtherVersion ( ProsthesisVersion other )
        {
            return this == other 
                || other.AllPartsCount.TrueForAll( op => this.AllPartsCount.Find( p => p.part == op.part ).count >= op.count );
        }

        private string GenerateLabel ()
        {
            if ( this.compProp.SupportedVersions.Count == 2 )
            {
                return "LimbComplete".Translate();
            }

            var races = this.compProp.GetRacesForVersion( this );
            if ( races.Count == 1 )
            {
                return races[0];
            }

            if ( this.compProp.SupportedVersions.TrueForAll( this.IncludesOtherVersion ) )
            {
                return "LimbComplete".Translate();
            }

            var builder = new StringBuilder();

            List<(ThingDef, int)> diffLimbs = this.VersionDifference();
            for ( int i = 0; i < diffLimbs.Count; i++ )
            {
                (ThingDef part, int count) = diffLimbs[i];

                if ( count > 0 )
                {
                    builder.AppendWithComma( count.ToStringCached() );
                    builder.Append( " " );
                    if ( count > 1 )
                    {
                        builder.Append( Find.ActiveLanguageWorker.Pluralize( part.label ) );
                    }
                    else
                    {
                        builder.Append( part.label );
                    }
                }
            }
            if ( builder.Length > 0 )
            {
                return builder.ToString();
            }

            for ( int i = 0; i < this.AllPartsCount.Count; i++ )
            {
                (ThingDef part, int count) = this.AllPartsCount[i];

                if ( count > 0 )
                {
                    builder.AppendWithComma( count.ToStringCached() );
                    builder.Append( " " );
                    if ( count > 1 )
                    {
                        builder.Append( Find.ActiveLanguageWorker.Pluralize( part.label ) );
                    }
                    else
                    {
                        builder.Append( part.label );
                    }
                }
            }
            if ( builder.Length > 0 )
            {
                return builder.ToString();
            }

            return "LimbSimple".Translate();
        }


        public virtual bool TryAddLimbConfig ( LimbConfiguration limbConfiguration )
        {
            bool success = false;

            if ( this.limbConfigurations == null )
            {
                this.limbConfigurations = new List<LimbConfiguration> { limbConfiguration };

                this.parts = this.compProp.StandardPartsForLimb( limbConfiguration ).ToList();

                success = true;
            }
            else
            {
                if ( IncludedPartsUtilities.EnumerableEqualsOutOfOrder( this.parts, this.compProp.StandardPartsForLimb( limbConfiguration ).ToList(), ( a, b ) => a == b ) )
                {
                    this.limbConfigurations.AddDistinct( limbConfiguration );

                    success = true;
                }
            }

            if ( success )
            {
                this.lazyAllPartsCount = null;
                this.lazyLabel = null;
            }


            return success;
        }

        protected List<LimbConfiguration> limbConfigurations;
        public virtual List<LimbConfiguration> LimbConfigurations { get => this.limbConfigurations; }

        public IEnumerable<BodyPartDef> BodyPartDefs => LimbConfigurations.Select( l => l.PartDef );

        protected List<(ThingDef thingDef, ProsthesisVersion version)> parts;
        public virtual List<(ThingDef thingDef, ProsthesisVersion version)> Parts { get => this.parts; }

        public IEnumerable<ThingDef> AllParts
        {
            get
            {
                foreach ( var (thingDef, version) in this.Parts )
                {
                    yield return thingDef;

                    if ( version != null )
                    {
                        foreach ( ThingDef subpart in version.AllParts )
                        {
                            yield return subpart;
                        }
                    }
                }
            }
        }

        private List<(ThingDef part, int count)> lazyAllPartsCount;
        private List<(ThingDef part, int count)> AllPartsCount =>
            lazyAllPartsCount ?? (lazyAllPartsCount = this.AllParts.GroupBy( p => p ).Select( g => (g.Key, g.Count()) ).ToList());

        public IEnumerable<BodyDef> BodyDefs => LimbConfigurations.SelectMany( l => l.Bodies ).Where(this.compProp.CompatibleBodyDefs.Contains);
    }
}