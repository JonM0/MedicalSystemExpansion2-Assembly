using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using RimWorld;

using UnityEngine;

using Verse;

namespace MSE2
{
    public partial class CompIncludedChildParts : ThingComp, IThingHolder
    {
        public override void Initialize ( CompProperties props )
        {
            base.Initialize( props );

            childPartsIncluded = new ThingOwner<Thing>( this );

            // Create the needed command gizmos
            this.command_SetTargetLimb = new Command_SetTargetLimb( this );
            this.command_AddExistingSubpart = new Command_AddExistingSubpart( this );
            this.command_SplitOffSubpart = new Command_SplitOffSubpart( this );
        }

        public CompProperties_IncludedChildParts Props => this.props as CompProperties_IncludedChildParts;

        // Save / Load
        public override void PostExposeData ()
        {
            base.PostExposeData();

            // Deep save the included Things
            Scribe_Deep.Look( ref this.childPartsIncluded, "childPartsIncluded", new object[] { this } );

            // save target version index
            Scribe_Values.Look( ref this.targetVersionIndex, "targetVersion", this.Props.SupportedVersions.IndexOf( this.Props.SegmentVersion ) );


            // compatibility with old version
            if ( this.IncludedParts == null && Scribe.mode == LoadSaveMode.LoadingVars )
            {
                List<Thing> oldThings = null;

                Scribe_Collections.Look( ref oldThings, "childPartsIncluded", LookMode.Deep );

                if ( oldThings != null )
                {
                    this.childPartsIncluded = new ThingOwner<Thing>( this );

                    foreach ( var thing in oldThings )
                    {
                        this.AddPart( thing );
                    }
                }
            }

            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                this.UpdateTargetLimbOrRemoveIncludedParts();

                // init the list if it comes up null (loading a map created without MSE2)
                if ( this.IncludedParts == null )
                {
                    this.childPartsIncluded = new ThingOwner<Thing>( this );
                    Log.Warning( "[MSE2] Included parts was null during loading." );
                }
            }
        }

        /// <summary>
        /// Resets the cache for MissingParts, MissingValue and strings
        /// </summary>
        private void DirtyCache ()
        {
            this.cachedCompatibleVersions.valid = false;
            this.cachedMissingParts.valid = false;
            this.cachedTransformLabelString = null;
            this.cachedInspectString = null;

            (this.ParentHolder as CompIncludedChildParts)?.DirtyCache();
        }

        public override bool AllowStackWith ( Thing other )
        {
            // disable stacking (would break everything)
            return false;
        }

        #region Gizmos

        // gizmos for merging and splitting

        private Command_SetTargetLimb command_SetTargetLimb;
        private Command_AddExistingSubpart command_AddExistingSubpart;
        private Command_SplitOffSubpart command_SplitOffSubpart;

