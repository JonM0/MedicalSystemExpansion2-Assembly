using RimWorld;
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

            installationDestinations = IncludedPartsUtilities.CachedInstallationDestinations( parentDef ).ToList();

            limbLabeller = new LimbLabeler( installationDestinations, (from s in IncludedPartsUtilities.SurgeryToInstall( parentDef )
                                                                       from u in s.AllRecipeUsers
                                                                       select u.race.body).Contains );
        }

        public override IEnumerable<string> ConfigErrors ( ThingDef parentDef )
        {
            foreach ( var entry in base.ConfigErrors( parentDef ) )
                yield return entry;

            if ( this.parentDef != parentDef )
            {
                yield return "ParentDefs do not match (should never happen wtf, did you manually call this function or ResolveReferences?)";
            }

            // warning for never installable
            if ( installationDestinations.NullOrEmpty() )
            {
                yield return parentDef.defName + " will never be installable anywhere";
            }

            // warning for empy comp
            if ( standardChildren.NullOrEmpty() )
            {
                yield return "CompIncludedChildParts on " + parentDef.defName + " has no children";
            }
        }

        public bool EverInstallableOn ( LimbConfiguration limb )
        {
            return installationDestinations.Contains( limb );
        }

        public IEnumerable<(ThingDef, LimbConfiguration)> StandardPartsForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) yield break;

            if ( !this.EverInstallableOn( limb ) )
            {
                Log.Error( "[MSE2] Tried to get standard parts of " + parentDef.defName + " for an incompatible part record (" + limb + ")" );
                yield break;
            }

            // standard limb parts
            List<BodyPartDef> ignoredParts = new List<BodyPartDef>(
                DefDatabase<HediffDef>.AllDefsListForReading.Find( h => h.spawnThingOnRemoved == this.parentDef )?.GetModExtension<IgnoreSubParts>()?.ignoredSubParts
                ?? Enumerable.Empty<BodyPartDef>() );

            foreach ( var lc in limb.ChildLimbs.Where( p => !ignoredParts.Contains( p.PartDef ) ) )
            {
                var thingDef = standardChildren.Find( td => IncludedPartsUtilities.CachedInstallationDestinations( td ).Contains( lc ) );
                if ( thingDef != null )
                {
                    yield return (thingDef, lc);
                }
                else
                {
                    Log.Error( "[MSE2] Could not find a standard child of " + parentDef.defName + " compatible with body part record " + lc );
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

        public string LabelNameForLimb ( LimbConfiguration limb )
        {
            return limbLabeller.GetLabelForLimb( limb );
        }

        public float AverageMarketValueForPawn(Pawn pawn)
        {
            float value = 0;
            int count = 0;

            for ( int i = 0; i < installationDestinations.Count; i++ )
            {
                var limb = installationDestinations[i];
                if(limb.Bodies.Contains(pawn.RaceProps.body))
                {
                    count++;
                    value += this.parentDef.BaseMarketValue;

                    foreach ( var part in AllPartsForLimb(limb) )
                    {
                        value += part.BaseMarketValue;
                    }
                }
            }

            return count == 0 ? 0 : value / count;

        }

        private ThingDef parentDef;

        private LimbLabeler limbLabeller;

        public List<LimbConfiguration> installationDestinations;

        public List<ThingDef> standardChildren;

        public List<ThingDef> alwaysInclude;
    }
}