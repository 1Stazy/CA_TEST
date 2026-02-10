using System.Windows.Controls;
using CinemaSystem.Desktop.ViewModels;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok zarządzania biletami - edycja i anulacja rezerwacji.
    /// </summary>
    /// <remarks>
    /// Dostępny dla kasjerów - pozwala na zmianę lub anulowanie rezerwacji.
    /// Obsługuje zwroty biletów i korektę cen.
    /// </remarks>
    public partial class TicketManagementView : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Inicjalizuje nową instancję widoku zarządzania biletami.
        /// </summary>
        public TicketManagementView()
        {
            InitializeComponent();
        }
    }
}