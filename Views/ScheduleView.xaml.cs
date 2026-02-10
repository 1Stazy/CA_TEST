using System.Windows.Controls;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok planu szeąściowy - pokazuje seansy filmów w tygodniowym rozpisę.
    /// </summary>
    /// <remarks>
    /// Pozwala użytkownikowi przejrzeć dostran seansy i wybyę do rezerwacji.
    /// Zawiera filtry du dnia, godziny i typu sali.
    /// </remarks>
    public partial class ScheduleView : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Inicjalizuje nową instancję widoku harmonogramu.
        /// </summary>
        public ScheduleView() => InitializeComponent();
    }
}