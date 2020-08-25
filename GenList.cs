using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSE2
{
    public static class GenList
    {
        public static bool Remove<T> ( this List<T> list, Predicate<T> predicate )
        {
            var index = list.FindIndex( predicate );

            if ( index != -1 )
            {
                list.RemoveAt( index );
                return true;
            }
            else
            {
                return false;
            }
        }

        //public static int FindSequenceIndex<T> ( this List<T> list, List<T> sequence ) where T : IEquatable<T>
        //{
        //    if ( list is null )
        //    {
        //        throw new ArgumentNullException( nameof( list ) );
        //    }

        //    if ( sequence is null )
        //    {
        //        throw new ArgumentNullException( nameof( sequence ) );
        //    }

        //    int matched = 0;

        //    for ( int i = 0; i < list.Count; i++ )
        //    {

        //        if ( list[i].Equals( sequence[matched] ) )
        //        {
        //            if ( ++matched == sequence.Count )
        //            {
        //                return i - matched + 1;
        //            }
        //        }
        //        else
        //        {
        //            i -= matched;
        //            matched = 0;
        //        }
        //        i++;
        //    }

        //    return -1;
        //}
    }
}