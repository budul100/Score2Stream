using System.Collections;
using System.Globalization;

namespace Score2Stream.Commons.Converters
{
    /// <summary>
    /// Converts an enumerable to a boolean that indicates whether it has items.
    /// </summary>
    public sealed class HasItemsConverter
        : OneWayValueConverter
    {
        #region Public Methods

        /// <inheritdoc />
        public override object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return false;
            }

            if (value is ICollection collection)
            {
                return collection.Count > 0;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var _ in enumerable)
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        #endregion Public Methods
    }
}