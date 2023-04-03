using System;
using System.Globalization;
using System.Windows.Data;

namespace Core.Converters
{
    /// <summary>
    /// An abstract base class for value converters that only convert one way (from source to binding).
    /// </summary>
    public abstract class OneWayValueConverter
        : IValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted value.</returns>
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Converts a binding value back to its source value. This implementation always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only convert one-way.");
        }

        #endregion Public Methods
    }
}