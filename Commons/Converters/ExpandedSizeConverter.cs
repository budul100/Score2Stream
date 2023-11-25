using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Score2Stream.Commons.Converters
{
    public class ExpandedSizeConverter
        : OneWayMultiValueConverter
    {
        #region Public Methods

        public override object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null
                || values.Count != 3
                || values.Any(v => v is UnsetValueType))
            {
                return null;
            }

            var position = (double?)values[0];
            var givenSize = (double?)values[1];
            var actualSize = (double?)values[2];

            var result = actualSize > givenSize
                ? position - ((actualSize - givenSize) / 2)
                : position;

            return result;
        }

        #endregion Public Methods
    }
}