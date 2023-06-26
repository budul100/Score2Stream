using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Score2Stream.Core.Extensions
{
    public static class EnumExtensions
    {
        #region Public Methods

        public static string GetDescription<T>(this T value)
            where T : Enum
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return value.ToString();
            }
            else
            {
                var attrib = attribArray[0] as DescriptionAttribute;

                return attrib.Description;
            }
        }

        public static IEnumerable<T> GetValues<T>()
            where T : Enum
        {
            var result = Enum.GetValues(typeof(T))
                .OfType<T>();

            return result;
        }

        #endregion Public Methods
    }
}