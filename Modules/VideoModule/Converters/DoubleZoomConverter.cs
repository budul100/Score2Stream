using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Score2Stream.Commons.Converters;

namespace Score2Stream.VideoModule.Converters
{
    public class DoubleZoomConverter
        : OneWayMultiValueConverter
    {
        #region Public Methods

        public override object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null
                || values.Count != 2
                || values.Any(v => v is UnsetValueType))
            {
                return null;
            }

            var value = (double)values[0];
            var zoom = (double?)values[1];

            if ((zoom ?? 0) == 0)
            {
                zoom = 1;
            }

            var result = value / zoom;

            return result;
        }

        #endregion Public Methods
    }
}