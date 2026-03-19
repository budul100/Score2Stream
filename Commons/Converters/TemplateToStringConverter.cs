using System;
using System.Globalization;
using Score2Stream.Commons.Models.Contents;

namespace Score2Stream.Commons.Converters
{
    public class TemplateToStringConverter
        : OneWayValueConverter
    {
        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Template template)
            {
                return template.Name;
            }

            return "(None)";
        }

        #endregion Public Methods
    }
}