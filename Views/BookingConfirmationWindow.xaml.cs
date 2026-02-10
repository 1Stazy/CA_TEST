using System.Diagnostics;
using System.Windows;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Okno potwierdzenia rezerwacji z przyciskami do otworzenia PDF i zamknięcia.
    /// </summary>
    /// <remarks>
    /// Wyświetlane po pomyślnym zakończeniu rezerwacji.
    /// Umożliwia klientowi otwarcie potwierdzenia w PDF lub zamknięcie okna.
    /// </remarks>
    public partial class BookingConfirmationWindow : Window
    {
        private string _pdfPath;

        /// <summary>
        /// Inicjalizuje okno z ścieżką do pliku PDF.
        /// </summary>
        /// <param name="pdfPath">Ścieżka do wygenerowanego pliku PDF z rezerwacją.</param>
        public BookingConfirmationWindow(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
        }

        /// <summary>
        /// Otwiera plik PDF rezerwacji w domyślnej aplikacji systemu.
        /// </summary>
        private void BtnOpenPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(_pdfPath) { UseShellExecute = true }
                }.Start();
            }
            catch { /* Ignoruj błędy otwierania */ }
            
            this.Close();
        }

        /// <summary>
        /// Zamyka okno potwierdzenia rezerwacji.
        /// </summary>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}