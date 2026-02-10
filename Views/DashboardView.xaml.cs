using System.Windows.Controls;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok głównego panelu nawigacyjnego aplikacji.
    /// </summary>
    /// <remarks>
    /// Działa jako główny widok (UserControl) dla managera/kierownika kina.
    /// Zawiera przyciski nawigacji do różnych widoków (raporty, planą, zakresy).
    /// </remarks>
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Inicjalizuje nową instancję klasy DashboardView.
        /// </summary>
        public DashboardView()
        {
            InitializeComponent();
        }
    }
}
