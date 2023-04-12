using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WisdomTool.Converters
{
    public class EnumToBooleanConverter : ValueConverterBase<EnumToBooleanConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            if (!Enum.IsDefined(value.GetType(), value))
            {
                return false;
            }

            object paramValue = Enum.Parse(value.GetType(), parameter.ToString());
            return value.Equals(paramValue);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return null;
            }

            if (!(bool)value)
            {
                return null;
            }

            return Enum.Parse(targetType, parameter.ToString());
        }
    }

}
