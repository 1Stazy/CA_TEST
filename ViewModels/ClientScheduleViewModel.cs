using CommunityToolkit.Mvvm.ComponentModel;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using Microsoft.EntityFrameworkCore;
using CinemaSystem.Desktop.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel wyświetlający harmonogram seansów dla klienta aplikacji.
    /// </summary>
    /// <remarks>
    /// Powiązania:
    /// - Pobiera listę nadchodzących seansów z bazy danych.
    /// - Wyświetla do 8 najbliższych seansów uporządkowanych chronologicznie.
    /// - Każdy seans zawiera dane o filmie i sali.
    /// </remarks>
    public partial class ClientScheduleViewModel : ViewModelBase
    {
        /// <summary>
        /// Kolekcja nadchodzących seansów do wyświetlenia.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Screening> _upcomingScreenings = new();

        /// <summary>
        /// Inicjalizuje ViewModel i ładuje seanse.
        /// </summary>
        public ClientScheduleViewModel()
        {
            LoadScreenings();
        }

        /// <summary>
        /// Ładuje seanse z bazy danych - wydobywa najbliższe 8 seansów.
        /// </summary>
        private void LoadScreenings()
        {
            try
            {
                using (var context = new CinemaDbContext())
                {
                    var now = DateTime.Now;

                    // Pobieramy 8 najbliższych seansów, które jeszcze się nie odbęły
                    var list = context.Screenings
                        .AsNoTracking()
                        .Include(s => s.Movie)
                        .Include(s => s.Hall)
                        .Where(s => s.Start > now)
                        .OrderBy(s => s.Start)
                        .Take(8)
                        .ToList();

                    UpcomingScreenings = new ObservableCollection<Screening>(list);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd harmonogramu klienta: {ex.Message}");
            }
        }
    }
}