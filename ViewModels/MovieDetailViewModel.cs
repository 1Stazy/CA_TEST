using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using CinemaSystem.Desktop.Core;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel wyświetlający szczegóły konkretnego filmu i jego seanse.
    /// </summary>
    /// <remarks>
    /// Powiązania:
    /// - Wyswietla informacje o wybranym filmie (SelectedFilm).
    /// - Pobiera i wyświetla seanse tego filmu (FilmScreenings).
    /// - Umozliwia przejście do wyboru miejsc dla wybranego seansu.
    /// - Komunikuje się z DashboardViewModel do zmiany widoku.
    /// </remarks>
    public partial class MovieDetailViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboard;

        /// <summary>
        /// Z wybrany film, dla którego są wyświetlane seanse.
        /// </summary>
        [ObservableProperty]
        private Film _selectedFilm;

        /// <summary>
        /// Kolekcja seansów dla wybranego filmu w przyszłości.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Screening> _filmScreenings = new();

        /// <summary>
        /// Inicjalizuje ViewModel z filmem i dashboard'em.
        /// </summary>
        public MovieDetailViewModel(Film film, DashboardViewModel dashboard)
        {
            _selectedFilm = film;
            _dashboard = dashboard;
            LoadScreenings();
        }

        /// <summary>
        /// Ładuje seanse wybranego filmu z bazy danych.
        /// </summary>
        private void LoadScreenings()
        {
            using (var context = new CinemaDbContext())
            {
                var list = context.Screenings
                    .Include(s => s.Hall)
                    .Include(s => s.Movie)
                    .Where(s => s.MovieId == SelectedFilm.Id && s.Start > DateTime.Now)
                    .OrderBy(s => s.Start)
                    .Take(10)
                    .ToList();

                FilmScreenings = new ObservableCollection<Screening>(list);
            }
        }

        /// <summary>
        /// Powraca do listy filmów/widoku filmów.
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            _dashboard.NavigateToMovies();
        }

        /// <summary>
        /// Przechodzi do wyboru miejsc dla wybranego seansu.
        /// </summary>
        [RelayCommand]
        private void BuyTicket(Screening screening)
        {
            _dashboard.CurrentContent = new SeatSelectionViewModel(screening, _dashboard, null);
        }
    }
}