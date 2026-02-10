using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CinemaSystem.Desktop.Services.Converters
{
    /// <summary>
    /// Konwerter WPF - zmienia wartość logiczną na kolor.
    /// </summary>
    /// <remarks>
    /// Użęcie: True -> zielony (LightGreen), False -> czerwony (Salmon).
    /// Idealne do wizualizacji trendów (zysk/strata, wzrost/spadek w raportach).
    /// </remarks>
    public class BooleanToColorConverter : IValueConverter
    {
        /// <summary>
        /// Konwertuje wartość logiczną na kolor pedzla (Color Brush).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUp)
                return isUp ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.Salmon);
            return new SolidColorBrush(Colors.Gray);
        }

        /// <summary>
        /// Konwerter odwrotny - nie jest implementowany.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}