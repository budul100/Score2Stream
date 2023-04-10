using System;
using System.Globalization;
using System.Text;

namespace Core.Converters
{
    public class EnumToStringConverter
           : OneWayValueConverter
    {
        #region Public Properties

        public string WordSeparator { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueName = Enum.GetName(value.GetType(), value);

            if (string.IsNullOrEmpty(WordSeparator) || valueName == null || valueName.Length <= 1)
                return valueName;

            var sb = new StringBuilder(valueName.Length * 2);
            sb.Append(valueName[0]);

            for (var index = 1; index < valueName.Length; index++)
            {
                if (char.IsUpper(valueName, index)
                    && char.IsLower(valueName, index + 1))
                {
                    sb.Append(WordSeparator);
                }

                sb.Append(valueName[index]);
            }

            return sb.ToString();
        }

        #endregion Public Methods
    }
}