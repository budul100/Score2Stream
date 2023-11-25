using System.Collections.Generic;

namespace Score2Stream.Commons.Extensions
{
    public static class ListExtensions
    {
        #region Public Methods

        public static T GetNext<T>(this List<T> values, T active, bool backward = false)
        {
            var result = active;

            if (values.Count > 0)
            {
                var index = values.IndexOf(active);

                if (backward)
                {
                    result = index > 0
                        ? values[index - 1]
                        : values[^1];
                }
                else
                {
                    result = index < values.Count - 1
                        ? values[index + 1]
                        : values[0];
                }
            }

            return result;
        }

        #endregion Public Methods
    }
}