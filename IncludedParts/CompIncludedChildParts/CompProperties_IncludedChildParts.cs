using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;

using RimWorld;

using Verse;

namespace MSE2
{
    public class CompProperties_IncludedChildParts : CompProperties
    {
        public CompProperties_IncludedChildParts ()
        {
            this.compClass = typeof( CompIncludedChildParts );
        }

        public override void ResolveReferences ( ThingDef parentDef )
        {
            base.ResolveReferences( parentDef );

            this.parentDef = parentDef;
        }

        public override IEnumerable<string> ConfigErrors ( ThingDef parentDef )
        {
            foreach ( string entry in base.ConfigErrors( parentDef ) )
                yield return entry;

            if ( this.parentDef != parentDef )
            {
                yield return "[MSE2] ParentDefs do not match (should never happen wtf, did you manually call ConfigErrors or ResolveReferences?)";
            }

            // warning for stack size
            if ( parentDef.stackLimit != 1 )
            {
                yield return "[MSE2] def must have stack limit of 1 to work properly";
            }

            // warning for empty comp
            if ( this.standardChildren.NullOrEmpty() )
            {
                yield return "[MSE2] CompProperties_IncludedChildParts has no children";
            }
        }

        public IEnumerable<(ThingDef, ProsthesisVersion)> StandardPartsForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) yield break;

            foreach ( LimbConfiguration lc in this.IgnoredSubparts.NullOrEmpty() ? limb.ChildLimbs : limb.ChildLimbs.Where( p => !this.IgnoredSubparts.Contains( p.PartDef ) ) )
            {
                // first standard child that can be installed on lc
                ThingDef thingDef = this.standardChildren
                    .Find( td =>
                        (td.GetCompProperties<CompProperties_IncludedChildParts>()?.SupportedLimbs ?? IncludedPartsUtilities.InstallationDestinations( td ))
                        .Contains( lc )
                        );
                if ( thingDef != null )
                {
                    var version = thingDef.GetCompProperties<CompProperties_IncludedChildParts>()?.SupportedVersions.Find( v => v.LimbConfigurations.Contains( lc ) );

                    yield return (thingDef, version);
                }
                else
                {
                    Log.Error( "[MSE2] Could not find a standard child of " + this.parentDef.defName + " compatible with body part record " + lc +
                        "\nIgnored parts: " + (this.IgnoredSubparts?.Select( p => p.defName ).ToCommaList() ?? "none") );
                }
            }

