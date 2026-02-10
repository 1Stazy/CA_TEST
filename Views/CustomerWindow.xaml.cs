using System;
using System.Windows; 
using System.Windows.Threading;
using CinemaSystem.Desktop.ViewModels;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Okno do wyświetlania na ekranie klienta (druga monitory).
    /// </summary>
    /// <remarks>
    /// Automatycznie rotuje karuzelę filmów co 5 sekund.
    /// Zatrzymuje rotację gdy kursor myszy jest nad karuzelą.
    /// Użęcie: Przenoszone na drugą monitor poprzez ScreenManager.
    /// </remarks>
    public partial class CustomerWindow : Window
    {
        private DispatcherTimer _idleTimer;

        /// <summary>
        /// Inicjalizuje okno klienta z timerem do rotacji karuzelek.
        /// </summary>
        public CustomerWindow()
        {
            InitializeComponent();

            // Konfiguracja Timera dla Ekranu Klienta
            _idleTimer = new DispatcherTimer();
            _idleTimer.Interval = TimeSpan.FromSeconds(5); // Zmienia slajd co 5 sekund
            _idleTimer.Tick += IdleTimer_Tick;
            
            // Uruchamiamy timer od razu po załadowaniu okna
            this.Loaded += (s, e) => _idleTimer.Start();
            
            // Zatrzymujemy przy zamknięciu
            this.Closed += (s, e) => _idleTimer.Stop();
        }

        /// <summary>
        /// Obsługuje tick timera - rotuje karuzelę filmów.
        /// </summary>
        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var context = DataContext as dynamic;
                
                if (context != null)
                {
                    // Pobieramy aktualny widok wyświetlany klientowi
                    var content = context.ClientView;
                    
                    // Sprawdzamy, czy to widok z filmami (czyli ten z karuzelą)
                    if (content is MoviesViewModel vm)
                    {
                        // NAPRAWA BŁĘDU:
                        // Wywołujemy nową metodę dedykowaną dla klienta (tę, którą dodaliśmy do ViewModelu)
                        vm.RotateClientCarousel(); 
                    }
                }
            }
            catch (Exception)
            {
                // Ignorujemy błędy rzutowania
            }
        }
    }
}