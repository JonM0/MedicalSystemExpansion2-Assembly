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

        public bool EverInstallableOn ( LimbConfiguration limb )
        {
            return this.InstallationDestinations.Contains( limb );
        }

        public IEnumerable<(ThingDef, LimbConfiguration)> StandardPartsForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) yield break;

            if ( !this.EverInstallableOn( limb ) )
            {
                Log.Error( "[MSE2] Tried to get standard parts of " + this.parentDef.defName + " for an incompatible part record (" + limb + ")" );
                yield break;
            }

            foreach ( LimbConfiguration lc in this.IgnoredSubparts.NullOrEmpty() ? limb.ChildLimbs : limb.ChildLimbs.Where( p => !this.IgnoredSubparts.Contains( p.PartDef ) ) )
            {
                // first standard child that can be installed on lc
                ThingDef thingDef = this.standardChildren
                    .Find( td =>
                        (td.GetCompProperties<CompProperties_IncludedChildParts>()?.InstallationDestinations ?? IncludedPartsUtilities.InstallationDestinations( td ))
                        .Contains( lc )
                        );
                if ( thingDef != null )
                {
                    yield return (thingDef, lc);
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
                        (td.GetCompProperties<CompProperties_IncludedChildParts>()?.InstallationDestinations ?? IncludedPartsUtilities.InstallationDestinations( td ))
                        .Contains( lc )
                        ) )
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<ThingDef> AllPartsForLimb ( LimbConfiguration limb )
        {
            foreach ( (ThingDef thingDef, LimbConfiguration childLimb) in this.StandardPartsForLimb( limb ) )
            {
                yield return thingDef;

                CompProperties_IncludedChildParts comp = thingDef.GetCompProperties<CompProperties_IncludedChildParts>();

                if ( comp != null )
                {
                    foreach ( ThingDef item in comp.AllPartsForLimb( childLimb ) )
                    {
                        yield return item;
                    }
                }
            }
        }

        private float MarketValueForConfiguration ( LimbConfiguration limb )
        {
            float value = this.parentDef.BaseMarketValue;

            foreach ( ThingDef part in this.AllPartsForLimb( limb ) )
            {
                value += part.BaseMarketValue;
            }

            return value;
        }

        public float AverageMarketValueForPawn ( Pawn pawn )
        {
            float value = 0;
            int count = 0;

            for ( int i = 0; i < this.InstallationDestinations.Count; i++ )
            {
                LimbConfiguration limb = this.InstallationDestinations[i];
                if ( limb.Bodies.Contains( pawn.RaceProps.body ) )
                {
                    count++;
                    value += this.MarketValueForConfiguration( limb );
                }
            }

            return count == 0 ? 0 : value / count;
        }

        [Unsaved]
        private float cachedAverageValue = -1;

        public float AverageValue
        {
            get
            {
                if ( this.cachedAverageValue == -1f )
                {
                    if ( this.InstallationDestinations == null )
                    {
                        Log.Error( "Tried to calculate min value before valid limbs were set. ThingDef: " + this.parentDef.defName );
                    }
                    else
                    {
                        this.cachedAverageValue = this.InstallationDestinations.Select( this.MarketValueForConfiguration ).Average();
                    }
                }

                return this.cachedAverageValue;
            }
        }

        [Unsaved]
        private ThingDef parentDef;

        [Unsaved]
        private LimbLabeler lazyLimbLabeller;

        internal LimbLabeler LimbLabeller => lazyLimbLabeller
            ?? (lazyLimbLabeller = new LimbLabeler( this.InstallationDestinations, this.IgnoredSubparts, (from s in IncludedPartsUtilities.SurgeryToInstall( parentDef )
                                                                                                          from u in s.AllRecipeUsers
                                                                                                          select u.race.body).Contains ));

        [Unsaved]
        private (bool valid, List<BodyPartDef> list) cachedIgnoredSubparts = (false, null);

        public List<BodyPartDef> IgnoredSubparts
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
        private List<LimbConfiguration> cachedInstallationDestinations;

        public List<LimbConfiguration> InstallationDestinations
        {
            get
            {
                if ( this.cachedInstallationDestinations == null )
                {
                    this.cachedInstallationDestinations = IncludedPartsUtilities.InstallationDestinations( this.parentDef ).Where( LimbIsCompatible ).ToList();
                }
                return this.cachedInstallationDestinations;
            }
        }

        // xml def fields

        public List<ThingDef> standardChildren;

        public List<ThingDef> alwaysInclude;
    }
}