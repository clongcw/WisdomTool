using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WisdomTool.Enums;

namespace WisdomTool.Converters
{
    public class BooleanToVisibilityConverter : ValueConverterBase<BooleanToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible && isVisible)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }
    }
}
