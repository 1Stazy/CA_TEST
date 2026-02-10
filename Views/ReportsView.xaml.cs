using System.Windows.Controls;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok raportów - wyświetla statystyki sprzedaży i frekwencji.
    /// </summary>
    /// <remarks>
    /// Dostępny tylko dla kierownika/menadżerów.
    /// Zawiera wykresy, tabele i możliwość eksportu do PDF.
    /// </remarks>
    public partial class ReportsView : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Inicjalizuje nową instancję widoku raportów.
        /// </summary>
        public ReportsView()
        {
            InitializeComponent();
        }
    }
}