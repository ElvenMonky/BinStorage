using System;
using System.Collections.Generic;

namespace BinStorage.Common
{
    internal static class CommonExtentions
    {
        /// <summary>
        /// Tests that parameter is not 'null' and throws otherwise
        /// </summary>
        /// <param name="parameter">The paramater to be tested against 'null'</param>
        /// <param name="parameterName">The parameter name, used in <see cref="ArgumentNullException"/> thrown, when parameter is 'null'</param>
        /// <returns>Unchanged parameter value</returns>
        /// <exception cref="ArgumentNullException">Gets thrown if parameter is 'null'</exception>
        public static T ThrowIfNull<T>(this T parameter, string parameterName)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return parameter;
        }

        /// <summary>
        /// Tests that long parameter is non-negative and throws otherwise
        /// </summary>
        /// <param name="parameter">The paramater to be tested for non-negativeness</param>
        /// <param name="parameterName">Name of parameter being tested.</param>
        /// <returns>Unchanged parameter value</returns>
        /// <exception cref="ArgumentOutOfRangeException">Gets thrown if parameter is negative</exception>
        public static long ThrowIfNegative(this long parameter, string parameterName)
        {
            if (parameter < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return parameter;
        }

        /// <summary>
        /// Tests that long parameter is within specified range and throws otherwise
        /// </summary>
        /// <param name="parameter">The paramater to be tested against scecified range</param>
        /// <param name="from">lower bound of the allowed range</param>
        /// <param name="to">higher bound of the allowed range</param>
        /// <param name="parameterName">Name of parameter being tested.</param>
        /// <returns>Unchanged parameter value</returns>
        /// <exception cref="ArgumentOutOfRangeException">Gets thrown if parameter is out of range</exception>
        public static long ThrowIfOutOfRange(this long parameter, long from, long to, string parameterName)
        {
            if (parameter < from || parameter > to)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return parameter;
        }

        /// <summary>
        /// Searches sorted list for a specified key using binary search
        /// </summary>
        /// <typeparam name="T">Type of collection items</typeparam>
        /// <typeparam name="U">Type of key to search</typeparam>
        /// <param name="collection">Sorted collection</param>
        /// <param name="key">Value being searched</param>
        /// <param name="accessor">Function retrieving keys from collection items</param>
        /// <param name="comparer">Comparer for keys</param>
        /// <returns>Non negative item index if found, inverted value of best insertion position if not found</returns>
        public static int BinarySearch<T, U>(this IList<T> collection, U key, Func<T, U> accessor, IComparer<U> comparer = null)
        {
            comparer = comparer ?? Comparer<U>.Default;
            var lower = 0;
            var upper = collection.Count - 1;
            int middle = 0;

            while (lower <= upper)
            {
                middle = (upper + lower) / 2;
                var comparisonResult = comparer.Compare(key, accessor(collection[middle]));
                if (comparisonResult == 0)
                    return middle;
                else if (comparisonResult < 0)
                    upper = middle - 1;
                else
                    lower = middle + 1;
            }

            return ~lower;
        }

        /// <summary>
        /// Searches sorted list for a specified key using binary search
        /// </summary>
        /// <typeparam name="T">Type of collection items</typeparam>
        /// <param name="collection">Sorted collection</param>
        /// <param name="key">Value being searched</param>
        /// <param name="comparer">Comparer for keys</param>
        /// <returns>Non negative item index if found, inverted value of best insertion position if not found</returns>
        public static int BinarySearch<T>(this IList<T> collection, T key, IComparer<T> comparer = null)
        {
            return BinarySearch(collection, key, x => x);
        }
    }
}