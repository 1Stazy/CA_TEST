using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis zarządzania ekranami - obsługuje przenoszenie okna na drugi monitor.
    /// </summary>
    /// <remarks>
    /// Umożliwia rozciągnięcie widoku klienta (CustomerWindow) na drugi monitor
    /// w systemach multimonitor, idealnie dla kin z ekranem czekalni.
    /// </remarks>
    public static class ScreenManager
    {
        /// <summary>
        /// Przenosi i maksymalizuje okno na drugie urządzenie wyświetlające.
        /// Jeśli dostępny jest tylko jeden monitor, wyświetla okno normalnie.
        /// </summary>
        /// <param name="customerWindow">Okno klienta do przeniesienia.</param>
        public static void OpenCustomerWindow(Window customerWindow)
        {
            // Pobieramy listę wszystkich ekranów
            var screens = Screen.AllScreens;

            // Szukamy drugiego ekranu (który nie jest główny)
            // Jeśli jest tylko jeden, bierzemy pierwszy (do testów)
            var targetScreen = screens.FirstOrDefault(s => !s.Primary) ?? screens.FirstOrDefault();

            if (targetScreen != null)
            {
                // Upewniamy się, że okno jest w trybie normalnym przed przeniesieniem
                customerWindow.WindowState = WindowState.Normal;
                customerWindow.WindowStyle = WindowStyle.None; // Bez belkami systemowymi
                
                // Ustawiamy pozycję okna idealnie na współrzędnych ekranu docelowego
                customerWindow.Left = targetScreen.WorkingArea.Left;
                customerWindow.Top = targetScreen.WorkingArea.Top;
                
                // Wymuszamy rozmiar
                customerWindow.Width = targetScreen.Bounds.Width;
                customerWindow.Height = targetScreen.Bounds.Height;

                // Pokazujemy okno
                customerWindow.Show();
                
                // Maksymalizujemy
                customerWindow.WindowState = WindowState.Maximized;
            }
            else
            {
                // Fallback, jeśli coś poszło nie tak
                customerWindow.Show();
            }
        }
    }
}