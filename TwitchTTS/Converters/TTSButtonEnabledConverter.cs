using System;
using System.Globalization;
using System.Windows.Data;

namespace TwitchTTS.Converters
{
    public class TTSButtonEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Default state (disabled)
            if (values.Length < 2)
                return false;

            // values[0] is Username
            // values[1] is Voices
            var username = values[0] as string;
            var voices = values[1] as System.Collections.IEnumerable;

            // Check if Username is null or empty and if Voices is empty
            bool isUsernameInvalid = string.IsNullOrEmpty(username);
            bool isVoicesEmpty = voices == null || !voices.GetEnumerator().MoveNext();

            // Return true if the button should be enabled
            return !isUsernameInvalid && !isVoicesEmpty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}