using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Pete.Converters
{
    public class FirstDegreeConverter : IValueConverter
    {
        #region Properties
        public string Delimiter { get; set; } = "|";
        #endregion

        #region Methods
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));

            double dubVal;
            if (!(value is double))
            {
                if (value is IConvertible conv)
                    dubVal = conv.ToDouble(null);
                else
                    throw new ArgumentException($"This converter can only be used on numerical types which can be converted to double. Instead got a value of type '{value.GetType()}'.", nameof(value));
            }
            else dubVal = (double)value;
            GetParams(parameter, out double val1, out double val2);

            dubVal = (dubVal * val1) + val2;

            if (!targetType.Equals(typeof(double)))
                return ((IConvertible)dubVal).ToType(targetType, null);

            return dubVal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));

            if (!(value is double dubVal))
            {
                if (value is IConvertible conv)
                    dubVal = conv.ToDouble(null);
                else
                    throw new ArgumentException($"This converter can only be used on numerical types which can be converted to double. Instead got a value of type '{value.GetType()}'.", nameof(value));
            }
            GetParams(parameter, out double val1, out double val2);

            dubVal = (dubVal * -val2) / val1;

            if (!targetType.Equals(typeof(double)))
                return ((IConvertible)dubVal).ToType(targetType, null);

            return dubVal;
        }
        private void GetParams(object param, out double val1, out double val2)
        {
            if (param is string str) GetParams(str, out val1, out val2);
            else throw new ArgumentException($"Parameter must be 2 double values seperated by a '{Delimiter}'.", nameof(param));
        }
        private void GetParams(string param, out double val1, out double val2)
        {
            string[] parts = param.Split(Delimiter);
            if (parts.Length == 2)
            {
                if (!double.TryParse(parts[0], out val1) || !double.TryParse(parts[1], out val2))
                    throw new ArgumentException($"Parameter contained invalid values, could not convert either '{parts[0]}' or '{parts[1]}' to a double.", nameof(param));
            }
            else throw new ArgumentException($"Must only provide 2 values seperated by a '{Delimiter}' as the parameter, got {parts.Length} instead.", nameof(param));
        }
        #endregion
    }
}
