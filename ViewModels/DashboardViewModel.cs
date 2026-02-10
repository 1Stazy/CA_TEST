using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using System;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel głównego panelu dla kasjera systemu kinematograficznego.
    /// </summary>
    /// <remarks>
    /// Zarządza nawigacją między różnymi widokami (filmy, seanse, raporty, zarządzanie biletami).
    /// Obsługuje zalogowanego użytkownika i zdarzenia wylogowania oraz wyświetlania harmonogramu klienta.
    /// 
    /// Powiązania:
    /// - Przechowuje referencję do MoviesViewModel - wspólny widok filmów dla kasjera i klienta.
    /// - Emituje zdarzenia: LogoutRequested, ShowClientScheduleRequested.
    /// - Zarządza obiektem CurrentContent, który określa aktualnie wyświetlany widok.
    /// </remarks>
    public partial class DashboardViewModel : ViewModelBase
    {
        /// <summary>
        /// Aktualnie wyświetlana zawartość (widok kasjera).
        /// </summary>
        [ObservableProperty]
        private object _currentContent;

        /// <summary>
        /// Zalogowany użytkownik aplikacji.
        /// </summary>
        [ObservableProperty]
        private User? _currentUser;

        /// <summary>
        /// Wyświetlana nazwa użytkownika w interfejsie.
        /// </summary>
        public string UserNameDisplay => CurrentUser != null 
            ? $"{CurrentUser.FullName} ({CurrentUser.Role})" 
            : "Gość";

        /// <summary>
        /// Zdarzenie wywoływane, gdy kasjer naciśnie przycisk wylogowania.
        /// </summary>
        public event Action? LogoutRequested;

        /// <summary>
        /// Zdarzenie wywoływane, gdy kasjer chce pokazać harmonogram klientowi.
        /// </summary>
        public event Action? ShowClientScheduleRequested;

        private readonly MoviesViewModel _moviesViewModel;

        /// <summary>
        /// Inicjalizuje DashboardViewModel.
        /// </summary>
        public DashboardViewModel(MoviesViewModel moviesVM)
        {
            _moviesViewModel = moviesVM;
            _moviesViewModel.SetDashboard(this);
            CurrentContent = _moviesViewModel;
        }

        /// <summary>
        /// Inicjalizuje DashboardViewModel z zalogowanym użytkownikiem.
        /// </summary>
        public DashboardViewModel(MoviesViewModel moviesVM, User user) : this(moviesVM)
        {
            CurrentUser = user;
        }

        /// <summary>
        /// Nawiguje do widoku listy filmów.
        /// </summary>
        [RelayCommand]
        public void NavigateToMovies() => CurrentContent = _moviesViewModel;

        /// <summary>
        /// Nawiguje do widoku harmonogramu seansów.
        /// </summary>
        [RelayCommand]
        public void NavigateToSchedule() => CurrentContent = new ScheduleViewModel(this);

        /// <summary>
        /// Nawiguje do widoku raportów sprzedaży i statystyk.
        /// </summary>
        [RelayCommand]
        public void NavigateToReports() => CurrentContent = new ReportsViewModel();

        /// <summary>
        /// Nawiguje do widoku zarządzania biletami (zwroty, zmiana danych, itp.).
        /// </summary>
        [RelayCommand]
        private void GoToTicketManagement() => CurrentContent = new TicketManagementViewModel(this);

        /// <summary>
        /// Emituje zdarzenie do wyświetlenia harmonogramu klientowi.
        /// </summary>
        [RelayCommand]
        private void ShowClientSchedule() => ShowClientScheduleRequested?.Invoke();

        /// <summary>
        /// Emituje zdarzenie wylogowania.
        /// </summary>
        [RelayCommand]
        private void Logout() => LogoutRequested?.Invoke();
    }
}