using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
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

            limbLabeller = new LimbLabeler( InstallationDestinations, IgnoredSubparts, (from s in IncludedPartsUtilities.SurgeryToInstall( parentDef )
                                                                                        from u in s.AllRecipeUsers
                                                                                        select u.race.body).Contains );
        }

        public override IEnumerable<string> ConfigErrors ( ThingDef parentDef )
        {
            foreach ( var entry in base.ConfigErrors( parentDef ) )
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

            // warning for never installable
            if ( InstallationDestinations.NullOrEmpty() )
            {
                yield return "[MSE2] will never be installable anywhere";
            }

            // warning for empy comp
            if ( standardChildren.NullOrEmpty() )
            {
                yield return "[MSE2] CompProperties_IncludedChildParts has no children";
            }
        }

        public bool EverInstallableOn ( LimbConfiguration limb )
        {
            return InstallationDestinations.Contains( limb );
        }

        public IEnumerable<(ThingDef, LimbConfiguration)> StandardPartsForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) yield break;

            if ( !this.EverInstallableOn( limb ) )
            {
                Log.Error( "[MSE2] Tried to get standard parts of " + parentDef.defName + " for an incompatible part record (" + limb + ")" );
                yield break;
            }

            foreach ( var lc in IgnoredSubparts.NullOrEmpty() ? limb.ChildLimbs : limb.ChildLimbs.Where( p => !IgnoredSubparts.Contains( p.PartDef ) ) )
            {
                // first standard child that can be installed on lc
                var thingDef = standardChildren
                    .Find( td =>
                        (td.GetCompProperties<CompProperties_IncludedChildParts>()?.InstallationDestinations ?? IncludedPartsUtilities.CachedInstallationDestinations( td ))
                        .Contains( lc )
                        );
                if ( thingDef != null )
                {
                    yield return (thingDef, lc);
                }
                else
                {
                    Log.Error( "[MSE2] Could not find a standard child of " + parentDef.defName + " compatible with body part record " + lc +
                        "\nIgnored parts: " + (IgnoredSubparts?.Select( p => p.defName ).ToCommaList() ?? "none") );
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

        public IEnumerable<ThingDef> AllPartsForLimb ( LimbConfiguration limb )
        {
            foreach ( (ThingDef thingDef, LimbConfiguration childLimb) in this.StandardPartsForLimb( limb ) )
            {
                yield return thingDef;

                var comp = thingDef.GetCompProperties<CompProperties_IncludedChildParts>();

                if ( comp != null )
                {
                    foreach ( var item in comp.AllPartsForLimb( childLimb ) )
                    {
                        yield return item;
                    }
                }
            }
        }

        public string LabelComparisonForLimb ( LimbConfiguration limb )
        {
            return limbLabeller.GetComparisonForLimb( limb );
        }

        public string GetCompatibilityReportDescription ( Predicate<LimbConfiguration> isCompatible )
        {
            return limbLabeller.GetCompatibilityReport( isCompatible );
        }

        private float MarketValueForConfiguration ( LimbConfiguration limb )
        {
            float value = this.parentDef.BaseMarketValue;

            foreach ( var part in AllPartsForLimb( limb ) )
            {
                value += part.BaseMarketValue;
            }

            return value;
        }

        public float AverageMarketValueForPawn ( Pawn pawn )
        {
            float value = 0;
            int count = 0;

            for ( int i = 0; i < InstallationDestinations.Count; i++ )
            {
                var limb = InstallationDestinations[i];
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
                if ( cachedAverageValue == -1f )
                {
                    if ( InstallationDestinations == null )
                    {
                        Log.Error( "Tried to calculate min value before valid limbs were set. ThingDef: " + this.parentDef.defName );
                    }
                    else
                    {
                        cachedAverageValue = InstallationDestinations.Select( MarketValueForConfiguration ).Average();
                    }
                }

                return cachedAverageValue;
            }
        }

        [Unsaved]
        private ThingDef parentDef;

        [Unsaved]
        private LimbLabeler limbLabeller;

        [Unsaved]
        private (bool valid, List<BodyPartDef> list) cachedIgnoredSubparts = (false, null);

        public List<BodyPartDef> IgnoredSubparts
        {
            get
            {
                if ( !cachedIgnoredSubparts.valid )
                {
                    if ( DefDatabase<HediffDef>.AllDefsListForReading.NullOrEmpty() )
                    {
                        throw new ApplicationException( "[MSE2] Tried to find IgnoredSubparts before DefDatabase was loaded." );
                    }
                    cachedIgnoredSubparts.list = DefDatabase<HediffDef>.AllDefsListForReading
                                                    .Find( h => h.spawnThingOnRemoved == this.parentDef )
                                                    ?.GetModExtension<IgnoreSubParts>()?.ignoredSubParts;

                    cachedIgnoredSubparts.valid = true;
                }
                return cachedIgnoredSubparts.list;
            }
        }

        [Unsaved]
        public List<LimbConfiguration> cachedInstallationDestinations;

        public List<LimbConfiguration> InstallationDestinations
        {
            get
            {
                if ( cachedInstallationDestinations == null )
                {
                    cachedInstallationDestinations = IncludedPartsUtilities.CachedInstallationDestinations( parentDef ).ToList();
                }
                return cachedInstallationDestinations;
            }
        }

        public enum CLType
        {
            Segment,
            Upgrade,
            FirstLimb,
        }

        public bool CanCraftSegment => costListType == CLType.Segment;

        // xml def fields

        public List<ThingDef> standardChildren;

        public List<ThingDef> alwaysInclude;

        public CLType costListType = CLType.Segment;
    }
}