using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using CinemaSystem.Desktop.Services;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel do wyboru miejsc w sali kinowej przed zakupem biletu.
    /// </summary>
    /// <remarks>
    /// Obsługuje zarówno sprzedaż nowych biletów jak i edycję istniejących biletów (zmianę miejsca).
    /// Powiązania:
    /// - Ładuje zarezerwowane już miejsca z bazy danych.
    /// - Generuje mapę sali na podstawie ilości rzędów i miejsc.
    /// - Komunikuje się z DashboardViewModel do nawigacji.
    /// </remarks>
    public partial class SeatSelectionViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;
        
        /// <summary>
        /// Aktualnie wybrany seans (film, sala, czas).
        /// </summary>
        [ObservableProperty]
        private Screening _currentScreening;

        /// <summary>
        /// Tekstowy opis podsumowania wyborów (liczba wybranych miejsc, cena przybliżona).
        /// </summary>
        [ObservableProperty]
        private string _summaryText = "Wybierz miejsca";

        /// <summary>
        /// Liczba rzędów w sali.
        /// </summary>
        [ObservableProperty]
        private int _rowsCount;

        /// <summary>
        /// Liczba miejsc w każdym rzędzie.
        /// </summary>
        [ObservableProperty]
        private int _columnsCount;

        /// <summary>
        /// Czy jesteśmy w trybie edycji istniejącego biletu.
        /// </summary>
        [ObservableProperty]
        private bool _isEditMode;

        private Ticket? _ticketToEdit;

        public ObservableCollection<Seat> Seats { get; set; }
        public ObservableCollection<string> RowLabels { get; set; }

        public SeatSelectionViewModel(Screening screening, DashboardViewModel dashboardViewModel, Ticket? ticketToEdit = null)
        {
            _dashboardViewModel = dashboardViewModel;
            _ticketToEdit = ticketToEdit;
            IsEditMode = _ticketToEdit != null;
            
            CurrentScreening = screening;

            Seats = new ObservableCollection<Seat>();
            RowLabels = new ObservableCollection<string>();

            if (CurrentScreening?.Hall != null)
            {
                RowsCount = CurrentScreening.Hall.Rows;
                ColumnsCount = CurrentScreening.Hall.SeatsPerRow;

                GenerateCinemaHall();
                LoadReservedSeats();
            }
        }

        private void LoadReservedSeats()
        {
            using (var context = new CinemaDbContext())
            {
                if (CurrentScreening == null) return;

                var takenSeats = context.Tickets
                    .Include(t => t.Reservation)
                    .Where(t => t.Reservation != null && 
                                t.Reservation.ScreeningId == CurrentScreening.Id && 
                                (t.Reservation.Status == null || t.Reservation.Status == "Active"))
                    .Select(t => new 
                    { 
                        t.Id, 
                        t.Row, 
                        t.SeatNumber 
                    })
                    .ToList();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var taken in takenSeats)
                    {
                        var seat = Seats.FirstOrDefault(s => s.Row == taken.Row && s.Number == taken.SeatNumber);
                        if (seat != null)
                        {
                            if (IsEditMode && taken.Id == _ticketToEdit?.Id)
                            {
                                seat.Status = SeatStatus.Editing;
                            }
                            else
                            {
                                seat.Status = SeatStatus.Taken;
                            }
                        }
                    }
                });
            }
        }

        private void GenerateCinemaHall()
        {
            for (int i = 1; i <= RowsCount; i++) RowLabels.Add(i.ToString());

            for (int r = 1; r <= RowsCount; r++)
            {
                for (int n = 1; n <= ColumnsCount; n++)
                {
                    Seats.Add(new Seat { Row = r, Number = n, Status = SeatStatus.Available });
                }
            }
        }

        [RelayCommand]
        private void ToggleSeat(Seat seat)
        {
            if (seat.Status == SeatStatus.Taken) return;
            
            if (IsEditMode)
            {
                if (seat.Status == SeatStatus.Editing) return;

                var previousSelected = Seats.FirstOrDefault(s => s.Status == SeatStatus.Selected);
                if (previousSelected != null && previousSelected != seat)
                    previousSelected.Status = SeatStatus.Available;

                seat.Status = seat.Status == SeatStatus.Selected ? SeatStatus.Available : SeatStatus.Selected;
            }
            else
            {
                if (seat.Status == SeatStatus.Available) seat.Status = SeatStatus.Selected;
                else if (seat.Status == SeatStatus.Selected) seat.Status = SeatStatus.Available;
            }

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var selected = Seats.Where(s => s.Status == SeatStatus.Selected).ToList();
            
            if (IsEditMode)
            {
                SummaryText = selected.Any() 
                    ? $"Nowe miejsce: Rząd {selected[0].Row}, Nr {selected[0].Number}" 
                    : "Wybierz nowe miejsce na mapie";
            }
            else
            {
                var count = selected.Count;
                SummaryText = count > 0 
                    ? $"Wybrano: {count} m. | ~{count * 25:C} PLN" 
                    : "Wybierz miejsca";
            }
        }

        // --- NOWE METODY DLA DRAG-TO-SELECT ---

        // Publiczna metoda do odświeżania podsumowania po zakończeniu przeciągania
        public void RecalculateSummary()
        {
            UpdateSummary();
        }

        // Metoda do ustawiania stanu fotela z poziomu widoku (Code-Behind)
        public void SetSeatSelectionState(Seat seat, bool isSelected)
        {
            // W trybie edycji blokujemy grupowe zaznaczanie
            if (IsEditMode) return;
            if (seat.Status == SeatStatus.Taken) return;

            var newStatus = isSelected ? SeatStatus.Selected : SeatStatus.Available;
            
            if (seat.Status != newStatus)
            {
                seat.Status = newStatus;
            }
        }

        // ----------------------------------------

        [RelayCommand]
        private async Task ConfirmBooking()
        {
            var selectedSeats = Seats.Where(s => s.Status == SeatStatus.Selected).ToList();
            
            if (!selectedSeats.Any())
            {
                CustomMessageBox.Show("Błąd", "Proszę zaznaczyć nowe miejsce na mapie.");
                return;
            }

            if (IsEditMode && _ticketToEdit != null)
            {
                try 
                {
                    using (var context = new CinemaDbContext())
                    using (var transaction = await context.Database.BeginTransactionAsync())
                    {
                        var ticket = await context.Tickets.FindAsync(_ticketToEdit.Id);
                        if (ticket != null)
                        {
                            ticket.Row = selectedSeats[0].Row;
                            ticket.SeatNumber = selectedSeats[0].Number;
                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                    }
                    CustomMessageBox.Show("SUKCES", "Miejsce zostało pomyślnie zmienione!");
                    _dashboardViewModel.CurrentContent = new TicketManagementViewModel(_dashboardViewModel);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    CustomMessageBox.Show("Błąd", "Inny użytkownik zmienił to miejsce. Spróbuj ponownie.");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Błąd", "Nie udało się zmienić miejsca: " + ex.Message);
                }
            }
            else
            {
                _dashboardViewModel.CurrentContent = new TicketSummaryViewModel(
                    _dashboardViewModel, 
                    CurrentScreening, 
                    selectedSeats
                );
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (IsEditMode)
                _dashboardViewModel.CurrentContent = new TicketManagementViewModel(_dashboardViewModel);
            else
                _dashboardViewModel.CurrentContent = new MoviesViewModel(_dashboardViewModel);
        }
    }
}