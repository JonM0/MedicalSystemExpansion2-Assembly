using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSE2
{
    internal static class GenCollections
    {
        internal static bool Remove<T>(this List<T> list, Predicate<T> predicate)
        {
            var index = list.FindIndex( predicate );

            if(index != -1)
            {
                list.RemoveAt( index );
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
