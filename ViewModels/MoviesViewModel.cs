using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows.Threading;

namespace CinemaSystem.Desktop.ViewModels
{
    public partial class MoviesViewModel : ViewModelBase
    {
        private DashboardViewModel? _dashboardViewModel;
        private List<Film> _allFilmsCache = new();

        // --- REKLAMY ---
        [ObservableProperty] private string _currentAdText = "Zapraszamy na seanse!";
        private List<Advertisement> _adsCache = new();
        private int _currentAdIndex = 0;
        private DispatcherTimer _adTimer;

        // --- LISTY ---
        [ObservableProperty] private ObservableCollection<Film> _movies = new();
        [ObservableProperty] private ObservableCollection<Film> _recommendedMovies = new();
        [ObservableProperty] private ObservableCollection<Film> _allActiveMovies = new();
        [ObservableProperty] private ObservableCollection<GenreItem> _genres = new();

        // --- WYBORY ---
        [ObservableProperty] private Film? _selectedMovie;
        [ObservableProperty] private Film? _selectedRecommendedMovie;
        [ObservableProperty] private Film? _clientSelectedMovie;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isSearchEmpty;

        partial void OnSearchTextChanged(string value) => FilterMovies();

        public MoviesViewModel()
        {
            _adTimer = new DispatcherTimer();
            LoadMoviesAsync();
            LoadAds();
            StartAdTimer();
        }

        public MoviesViewModel(DashboardViewModel? dashboardViewModel) : this()
        {
            _dashboardViewModel = dashboardViewModel;
        }

        public void SetDashboard(DashboardViewModel dashboard)
        {
            _dashboardViewModel = dashboard;
        }

        private async void LoadMoviesAsync()
        {
            IsLoading = true;
            await Task.Delay(200);

            await Task.Run(() =>
            {
                try
                {
                    using (var context = new CinemaDbContext())
                    {
                        _allFilmsCache = context.Films
                            .Include(f => f.Director)
                            .Where(f => f.IsActive)
                            // POPRAWKA: Sortowanie alfabetyczne
                            .OrderBy(f => f.Title)
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            });

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 1. Klient - Wszystkie
                AllActiveMovies = new ObservableCollection<Film>(_allFilmsCache);

                // 2. Kasjer - Polecane (np. > 7.0)
                var topPicks = _allFilmsCache
                    .Where(f => f.ImdbRating >= 7.0)
                    .OrderByDescending(f => f.ImdbRating)
                    .Take(10)
                    .ToList();
                RecommendedMovies = new ObservableCollection<Film>(topPicks);

                // 3. Gatunki
                var allGenreNames = _allFilmsCache.Select(f => f.Genre).Distinct().OrderBy(g => g).ToList();
                Genres.Clear();
                foreach (var gName in allGenreNames)
                {
                    Genres.Add(new GenreItem(gName, FilterMovies));
                }

                // Domyślne wybory (żeby karuzele nie były puste)
                if (RecommendedMovies.Any()) SelectedRecommendedMovie = RecommendedMovies.First();
                if (AllActiveMovies.Any()) ClientSelectedMovie = AllActiveMovies.First();

                FilterMovies();
            });

            IsLoading = false;
        }

        private void FilterMovies()
        {
            if (_allFilmsCache == null) return;

            var query = SearchText?.ToLower().Trim() ?? "";
            var selectedGenres = Genres?.Where(g => g.IsSelected).Select(g => g.Name).ToList() ?? new List<string>();

            var filtered = _allFilmsCache.Where(f =>
                (string.IsNullOrEmpty(query) ||
                 f.Title.ToLower().Contains(query) ||
                 (f.Director != null && f.Director.Name.ToLower().Contains(query)) ||
                 f.ReleaseDate.Contains(query))
                &&
                (selectedGenres.Count == 0 || selectedGenres.Contains(f.Genre))
            );

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Movies = new ObservableCollection<Film>(filtered);
                IsSearchEmpty = Movies.Count == 0;
            });
        }

        /// <summary>
        /// Ładuje aktywne reklamy z bazy do cache'u dla karuzeli.
        /// </summary>
        private void LoadAds()
        {
            try
            {
                using (var context = new CinemaDbContext())
                {
                    _adsCache = context.Advertisements.Where(a => a.IsActive).ToList();
                    if (_adsCache.Any()) CurrentAdText = _adsCache[0].Content;
                }
            }
            catch { }
        }

        /// <summary>
        /// Uruchamia timer obrotu reklam (zmiana co 30 sekund).
        /// </summary>
        private void StartAdTimer()
        {
            if (_adTimer.IsEnabled) return;
            _adTimer.Interval = TimeSpan.FromSeconds(30);
            _adTimer.Tick += (s, e) => RotateAd();
            _adTimer.Start();
        }

        /// <summary>
        /// Zmienia aktualną reklamę na następną w karuzeli.
        /// </summary>
        private void RotateAd()
        {
            if (!_adsCache.Any()) return;
            _currentAdIndex++;
            if (_currentAdIndex >= _adsCache.Count) _currentAdIndex = 0;
            CurrentAdText = _adsCache[_currentAdIndex].Content;
        }

        /// <summary>
        /// Obraca karuzelę polecanych filmów dla kasjera (następny film).
        /// </summary>
        public void RotateCashierCarousel()
        {
            if (RecommendedMovies == null || RecommendedMovies.Count == 0) return;
            if (SelectedRecommendedMovie == null) { SelectedRecommendedMovie = RecommendedMovies.First(); return; }

            int currentIndex = RecommendedMovies.IndexOf(SelectedRecommendedMovie);
            int nextIndex = (currentIndex + 1) % RecommendedMovies.Count;
            SelectedRecommendedMovie = RecommendedMovies[nextIndex];
        }

        /// <summary>
        /// Obraca karuzelę filmów dla klienta (następny film).
        /// </summary>
        public void RotateClientCarousel()
        {
            if (AllActiveMovies == null || AllActiveMovies.Count == 0) return;
            if (ClientSelectedMovie == null) { ClientSelectedMovie = AllActiveMovies.First(); return; }

            int currentIndex = AllActiveMovies.IndexOf(ClientSelectedMovie);
            int nextIndex = (currentIndex + 1) % AllActiveMovies.Count;
            ClientSelectedMovie = AllActiveMovies[nextIndex];
        }

        /// <summary>
        /// Obsługa zmiany wybranego filmu kliента - wykorzystywana przez timer karuzeli.
        /// UWAGA: Nie zmieniamy tutaj widoku kasjera (CurrentContent) aby uniknąć "skakania" ekranów.
        /// </summary>
        partial void OnClientSelectedMovieChanged(Film? value)
        {
            // Pozostawiono puste - zmiana obsługiwana jest przez Binding w XAML
        }

        /// <summary>
        /// Przechodzi do widoku szczegółów wybranego filmu.
        /// </summary>
        [RelayCommand]
        private void NavigateToDetails(Film? movie)
        {
            if (movie == null || _dashboardViewModel == null) return;
            var detailsViewModel = new MovieDetailViewModel(movie, _dashboardViewModel);
            _dashboardViewModel.CurrentContent = detailsViewModel;
        }
    }
}