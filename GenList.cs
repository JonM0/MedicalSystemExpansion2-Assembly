using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSE2
{
    public static class GenList
    {
        /// <summary>
        /// Removes the first element that matches the conditions defined by the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the list</typeparam>
        /// <param name="list">The list from where to remove the element</param>
        /// <param name="predicate">The <c>System.Predicate</c> delegate that defines the conditions of the elements to remove.</param>
        /// <returns><see langword="true"/> if item is successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if item was not found in the <c>System.Collections.Generic.List</c>.</returns>
        /// <exception cref="System.ArgumentNullException">if predicate is null</exception>
        public static bool Remove<T> ( this List<T> list, Predicate<T> predicate )
        {
            int index = list.FindIndex( predicate );

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
    }
}