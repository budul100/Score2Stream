using Score2Stream.Core.Models.Contents;
using System.Collections.Generic;
using System.Linq;

namespace Score2Stream.Core.Extensions
{
    public static class NameableExtensions
    {
        #region Public Methods

        public static string GetNextName<T>(this IEnumerable<T> values)
            where T : Nameable
        {
            var index = 0;

            string result;

            var typeName = typeof(T).Name;

            do
            {
                result = $"{typeName}{++index}";
            } while (values.Any(c => c.Name == result));

            return result;
        }

        #endregion Public Methods
    }
}