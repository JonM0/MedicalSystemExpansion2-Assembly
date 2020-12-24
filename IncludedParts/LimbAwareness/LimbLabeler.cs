using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;

using Verse;

namespace MSE2
{
    internal class LimbLabeler
    {
        private readonly List<LimbConfiguration> limbPool;

        private readonly List<BodyPartDef> ignoredParts;

        private readonly Predicate<BodyDef> bodyRestrictions;

        private HashSet<BodyDef> cachedAllBodies;

        private ISet<BodyDef> AllBodies => this.cachedAllBodies
            ?? (this.cachedAllBodies = new HashSet<BodyDef>( this.limbPool.SelectMany( l => l.Bodies ).Where( this.bodyRestrictions.Invoke ) ));

        public LimbLabeler ( List<LimbConfiguration> limbPool, List<BodyPartDef> ignoredParts, Predicate<BodyDef> bodyRestrictions )
        {
            this.limbPool = limbPool;
            this.ignoredParts = ignoredParts;
            this.bodyRestrictions = bodyRestrictions;

            this.cachedLimbComparisons = new string[limbPool.Count];
            for ( int i = 0; i < limbPool.Count; i++ )
            {
                this.cachedLimbComparisons[i] = this.GetComparisonForLimb_Int( limbPool[i] );
            }
        }

        private bool PartShouldBeIgnored ( BodyPartDef bodyPartDef )
        {
            return this.ignoredParts != null && this.ignoredParts.Contains( bodyPartDef );
        }

        public List<string> GetRacesForLimb ( LimbConfiguration limb )
        {
            if ( limb == null )
            {
                return null;
            }

            List<string> outList = new List<string>();

            List<ThingDef> pawns = DefDatabase<ThingDef>.AllDefsListForReading;
            for ( int i = 0; i < pawns.Count; i++ )
            {
                ThingDef pawnDef = pawns[i];
                BodyDef body = pawnDef.race?.body;
                if ( body != null && limb.Bodies.Contains( body ) && this.AllBodies.Contains( body ) )
                {
                    // if is the only limb from this body
                    if ( !this.limbPool.Except( limb ).Any( l => l.Bodies.Contains( body ) ) )
                    {
                        outList.AddDistinct( pawnDef.label );
                    }
                    else
                    {
                        IEnumerable<string> recordUniqueNames = from bpr in limb.AllRecords
                                                                where bpr.body == body
                                                                select bpr.Label.Replace( bpr.LabelShort, "" ).Trim();

                        string records = string.Join( ", ", recordUniqueNames );

                        outList.AddDistinct( string.Format( "{0} ({1} {2})", pawnDef.label, records, limb.PartDef.LabelShort ) );
                    }
                }
            }

            return outList;
        }

        private List<(BodyPartDef, int)> LimbDifference ( LimbConfiguration limb )
        {
            List<(BodyPartDef, int)> difference = new List<(BodyPartDef, int)>();

            for ( int i = 0; i < limb.AllSegments.Count; i++ )
            {
                (BodyPartDef, int) item = limb.AllSegments[i];

                if ( !this.limbPool.TrueForAll( l => l.AllSegments.Contains( item ) ) )
                {
                    difference.Add( item );
                }
            }

            return difference;
        }

        private readonly string[] cachedLimbComparisons;

        private string GetComparisonForLimb_Int ( LimbConfiguration limb )
        {
            if ( limb == null ) return null;

            List<(BodyPartDef, int)> diffLimbs = this.LimbDifference( limb );
            builder.Clear();

            for ( int i = 0; i < diffLimbs.Count; i++ )
            {
                (BodyPartDef part, int count) = diffLimbs[i];

                if ( !this.PartShouldBeIgnored( part ) && count > 0 )
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

            if ( builder.Length == 0 )
            {
                return "LimbComplete".Translate();
            }
            else
            {
                return builder.ToString();
            }
        }

        public string GetComparisonForLimb ( LimbConfiguration limb )
        {
            int i = this.limbPool.IndexOf( limb );

            if ( i != -1 )
            {
                return this.cachedLimbComparisons[i];
            }
            else
            {
                return null;
            }
        }

        public string GetCompatibilityReport ( Predicate<LimbConfiguration> isCompatible )
        {
            builder.Clear();

            for ( int i = 0; i < this.limbPool.Count; i++ )
            {
                builder.AppendFormat( "{0} {1}: {2}",
                    this.cachedLimbComparisons[i].CapitalizeFirst(),
                    "LimbVersion".Translate(),
                    isCompatible( this.limbPool[i] ) ? "LimbCompatible".TranslateSimple() : "LimbIncompatible".TranslateSimple() ).AppendLine();

                List<string> labels = this.GetRacesForLimb( this.limbPool[i] );
                for ( int l = 0; l < labels.Count; l++ )
                {
                    builder.AppendFormat( " - {0}", labels[l].CapitalizeFirst() ).AppendLine();
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static readonly StringBuilder builder = new StringBuilder();
    }
}