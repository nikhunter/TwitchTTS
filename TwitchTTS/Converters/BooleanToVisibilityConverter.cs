using System.Globalization;
using System.Windows;

namespace TwitchTTS.Converters
{
    [System.Windows.Localizability(System.Windows.LocalizationCategory.NeverLocalize)]
    public sealed class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
