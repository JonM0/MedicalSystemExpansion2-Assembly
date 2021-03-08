using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;

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

        public IEnumerable<string> PostLoadedConfigErrors ()
        {
            if ( this.SupportedLimbs.EnumerableNullOrEmpty() )
            {
                yield return " prosthesis is not installable anywhere.";
            }
        }

        public IEnumerable<(ThingDef, ProsthesisVersion)> StandardPartsForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) yield break;

            foreach ( LimbConfiguration lc in this.NotIgnoredChildLimbs( limb ) )
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

            foreach ( LimbConfiguration lc in this.NotIgnoredChildLimbs( limb ) )
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

        public bool IncompatibleLimbsReport ( StringBuilder stringBuilder )
        {
            var incompatibles = IncludedPartsUtilities.InstallationDestinations( this.parentDef ).Where( l => !this.LimbIsCompatible( l ) );

            if ( incompatibles.Any() )
            {
                var pawns = DefDatabase<ThingDef>.AllDefs.Where( t => t.race?.body != null ).ToList();

                stringBuilder.AppendLine().Append( ">" ).Append( this.parentDef.defName ).Append( ":" );

                foreach ( var incLimb in incompatibles )
                {
                    stringBuilder.AppendLine()
                        .Append( " - " )
                        .Append( incLimb.PartDef.defName ).AppendLine()
                        .Append( "    " )
                        .Append( pawns.Where( p => incLimb.Bodies.Contains( p.race.body ) ).Select( p => p.defName ).ToCommaList() ).AppendLine()
                        .Append( "    " )
                        .Append(
                            (this.NotIgnoredChildLimbs( incLimb ))
                            .Where( lc => !this.standardChildren.Exists( td =>
                                (td.GetCompProperties<CompProperties_IncludedChildParts>()?.SupportedLimbs
                                ?? IncludedPartsUtilities.InstallationDestinations( td )).Contains( lc ) ) )
                            .Select( lc => lc.PartDef.defName ).ToCommaList() ).AppendLine();
                }

                stringBuilder.AppendLine();

                return true;
            }

            return false;
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
                        this.cachedAverageValue = this.SupportedVersionsNoSegment.Select( this.MarketValueForVersion ).Average();
                        this.cachedAverageValue = GenMath.RoundTo( this.cachedAverageValue, 5 );
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
                                                                                                           select u.race?.body).Where( b => b != null ).ToHashSet());
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
                    if ( !IgnoreSubPartsUtilities.FinishedIgnoring )
                    {
                        throw new ApplicationException( "[MSE2] Tried to find IgnoredSubparts before auto ignorings were created." );
                    }

                    var hediffs = DefDatabase<HediffDef>.AllDefsListForReading.FindAll( h => h.spawnThingOnRemoved == this.parentDef );

                    if ( hediffs.Count == 0 )
                    {
                        throw new ApplicationException( string.Format( "[MSE2] Prosthesis ThingDef {0} has no corresponding HediffDef", this.parentDef?.defName ) );
                    }
                    else if ( hediffs.Count == 1 )
                    {
                        this.cachedIgnoredSubparts.list = hediffs[0]?.GetModExtension<IgnoreSubParts>()?.ignoredSubParts;
                    }
                    else
                    {
                        this.cachedIgnoredSubparts.list = hediffs
                                                        .Select( h => h?.GetModExtension<IgnoreSubParts>()?.ignoredSubParts )
                                                        .Where( l => l != null ).SelectMany( l => l ).ToList();
                    }

                    this.cachedIgnoredSubparts.valid = true;
                }
                return this.cachedIgnoredSubparts.list;
            }
        }
        [Unsaved]
        private (bool valid, List<BodyPartDef> list) cachedIgnoredSubparts = (false, null);

        private IEnumerable<LimbConfiguration> NotIgnoredChildLimbs ( LimbConfiguration limb ) =>
            this.IgnoredSubparts.NullOrEmpty()
            ? limb.ChildLimbs
            : limb.ChildLimbs.Where( p => !this.IgnoredSubparts.Contains( p.PartDef ) );

        public ProsthesisVersionSegment SegmentVersion => (ProsthesisVersionSegment)SupportedVersions.Find( v => v is ProsthesisVersionSegment );

        public IEnumerable<ProsthesisVersion> SupportedVersionsNoSegment => SupportedVersions.Where( v => !v.LimbConfigurations.NullOrEmpty() );

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

        public override string ToString ()
        {
            return (this.parentDef?.defName ?? "???") + "." + nameof( CompProperties_IncludedChildParts );
        }


        // xml def fields

        public List<ThingDef> standardChildren;

        public List<ThingDef> alwaysInclude;
    }
}