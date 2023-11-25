using Score2Stream.Commons.Extensions;
using System;
using System.Globalization;

namespace Score2Stream.Commons.Converters
{
    public class DescriptionToStringConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumValue = value as Enum;

            var result = enumValue?.GetDescription();

            return result;
        }

        #endregion Public Methods
    }
}