using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VBank_ControlPanel.Classes
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                if (parameter?.ToString() == "withTime")
                {
                    return date.ToString("yyyy-MM-dd HH:mm:ss");
                }
                return date.ToString("yyyy-MM-dd");
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                if (parameter?.ToString() == "withTime" &&
                    DateTime.TryParseExact(str, "yyyy-MM-dd HH:mm:ss", culture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }

                if (DateTime.TryParseExact(str, "yyyy-MM-dd", culture, DateTimeStyles.None, out result))
                {
                    return result;
                }

                if (DateTime.TryParse(str, culture, DateTimeStyles.None, out result))
                {
                    return result;
                }
            }

            return Binding.DoNothing;
        }
    }
}