        public override IEnumerable<Gizmo> CompGetGizmosExtra ()
        {
            yield return this.command_SetTargetLimb;
            yield return this.command_AddExistingSubpart;
            yield return this.command_SplitOffSubpart;

            if ( Prefs.DevMode )
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Make complete",
                    action = delegate ()
                    {
                        this.InitializeForVersion( this.TargetVersion );
                    }
                };
            }

            foreach ( Gizmo g in base.CompGetGizmosExtra() ) yield return g;

            yield break;
        }

        #endregion Gizmos

        #region Holder

        public void GetChildHolders ( List<IThingHolder> outChildren )
        {
            ThingOwnerUtility.AppendThingHoldersFromThings( outChildren, this.GetDirectlyHeldThings() );
        }

        public ThingOwner GetDirectlyHeldThings ()
        {
            return this.childPartsIncluded;
        }

        #endregion Holder

        #region PartHandling

        private readonly List<Thing> tmpThingList = new List<Thing>();

        #region Included parts

        private ThingOwner childPartsIncluded;

        public ThingOwner IncludedParts
        {
            get => this.childPartsIncluded;
        }

        public IEnumerable<CompIncludedChildParts> IncludedPartComps =>
            this.IncludedParts.Select( p => p.TryGetComp<CompIncludedChildParts>() ).Where( c => c != null );

        //private IEnumerable<(Thing, LimbConfiguration)> IncludedPartsVersions
        //{
        //    get
        //    {
        //        this.tmpThingList.Clear();
        //        this.tmpThingList.AddRange( this.IncludedParts );

        //        foreach ( (ThingDef def, LimbConfiguration limb) in this.Props.StandardPartsForLimb( this.TargetLimb ) )
        //        {
        //            Thing thing = this.tmpThingList.Find( t => t.def == def && (t.TryGetComp<CompIncludedChildParts>() == null || t.TryGetComp<CompIncludedChildParts>().TargetLimb == limb) );

        //            if ( thing != null )
        //            {
        //                this.tmpThingList.Remove( thing );
        //                yield return (thing, limb);
        //            }
        //        }
        //    }
        //}

        public void AddPart ( Thing part )
        {
            if ( !this.StandardParts.Contains( part.def ) && !this.Props.alwaysInclude.Contains( part.def ) )
            {
                Log.Error( "[MSE2] " + part.Label + " is not a valid subpart for " + this.parent.Label );
                return;
            }

            CompIncludedChildParts partComp = part.TryGetComp<CompIncludedChildParts>();

            // prioritize matches of both def and target part
            var target = this.MissingParts.Find( p => p.thingDef == part.def && (partComp == null || partComp.TargetVersion == p.limb) );
            // fallback to just matching the def
            if ( target.thingDef == null ) target = this.MissingParts.Find( p => p.thingDef == part.def );

            // found a match (part is actually missing)
            if ( target.thingDef != null )
            {
                this.IncludedParts.TryAdd( part.SplitOff( 1 ), false );
                this.DirtyCache();

                if ( partComp != null )
                {
                    partComp.TargetVersion = target.limb;
                }

                if ( part.Spawned )
                {
                    part.DeSpawn();
                }
            }
        }

        public void RemoveAndSpawnPart ( Thing part, IntVec3 position, Map map )
        {
            if ( this.IncludedParts.Remove( part ) )
            {
                this.DirtyCache();

                GenPlace.TryPlaceThing( part, position, map, ThingPlaceMode.Near );
            }
            else
            {
                Log.Error( "[MSE2] Tried to remove " + part.Label + " from " + this.parent.Label + " while it wasn't actually included." );
            }
        }
        public void RemoveAndSpawnPart ( Thing part )
        {
            this.RemoveAndSpawnPart( part, ThingOwnerUtility.GetRootPosition( this ), ThingOwnerUtility.GetRootMap( this ) );
        }

        #endregion Included parts

        #region Target Limb

        private int targetVersionIndex;

        public ProsthesisVersion TargetVersion
        {
            get => this.Props.SupportedVersions[targetVersionIndex];
            set
            {
                if ( this.TargetVersion != value )
                {
                    int newIndex = this.Props.SupportedVersions.IndexOf( value );

                    if ( newIndex == -1 )
                    {
                        Log.Error( string.Format( "[MSE2] Tried to set invalid target version ({0}) on {1}", value, this.parent.Label ) );
                    }
                    else
                    {
                        this.targetVersionIndex = newIndex;
                        this.UpdateTargetLimbOrRemoveIncludedParts();

                        this.DirtyCache();
                    }
                }
            }
        }

        private void UpdateTargetLimbOrRemoveIncludedParts ()
        {
            if ( this.TargetVersion == this.Props.SegmentVersion )
            {
                foreach ( CompIncludedChildParts comp in this.IncludedPartComps )
                {
                    comp.TargetVersion = comp.Props.SegmentVersion;
                }
            }
            else
            {
                this.tmpThingList.Clear();
                this.tmpThingList.AddRange( this.IncludedParts );

                // update compatible parts
                foreach ( var (thingDef, version) in this.TargetVersion.Parts )
                {
                    Thing candidate = this.tmpThingList.Find( t => t.def == thingDef );
                    if ( candidate != null )
                    {
                        this.tmpThingList.Remove( candidate );

                        CompIncludedChildParts potentialComp = candidate.TryGetComp<CompIncludedChildParts>();

                        if ( potentialComp != null )
                        {
                            potentialComp.TargetVersion = version;
                        }
                        else
                        {
                            // this will either never happen, or can be checked in standardpartsforlimb
                            if ( version != null )
                            {
                                Log.Error( string.Format( "[MSE2] Included thing {0} has no CompIncludedChildParts, but was assigned version {1}, which has {2} sub parts.", candidate.def.defName, version.Label, version.Parts.Count() ) );
                            }
                        }
                    }
                }

                // remove the others
                foreach ( Thing thing in this.tmpThingList )
                {
                    this.RemoveAndSpawnPart( thing );
                }
            }
        }

        #endregion Target Limb

        #region Standard parts

        public List<ThingDef> StandardParts => this.Props?.standardChildren;

        #endregion Standard parts

        #region Missing Parts

        private (List<(ThingDef thingDef, ProsthesisVersion limb)> list, bool valid) cachedMissingParts = (new List<(ThingDef, ProsthesisVersion)>(), false);

        public List<(ThingDef thingDef, ProsthesisVersion limb)> MissingParts
        {
            get
            {
                if ( !this.cachedMissingParts.valid )
                {
                    this.cachedMissingParts.list.Clear();

                    if ( this.TargetVersion != null )
                    {
                        this.cachedMissingParts.list.AddRange( this.TargetVersion.Parts );

                        foreach ( Thing thing in this.IncludedParts )
                        {
                            CompIncludedChildParts thingComp = thing.TryGetComp<CompIncludedChildParts>();

                            this.cachedMissingParts.list.Remove( this.cachedMissingParts.list.Find( c =>
                                 thing.def == c.thingDef
                                 && (thingComp == null || thingComp.TargetVersion == c.limb) ) );
                        }
                    }
                    this.cachedMissingParts.valid = true;
                }

                return this.cachedMissingParts.list;
            }
        }

        public bool IsComplete => this.MissingParts.Count == 0 && this.IncludedPartComps.All( c => c.IsComplete );

        public bool AllAlwaysIncludedPartsPresent => this.Props.alwaysInclude?.TrueForAll( p => this.IncludedParts.Any( t => t.def == p ) ) ?? true;

        #endregion Missing Parts

        #region Compatible limbs

        private (List<ProsthesisVersion> list, bool valid) cachedCompatibleVersions = (new List<ProsthesisVersion>(), false);

        public List<ProsthesisVersion> CompatibleVersions
        {
            get
            {
                if ( !this.cachedCompatibleVersions.valid )
                {
                    this.cachedCompatibleVersions.list.Clear();
                    this.cachedCompatibleVersions.list.AddRange( from v in this.Props.SupportedVersions
                                                                 where IncludedPartsUtilities.InstallationCompatibility(
                                                                     this.childPartsIncluded,
                                                                     v.Parts.Select( p => p.version ) )
                                                                 select v );
                    this.cachedCompatibleVersions.valid = true;
                }
                return this.cachedCompatibleVersions.list;
            }
        }

        #endregion Compatible limbs

        #region Creation / Deletion

        public void InitializeFromList ( List<Thing> available )
        {
            this.childPartsIncluded.Clear();
            this.DirtyCache();

            this.AddMissingFromList( available );
        }

        public void AddMissingFromList ( List<Thing> available )
        {
            this.tmpThingList.Clear();
            this.tmpThingList.AddRange( available );

            // first add things that match both def and target
            foreach ( Thing availableThing in available.ToArray() )
            {
                if ( this.MissingParts.Any( mp => mp.thingDef == availableThing.def && mp.limb == availableThing.TryGetComp<CompIncludedChildParts>()?.TargetVersion ) )
                {
                    this.AddPart( availableThing );
                    available.Remove( availableThing );
                }
            }

            this.tmpThingList.Clear();
            this.tmpThingList.AddRange( available );

            // then just match thingdef
            foreach ( Thing availableThing in available.ToArray() )
            {
                if ( this.MissingParts.Any( mp => mp.thingDef == availableThing.def ) )
                {
                    this.AddPart( availableThing );
                    available.Remove( availableThing );
                }
            }

            this.tmpThingList.Clear();
        }

        public void InitializeForVersion ( ProsthesisVersion version )
        {
            this.TargetVersion = version;

            this.childPartsIncluded.Clear();
            this.DirtyCache();

            foreach ( (ThingDef childDef, ProsthesisVersion bpr) in this.TargetVersion.Parts )
            {
                Thing child = ThingMaker.MakeThing( childDef );
                child.TryGetComp<CompIncludedChildParts>()?.InitializeForVersion( bpr );
                this.AddPart( child );
            }
        }

        //public void InitializeFromSimilar ( CompIncludedChildParts other )
        //{
        //    LimbConfiguration otherTarget = other.TargetLimb;

        //    // null target
        //    if ( otherTarget == null )
        //    {
        //        this.TargetLimb = null;
        //        return;
        //    }

        //    // same target if possible
        //    if ( this.Props.EverInstallableOn( otherTarget ) )
        //    {
        //        this.TargetLimb = otherTarget;

        //        // initialize for that target
        //        List<(ThingDef, LimbConfiguration)> otherMissing = other.MissingParts;
        //        List<(Thing, LimbConfiguration)> otherIncluded = other.IncludedPartsVersions.ToList();

        //        this.childPartsIncluded.Clear();
        //        this.DirtyCache();

        //        foreach ( (ThingDef childDef, LimbConfiguration bpr) in this.StandardPartsForTargetLimb() )
        //        {
        //            // add if not missing
        //            if ( !otherMissing.Remove( i => i.Item2 == bpr ) )
        //            {
        //                Thing child = ThingMaker.MakeThing( childDef );

        //                // if it is a limb segment, try to initialize based on similar or limb
        //                if ( bpr != null )
        //                {
        //                    CompIncludedChildParts childComp = child.TryGetComp<CompIncludedChildParts>();

        //                    // initialize the child comp
        //                    if ( childComp != null )
        //                    {
        //                        // find corresponding
        //                        (Thing, LimbConfiguration) otherChild = otherIncluded.Find( i => i.Item2 == bpr );
        //                        otherIncluded.Remove( otherChild );
        //                        CompIncludedChildParts otherComp = otherChild.Item1?.TryGetComp<CompIncludedChildParts>();

        //                        if ( otherComp != null )
        //                        {
        //                            childComp.InitializeFromSimilar( otherComp );
        //                        }
        //                        else
        //                        {
        //                            childComp.InitializeForLimb( bpr );
        //                        }
        //                    }
        //                }

        //                this.AddPart( child );
        //            }
        //        }
        //        return;
        //    }

        //    // target is parent of other
        //    LimbConfiguration goodTarget = this.Props.SupportedVersions.Find( l => l.ChildLimbs.Contains( otherTarget ) );
        //    if ( goodTarget != null )
        //    {
        //        this.childPartsIncluded.Clear();
        //        this.DirtyCache();

        //        this.TargetLimb = goodTarget;

        //        foreach ( (ThingDef childDef, LimbConfiguration bpr) in this.StandardPartsForTargetLimb() )
        //        {
        //            Thing child = ThingMaker.MakeThing( childDef );

        //            // try to initialize from other on correct child, else just initialize for bpr
        //            if ( bpr == otherTarget )
        //            {
        //                child.TryGetComp<CompIncludedChildParts>()?.InitializeFromSimilar( other );
        //            }
        //            else
        //            {
        //                child.TryGetComp<CompIncludedChildParts>()?.InitializeForLimb( bpr );
        //            }

        //            this.AddPart( child );
        //        }
        //    }
        //}

        public override void PostDestroy ( DestroyMode mode, Map previousMap )
        {
            base.PostDestroy( mode, previousMap );

            // destroy included child items
            this.IncludedParts.ClearAndDestroyContents();
        }

        #endregion Creation / Deletion

        #endregion PartHandling

        #region StatsDisplay

        // Label

        protected string cachedTransformLabelString = null;

        //public override string TransformLabel ( string label )
        //{
        //    if ( this.IncludedParts != null )
        //    {
        //        if ( this.cachedTransformLabelString == null )
        //        {
        //            this.cachedTransformLabelString = " (";

        //            if ( this.CompatibleLimbs.Any() )
        //            {
        //                this.cachedTransformLabelString += String.Join( "; ", this.CompatibleLimbs.Select( lc => lc.Label ) );
        //            }
        //            else
        //            {
        //                this.cachedTransformLabelString += "incomplete";
        //            }

        //            this.cachedTransformLabelString += ")";
        //        }

        //        return label + this.cachedTransformLabelString;
        //    }
        //    return null;
        //}

        // Inspect string

        protected string cachedInspectString = null;

        public override string CompInspectStringExtra ()
        {
            if ( this.IncludedParts != null )
            {
                if ( this.cachedInspectString == null )
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    stringBuilder.Append( "CompIncludedChildParts_InspectStringIncludes".Translate( this.IncludedParts.Count ) );

                    stringBuilder.AppendInNewLine( "CompIncludedChildParts_InspectStringTarget".Translate( this.TargetVersion.Label ) );
                    if ( this.AllMissingParts.Any() )
                    {
                        stringBuilder.Append( "CompIncludedChildParts_InspectStringMissing".Translate( this.AllMissingParts.Count() ) ); // maybe optimize
                    }

                    this.cachedInspectString = stringBuilder.ToString();
                }

                return this.cachedInspectString;
            }
            return null;
        }

        // Stat entries
        private StatDrawEntry IncluededPartsStat => new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "CompIncludedChildParts_StatIncludedParts_Label".Translate(),
            this.IncludedParts.Count.ToString(),
            "CompIncludedChildParts_StatIncludedParts_Description".Translate(),
            2502,
            null,
            this.IncludedParts.Select( p => new Dialog_InfoCard.Hyperlink( p ) ),
            false );

        private StatDrawEntry CompatibilityStat => new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "CompIncludedChildParts_StatCompatibility_Label".Translate(),
            "CompIncludedChildParts_StatCompatibility_Value".Translate( this.CompatibleVersions.Count - 1, this.Props.SupportedVersions.Count - 1 ),
            this.GetCompatibilityReport(),
            2500 );

        private StatDrawEntry MissingPartsStat => new StatDrawEntry(
            StatCategoryDefOf.Basics,
            "CompIncludedChildParts_StatMissing_Label".Translate(),
            this.AllMissingParts.Count().ToString(),
            this.AllMissingParts.Any() ? "CompIncludedChildParts_StatMissing_Description".Translate() : "CompIncludedChildParts_StatMissing_Desc_None".Translate(),
            2501,
            null,
            this.AllMissingParts.Select( p => new Dialog_InfoCard.Hyperlink( p.thingDef ) ) );

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats ()
        {
            yield return this.IncluededPartsStat;
            yield return this.CompatibilityStat;
            yield return this.MissingPartsStat;
        }

        private string GetCompatibilityReport ()
        {
            internalStringBuilder.Clear();

            foreach ( var ver in this.Props.SupportedVersions.Where( v => !(v is ProsthesisVersionSegment) ) )
            {
                internalStringBuilder.AppendFormat( "{0} {1}: {2}",
                    ver.Label.CapitalizeFirst(),
                    "LimbVersion".Translate(),
                    this.CompatibleVersions.Contains( ver )
                        ? "LimbCompatible".TranslateSimple()
                        : "LimbIncompatible".TranslateSimple() );

                if ( ver == this.TargetVersion )
                {
                    internalStringBuilder.Append( "LimbSelected".Translate() );
                }

                internalStringBuilder.AppendLine();

                List<string> labels = this.Props.GetRacesForVersion( ver );
                for ( int l = 0; l < labels.Count; l++ )
                {
                    internalStringBuilder.AppendFormat( " - {0}", labels[l].CapitalizeFirst() ).AppendLine();
                }

                internalStringBuilder.AppendLine();
            }

            return internalStringBuilder.ToString();
        }
        private static readonly StringBuilder internalStringBuilder = new StringBuilder();

        #endregion StatsDisplay

        #region RecursiveData

        /// <summary>
        /// Recursively searches for IncludedParts in all of the sub-parts
        /// </summary>
        public IEnumerable<(Thing thing, CompIncludedChildParts ownerComp)> AllIncludedParts => Enumerable.Concat(

                // the sub-parts included in this part
                this.IncludedParts.Select( p => (p, this) ),

                // the sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from couple in comp.AllIncludedParts
                select couple );

        /// <summary>
        /// Recursively searches for StandardParts in all of the sub-parts
        /// </summary>
        public IEnumerable<(ThingDef thingDef, CompIncludedChildParts ownerComp)> AllStandardParts => Enumerable.Concat(

                // the standard sub-parts included in this part
                this.StandardParts.Select( p => (p, this) ),

                // the standard sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from couple in comp.AllStandardParts
                select couple );

        public IEnumerable<(ThingDef thingDef, ProsthesisVersion limb, CompIncludedChildParts ownerComp)> AllMissingParts => Enumerable.Concat(

                // the standard sub-parts included in this part
                this.MissingParts.Select( p => (p.thingDef, p.limb, this) ),

                // the standard sub-parts of the children with CompIncludedChildParts
                from i in this.IncludedParts
                let comp = i.TryGetComp<CompIncludedChildParts>()
                where comp != null
                from triplet in comp.AllMissingParts
                select triplet );

        #endregion RecursiveData
    }
}