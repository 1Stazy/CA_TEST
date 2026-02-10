using System;
using System.Windows; 
using System.Windows.Controls;
using System.Windows.Input; 
using System.Windows.Threading;
using CinemaSystem.Desktop.ViewModels;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok z karuzelą filmów do przeglądania i wybóru.
    /// </summary>
    /// <remarks>
    /// Zawiera karuzelę 3D,  automatycznie rotującą co 5 sekund.
    /// Rotacja wstrzymuje się na najechanie myszką, wznawia po jej zmoved.
    /// Dla kasjera i klienta istnieją oddzielne metody rotacji (opr.
    /// </remarks>
    // FIX 1: Jawnie wskazujemy System.Windows.Controls.UserControl
    public partial class MoviesView : System.Windows.Controls.UserControl
    {
        private DispatcherTimer _carouselTimer;

        /// <summary>
        /// Inicjalizuje widok z timerem dla karuzelek.
        /// </summary>
        public MoviesView()
        {
            InitializeComponent();

            _carouselTimer = new DispatcherTimer();
            _carouselTimer.Interval = TimeSpan.FromSeconds(5);
            _carouselTimer.Tick += CarouselTimer_Tick;

            this.Loaded += OnViewLoaded;
            this.Unloaded += OnViewUnloaded;
        }

        /// <summary>
        /// Obsługuje załadowanie widoku - uruchamia timer i podpina eve<br/>nty myszy.
        /// </summary>
        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            _carouselTimer.Start();

            if (MainCarousel != null)
            {
                MainCarousel.MouseEnter -= OnCarouselMouseEnter;
                MainCarousel.MouseLeave -= OnCarouselMouseLeave;

                MainCarousel.MouseEnter += OnCarouselMouseEnter;
                MainCarousel.MouseLeave += OnCarouselMouseLeave;
            }
        }

        /// <summary>
        /// Obsługuje wyładowanie widoku - zatrzymuje timer i zmy\wa eventy.
        /// </summary>
        private void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            _carouselTimer.Stop();

            if (MainCarousel != null)
            {
                MainCarousel.MouseEnter -= OnCarouselMouseEnter;
                MainCarousel.MouseLeave -= OnCarouselMouseLeave;
            }
        }

        /// <summary>
        /// Obsługuje najechanie myszy na karuzelę - zatrzymuje rotację.
        /// </summary>
        // FIX 2: Jawnie wskazujemy System.Windows.Input.MouseEventArgs (WPF)
        private void OnCarouselMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Kasjer najechał myszką -> ZATRZYMUJEMY timer
            _carouselTimer.Stop();
            
            // Opcjonalnie: ustawiamy flagę w ViewModelu, jeśli tego używasz
            if (DataContext is MoviesViewModel vm)
            {
               // vm.IsPaused = true; 
            }
        }

        /// <summary>
        /// Obsługuje zjechanie myszy z karuzelek - wznawia rotację.
        /// </summary>
        // FIX 3: Jawnie wskazujemy System.Windows.Input.MouseEventArgs (WPF)
        private void OnCarouselMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Kasjer zjechał myszką -> WZNAWIAMY timer
            _carouselTimer.Start();
            
            if (DataContext is MoviesViewModel vm)
            {
               // vm.IsPaused = false;
            }
        }

        /// <summary>
        /// Obsługuje tick timera - rotuje karuzelę filmńw dla kasjera.
        /// </summary>
        private void CarouselTimer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is MoviesViewModel vm)
            {
                // ZMIANA: Wywołujemy metodę dedykowaną dla kasjera
                vm.RotateCashierCarousel();
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie na pole "szukaj" - ustawia fokus i kursor.
        /// </summary>
        private void FocusSearchBox(object sender, MouseButtonEventArgs e)
        {
            if (SearchBox != null)
            {
                SearchBox.Focus();
                SearchBox.CaretIndex = SearchBox.Text.Length;
            }
        }
    }
}