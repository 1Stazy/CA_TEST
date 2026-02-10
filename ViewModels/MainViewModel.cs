using CommunityToolkit.Mvvm.ComponentModel;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// Główny ViewModel aplikacji, zarządzający widokami dla kasjera i klienta.
    /// </summary>
    /// <remarks>
    /// Architektura:
    /// - CurrentView: widok kasjera (logowanie -> dashboard)
    /// - ClientView: widok dla klienta (filmy, szczegóły, miejsca, podsumowanie)
    /// 
    /// Powiązania:
    /// - Instancjonuje LoginViewModel, DashboardViewModel, ClientScheduleViewModel.
    /// - Synchronizuje zdarzenia z DashboardViewModel do aktualizacji ClientView.
    /// - Zarządza harmonogramem 30-sekundowego wyświetlania harmonogramu dla klienta.
    /// </remarks>
    public partial class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Bieżący widok dla kasjera (ekran logowania lub dashboard).
        /// </summary>
        [ObservableProperty]
        private object _currentView;

        /// <summary>
        /// Widok dla klienta wyświetlany na drugim monitorze/ekranie.
        /// </summary>
        [ObservableProperty]
        private object _clientView;

        private readonly MoviesViewModel _moviesViewModel;
        private DashboardViewModel? _dashboardViewModel;
        private DispatcherTimer? _clientViewTimer;

        /// <summary>
        /// Inicjalizuje MainViewModel - przygotowuje widoki i wyświetla ekran logowania.
        /// </summary>
        public MainViewModel()
        {
            _moviesViewModel = new MoviesViewModel();
            _clientView = _moviesViewModel;
            _currentView = new object();
            ShowLoginScreen();
        }

        /// <summary>
        /// Wyświetla ekran logowania.
        /// </summary>
        private void ShowLoginScreen()
        {
            var loginVM = new LoginViewModel();
            loginVM.LoginSuccess += OnLoginSuccess;
            CurrentView = loginVM;
        }

        /// <summary>
        /// Obsługuje zdarzenie pomyślnego logowania - tworzy dashboard i podpina zdarzenia.
        /// </summary>
        private void OnLoginSuccess(User user)
        {
            _dashboardViewModel = new DashboardViewModel(_moviesViewModel, user);

            // Wylogowanie
            _dashboardViewModel.LogoutRequested += () =>
            {
                ClientView = _moviesViewModel;
                ShowLoginScreen();
            };

            // Pokaż harmonogram klientowi (30 sekund)
            _dashboardViewModel.ShowClientScheduleRequested += StartClientScheduleTimer;

            // Synchronizacja widoków
            _dashboardViewModel.PropertyChanged += Dashboard_PropertyChanged;

            CurrentView = _dashboardViewModel;
        }

        /// <summary>
        /// Startuje timer wyświetlania harmonogramu klientowi na 30 sekund.
        /// </summary>
        private void StartClientScheduleTimer()
        {
            ClientView = new ClientScheduleViewModel();

            if (_clientViewTimer != null) 
                _clientViewTimer.Stop();

            _clientViewTimer = new DispatcherTimer();
            _clientViewTimer.Interval = TimeSpan.FromSeconds(30);
            _clientViewTimer.Tick += (s, e) =>
            {
                ClientView = _moviesViewModel;
                _clientViewTimer.Stop();
            };
            _clientViewTimer.Start();
        }

        /// <summary>
        /// Synchronizuje zmianę widoku kasjera z widokiem klienta.
        /// </summary>
        private void Dashboard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DashboardViewModel dashboard || 
                e.PropertyName != nameof(DashboardViewModel.CurrentContent))
                return;

            var content = dashboard.CurrentContent;

            // Klient widzi tylko proces zakupu (filmy, szczegóły, miejsca, podsumowanie)
            if (content is MoviesViewModel or MovieDetailViewModel or SeatSelectionViewModel or TicketSummaryViewModel)
            {
                if (_clientViewTimer != null && _clientViewTimer.IsEnabled) 
                    _clientViewTimer.Stop();
                ClientView = content;
            }
            else
            {
                if (_clientViewTimer == null || !_clientViewTimer.IsEnabled)
                    ClientView = _moviesViewModel;
            }
        }
    }
}