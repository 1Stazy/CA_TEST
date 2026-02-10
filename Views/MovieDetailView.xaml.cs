using System.Windows.Controls;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok szczegółów filmu - wyświetla opis, obsadę, reżyserig.
    /// </summary>
    /// <remarks>
    /// Wyświetlany po kliknięciu na film w karuzeli.
    /// Pokazuje pełne informacje o filmie oraz przycisk do rezerwacji.
    /// </remarks>
    public partial class MovieDetailView : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Inicjalizuje nową instancję widoku szczegółów filmu.
        /// </summary>
        public MovieDetailView()
        {
            InitializeComponent();
        }
    }
}