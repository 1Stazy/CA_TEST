using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CinemaSystem.Desktop.Services.Converters
{
    /// <summary>
    /// Konwerter WPF - zmienia warto\u015b\u0107 logiczn\u0105 na widoczno\u015b\u0107 elemenetu (Visibility).
    /// </summary>
    /// <remarks>
    /// Domy\u015blnie: True = Visible, False = Collapsed.
    /// Obs\u0142uguje parametr "Inverse" do odwrotnej logiki.
    /// Wykorzystywany do pokazywania/ukrywania kafelk\u00f3w i sekcji UI.
    /// </remarks>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Konwertuje warto\u015b\u0107 logiczn\u0105 na Visibility.
        /// Parametr "Inverse" odwraca logik\u0119 (True = Hidden, False = Visible).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Obsługa parametru "Inverse" (odwrócenie logiki)
                if (parameter?.ToString() == "Inverse")
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }

                // Domyślne zachowanie: True = Widoczny, False = Ukryty
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Domyślna wartość, jeśli binding zawiedzie
            return Visibility.Visible;
        }

        /// <summary>
        /// Konwersja odwrotna - zmienia Visibility z powrotem na bool.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }
}