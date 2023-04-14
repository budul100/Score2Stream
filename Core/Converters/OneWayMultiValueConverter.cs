using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Score2Stream.Core.Converters
{
    /// <summary>
    /// An abstract base class for multi-value converters that only convert one way (from source to binding).
    /// </summary>
    public abstract class OneWayMultiValueConverter
        : IMultiValueConverter
    {
        #region Public Methods

        /// <summary>
        /// Converts source values to a value for the binding target. The data binding
        /// engine calls this method when it propagates the values from source bindings
        /// to the binding target.
        /// </summary>
        /// <param name="values">The values produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted value.</returns>
        public abstract object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture);

        /// <summary>
        /// Converts a binding value back to its source value. This implementation always throws a NotSupportedException.
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(GetType().Name + " can only convert one-way.");
        }

        #endregion Public Methods
    }
}