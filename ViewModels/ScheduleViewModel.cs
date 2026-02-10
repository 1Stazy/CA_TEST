using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Services;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel zarządzający harmonogramem seansów dla kasjera.
    /// </summary>
    /// <remarks>
    /// Pozwala na przeglądanie seansów na wybranym dniu z opcją pokazania zarchiwizowanych seansów.
    /// 
    /// Powiązania:
    /// - Ładuje seanse z bazy danych dla wybranej daty.
    /// - Wspiera szybkie przyciski (dzisiaj, jutro, pojutrze) i selektor daty.
    /// - Umożliwia sprzedaż biletów na seanse, blokując już przeszłe seanse.
    /// </remarks>
    public partial class ScheduleViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;

        /// <summary>
        /// Wybrana data w kalendarzu (domyślnie dziś).
        /// </summary>
        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        /// <summary>
        /// Lista seansów dla wybranej daty.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Screening> _screenings = new();

        /// <summary>
        /// Flaga czy ładowanie listy idzie.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Flaga czy brak seansów na wybrany dzień.
        /// </summary>
        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>
        /// Czy wyświetlać tamtę (zakończone) seanse.
        /// </summary>
        [ObservableProperty]
        private bool _showArchived = false;

        /// <summary>
        /// Aktualnie wybrany przycisk szybkiej nawigacji (TODAY, TOMORROW, DAY2, CUSTOM).
        /// </summary>
        [ObservableProperty]
        private string _activeTab = "TODAY";

        partial void OnShowArchivedChanged(bool value) => LoadScreenings();

        /// <summary>
        /// Inicjalizuje ViewModel i ładuje seanse.
        /// </summary>
        public ScheduleViewModel(DashboardViewModel dashboardViewModel)
        {
            _dashboardViewModel = dashboardViewModel;
            LoadScreenings();
        }

        /// <summary>
        /// Reaguje na zmianę daty w kalendarzu - autoaktualizuje tabulaturę.
        /// </summary>
        partial void OnSelectedDateChanged(DateTime value)
        {
            LoadScreenings();
            if (value.Date == DateTime.Today) ActiveTab = "TODAY";
            else if (value.Date == DateTime.Today.AddDays(1)) ActiveTab = "TOMORROW";
            else if (value.Date == DateTime.Today.AddDays(2)) ActiveTab = "DAY2";
            else ActiveTab = "CUSTOM";
        }

        /// <summary>
        /// Ładuje seanse z bazy dla wybranej daty.
        /// </summary>
        private async void LoadScreenings()
        {
            IsLoading = true;
            IsEmpty = false;
            Screenings.Clear();

            await Task.Run(() =>
            {
                using (var context = new CinemaDbContext())
                {
                    var list = context.Screenings
                        .Include(s => s.Movie)
                        .Include(s => s.Hall)
                        .AsEnumerable()
                        .Where(s => s.Start.Date == SelectedDate.Date)
                        .Where(s => ShowArchived || s.Start > DateTime.Now)
                        .OrderBy(s => s.Start)
                        .ToList();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Screenings = new ObservableCollection<Screening>(list);
                        IsEmpty = Screenings.Count == 0;
                    });
                }
            });

            IsLoading = false;
        }

        /// <summary>
        /// Starts the ticket purchase process for selected screening.
        /// </summary>
        [RelayCommand]
        private void BookTicket(Screening screening)
        {
            if (screening == null) return;

            // Blokada sprzedaży biletów na już przeszłe seanse
            if (screening.Start < DateTime.Now)
            {
                System.Windows.MessageBox.Show(
                    "Nie można sprzedawać biletu na seans, który już się rozpoczął lub zakończył.",
                    "Miniony seans",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            _dashboardViewModel.CurrentContent = new SeatSelectionViewModel(screening, _dashboardViewModel);
        }

        /// <summary>
        /// Ustawia datę na dzisiaj.
        /// </summary>
        [RelayCommand]
        private void SetDateToday()
        {
            SelectedDate = DateTime.Today;
            ActiveTab = "TODAY";
        }

        /// <summary>
        /// Ustawia datę na jutro.
        /// </summary>
        [RelayCommand]
        private void SetDateTomorrow()
        {
            SelectedDate = DateTime.Today.AddDays(1);
            ActiveTab = "TOMORROW";
        }

        /// <summary>
        /// Ustawia datę na pojutrze.
        /// </summary>
        [RelayCommand]
        private void SetDateDayAfterTomorrow()
        {
            SelectedDate = DateTime.Today.AddDays(2);
            ActiveTab = "DAY2";
        }

        /// <summary>
        /// Załatwia wybór daty z datepickera - ustawia tab na CUSTOM.
        /// </summary>
        [RelayCommand]
        private void SetDateCustom()
        {
            ActiveTab = "CUSTOM";
        }
    }
}