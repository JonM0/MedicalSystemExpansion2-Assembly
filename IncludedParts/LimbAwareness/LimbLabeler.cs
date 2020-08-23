using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MSE2
{
    internal class LimbLabeler
    {
        private readonly List<LimbConfiguration> limbPool;

        private readonly Predicate<BodyDef> bodyRestrictions;

        private HashSet<BodyDef> cachedAllBodies;

        private ISet<BodyDef> AllBodies => cachedAllBodies
            ?? (cachedAllBodies = new HashSet<BodyDef>( limbPool.SelectMany( l => l.Bodies ).Where( bodyRestrictions.Invoke ) ));

        public LimbLabeler ( List<LimbConfiguration> limbPool, Predicate<BodyDef> bodyRestrictions )
        {
            this.limbPool = limbPool;
            this.bodyRestrictions = bodyRestrictions;
        }

        public string GetLabelForLimb ( LimbConfiguration limb )
        {
            if ( limb == null )
            {
                return null;
            }

            builder.Clear();

            foreach ( var body in limb.Bodies.Where( AllBodies.Contains ) )
            {
                // if is the only limb from this body
                if ( !limbPool.Except( limb ).Any( l => l.Bodies.Contains( body ) ) )
                {
                    builder.AppendWithSeparator( body.label, "; " );
                }
                else
                {
                    var recordUniqueNames = from bpr in limb.AllRecords
                                            where bpr.body == body
                                            select bpr.Label.Replace( bpr.LabelShort, "" ).Trim();

                    string records = string.Join( ", ", recordUniqueNames );

                    builder.AppendWithSeparator( string.Format( "{0} {1} {2}", records, body.label, limb.PartDef.LabelShort ), "; " );
                }
            }

            return builder.ToString();
        }

        private List<(BodyPartDef, int)> LimbDifference ( LimbConfiguration limb )
        {
            var difference = new List<(BodyPartDef, int)>();

            for ( int i = 0; i < limb.AllSegments.Count; i++ )
            {
                var item = limb.AllSegments[i];

                if ( !limbPool.TrueForAll( l => l.AllSegments.Contains( item ) ) )
                {
                    difference.Add( item );
                }
            }

            return difference;
        }

        public string GetComparisonForLimb ( LimbConfiguration limb )
        {
            if ( limb == null ) return null;

            var diffLimbs = LimbDifference( limb );
            builder.Clear();

            for ( int i = 0; i < diffLimbs.Count; i++ )
            {
                (BodyPartDef part, int count) = diffLimbs[i];

                if ( count > 0 )
                {
                    if ( count > 1 )
                    {
                        builder.AppendWithComma( count.ToStringCached() );
                        builder.Append( " " );
                        builder.Append( Find.ActiveLanguageWorker.Pluralize( part.label ) );
                    }
                    else
                    {
                        builder.AppendWithComma( Find.ActiveLanguageWorker.WithIndefiniteArticlePostProcessed( part.label ) );
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

        private static readonly StringBuilder builder = new StringBuilder();
    }
}