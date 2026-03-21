using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Score2Stream.Commons.Converters
{
    public class CountToBackgroundConverter
        : IValueConverter
    {
        #region Public Properties

        public IBrush EmptyBrush { get; set; } = Brushes.White;

        public IBrush FilledBrush { get; set; } = Brushes.Transparent;

        #endregion Public Properties

        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int i
                ? i
                : 0;

            return count == 0
                ? EmptyBrush
                : FilledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();

        #endregion Public Methods
    }
}