            // always included parts
            if ( this.alwaysInclude != null )
            {
                for ( int i = 0; i < this.alwaysInclude.Count; i++ )
                {
                    yield return (this.alwaysInclude[i], null);
                }
            }
        }

        private bool LimbIsCompatible ( LimbConfiguration limb )
        {
            if ( limb == null ) return true;

            foreach ( LimbConfiguration lc in this.IgnoredSubparts.NullOrEmpty() ? limb.ChildLimbs : limb.ChildLimbs.Where( p => !this.IgnoredSubparts.Contains( p.PartDef ) ) )
            {
                // there is no standard child that can be installed on lc
                if ( !this.standardChildren.Exists( td =>
                        (td.GetCompProperties<CompProperties_IncludedChildParts>()?.SupportedLimbs
                            ?? IncludedPartsUtilities.InstallationDestinations( td ))
                        .Contains( lc )
                        ) )
                {
                    return false;
                }
            }

            return true;
        }

        public List<string> GetRacesForVersion ( ProsthesisVersion version )
        {
            if ( version == null )
            {
                return null;
            }

            List<string> outList = new List<string>();

            List<ThingDef> pawns = DefDatabase<ThingDef>.AllDefsListForReading;
            for ( int i = 0; i < pawns.Count; i++ )
            {
                ThingDef pawnDef = pawns[i];
                BodyDef body = pawnDef.race?.body;
                if ( body != null && version.BodyDefs.Contains( body ) && this.CompatibleBodyDefs.Contains( body ) )
                {
                    // if is the only limb from this body
                    if ( !this.SupportedVersions.Except( version ).Any( l => l.BodyDefs.Contains( body ) ) )
                    {
                        outList.AddDistinct( pawnDef.label );
                    }
                    else
                    {
                        foreach ( var bodyPartDef in version.BodyPartDefs )
                        {
                            IEnumerable<string> recordUniqueNames = from bpr in version.LimbConfigurations.SelectMany( l => l.AllRecords ).Distinct()
                                                                    where bpr.body == body
                                                                    where bpr.def == bodyPartDef
                                                                    select bpr.Label.Replace( bpr.LabelShort, "" ).Trim();

                            string records = string.Join( ", ", recordUniqueNames );

                            outList.AddDistinct( string.Format( "{0} ({1} {2})", pawnDef.label, records, bodyPartDef.LabelShort ) );
                        }
                    }
                }
            }

            return outList;
        }

        private float MarketValueForVersion ( ProsthesisVersion version )
        {
            float value = this.parentDef.BaseMarketValue;

            foreach ( ThingDef part in version.AllParts )
            {
                value += part.BaseMarketValue;
            }

            return value;
        }

        public float AverageMarketValueForPawn ( Pawn pawn )
        {
            float value = 0;
            int count = 0;

            for ( int i = 0; i < this.SupportedVersions.Count; i++ )
            {
                ProsthesisVersion version = this.SupportedVersions[i];
                if ( version.LimbConfigurations.Exists( l => l.Bodies.Contains( pawn.RaceProps.body ) ) )
                {
                    count++;
                    value += this.MarketValueForVersion( version );
                }
            }

            return count == 0 ? 0 : value / count;
        }

        public float AverageValue
        {
            get
            {
                if ( this.cachedAverageValue == -1f )
                {
                    if ( this.lazySupportedVersions == null )
                    {
                        Log.Error( "[MSE2] Tried to calculate avg value before valid versions were set. ThingDef: " + this.parentDef.defName );
                    }
                    else
                    {
                        this.cachedAverageValue = this.SupportedVersions.Select( this.MarketValueForVersion ).Average();
                    }
                }

                return this.cachedAverageValue;
            }
        }
        [Unsaved]
        private float cachedAverageValue = -1;

        [Unsaved]
        private ThingDef parentDef;

        public HashSet<BodyDef> CompatibleBodyDefs => lazyCompatibleBodyDefs ?? (lazyCompatibleBodyDefs = (from s in IncludedPartsUtilities.SurgeryToInstall( parentDef )
                                                               from u in s.AllRecipeUsers
                                                               select u.race.body).ToHashSet());
        [Unsaved]
        private HashSet<BodyDef> lazyCompatibleBodyDefs;


        private List<BodyPartDef> IgnoredSubparts
        {
            get
            {
                if ( !this.cachedIgnoredSubparts.valid )
                {
                    if ( DefDatabase<HediffDef>.AllDefsListForReading.NullOrEmpty() )
                    {
                        throw new ApplicationException( "[MSE2] Tried to find IgnoredSubparts before DefDatabase was loaded." );
                    }
                    this.cachedIgnoredSubparts.list = DefDatabase<HediffDef>.AllDefsListForReading
                                                    .Find( h => h.spawnThingOnRemoved == this.parentDef )
                                                    ?.GetModExtension<IgnoreSubParts>()?.ignoredSubParts;

                    this.cachedIgnoredSubparts.valid = true;
                }
                return this.cachedIgnoredSubparts.list;
            }
        }
        [Unsaved]
        private (bool valid, List<BodyPartDef> list) cachedIgnoredSubparts = (false, null);


        public ProsthesisVersionSegment SegmentVersion => (ProsthesisVersionSegment)SupportedVersions.Find( v => v is ProsthesisVersionSegment );
        public List<ProsthesisVersion> SupportedVersions
        {
            get
            {
                if ( this.lazySupportedVersions == null )
                {
                    this.lazySupportedVersions = new List<ProsthesisVersion> { new ProsthesisVersionSegment( this ) };

                    foreach ( var limb in IncludedPartsUtilities.InstallationDestinations( this.parentDef ).Where( LimbIsCompatible ) )
                    {
                        bool merged = false;
                        for ( int i = 0; !merged && i < this.lazySupportedVersions.Count; i++ )
                        {
                            merged = this.lazySupportedVersions[i].TryAddLimbConfig( limb );
                        }

                        if ( !merged )
                        {
                            this.lazySupportedVersions.Add( new ProsthesisVersion( this, limb ) );
                        }
                    }
                }
                return this.lazySupportedVersions;
            }
        }
        [Unsaved]
        private List<ProsthesisVersion> lazySupportedVersions;

        private IEnumerable<LimbConfiguration> SupportedLimbs => this.SupportedVersions.SelectMany( v => v.LimbConfigurations );


        // xml def fields

        public List<ThingDef> standardChildren;

        public List<ThingDef> alwaysInclude;
    }
}