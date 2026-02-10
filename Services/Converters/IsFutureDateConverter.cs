using System;
using System.Globalization;
using System.Windows.Data;

namespace CinemaSystem.Desktop.Services.Converters
{
    /// <summary>
    /// Konwerter WPF - sprawdza czy data jest w przyszłości czy przeszłości.
    /// </summary>
    /// <remarks>
    /// Zwraca True dla dat przyszłych, False dla przeszłych.
    /// Użęcie: wybór seansow do rezerwacji, blokowanie przeszłych seansów.
    /// </remarks>
    public class IsFutureDateConverter : IValueConverter
    {
        /// <summary>
        /// Sprawdza czy przeszłana data jest większa niż teraz (przyszłość).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                // Zwraca TRUE jeśli data jest w przyszłości, FALSE jeśli w przeszłości
                return date > DateTime.Now;
            }
            return false;
        }

        /// <summary>
        /// Konwerter odwrotny - nie jest implementowany.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}