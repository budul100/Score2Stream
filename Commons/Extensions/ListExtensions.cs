using System;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.Commons.Extensions
{
    public static class ListExtensions
    {
        #region Public Methods

        public static T GetNext<T>(this IEnumerable<T> values, T active, bool backward = false)
        {
            var result = active;

            var array = values as T[]
                ?? values.ToArray();

            if (array.Length > 0)
            {
                var index = Array.IndexOf(
                    array: array,
                    value: active);

                if (backward)
                {
                    result = index > 0
                        ? array[index - 1]
                        : array[^1];
                }
                else
                {
                    result = index < array.Length - 1
                        ? array[index + 1]
                        : array[0];
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}