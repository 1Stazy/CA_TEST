using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using CinemaSystem.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel do zarządzania biletami - zwroty, edycja danych, zmiana miejsc.
    /// </summary>
    /// <remarks>
    /// Pozwala na przeglądanie dostępnych biletów z filtrami oraz wykonywanie operacji:
    /// - Edycja danych klienta (imię, email)
    /// - Zwrot pojedynczego biletu
    /// - Zwrot całej rezerwacji (z bloką do 10 minut przed seansem)
    /// - Zmiana miejsca na bietem
    /// - Ponowne wysłanie biletu (email)
    /// 
    /// Powiązania:
    /// - Pobiera bilety z bazy danych CinemaDbContext.
    /// - Komunikuje się z DashboardViewModel do nawigacji.
    /// </remarks>
    public partial class TicketManagementViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;

        /// <summary>
        /// Tekst do wyszukania biletu (nr transakcji, imię klienta, ID).
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// Data filtrowania biletów (data utworzenia rezerwacji).
        /// </summary>
        [ObservableProperty]
        private DateTime? _selectedDate = DateTime.Today;

        /// <summary>
        /// Przefiltrowana lista biletów do wyświetlenia.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Ticket> _filteredTickets = new();

        /// <summary>
        /// Czy aktualnie edytujemy bilet/rezerwację.
        /// </summary>
        [ObservableProperty]
        private bool _isEditing = false;

        /// <summary>
        /// Imię klienta w trakcie edycji.
        /// </summary>
        [ObservableProperty]
        private string _editCustomerName = string.Empty;

        /// <summary>
        /// Email klienta w trakcie edycji.
        /// </summary>
        [ObservableProperty]
        private string _editCustomerEmail = string.Empty;

        private Ticket? _ticketBeingEdited;
        private List<Ticket> _allTickets = new();

        /// <summary>
        /// Inicjalizuje ViewModel i ładuje listę biletów.
        /// </summary>
        public TicketManagementViewModel(DashboardViewModel dashboardViewModel)
        {
            _dashboardViewModel = dashboardViewModel;
            LoadTickets();
        }

        /// <summary>
        /// Ładuje wszystkie bilety z bazy danych.
        /// </summary>
        private void LoadTickets()
        {
            using (var context = new CinemaDbContext())
            {
                _allTickets = context.Tickets
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r!.Screening)
                            .ThenInclude(s => s!.Hall)
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r!.Screening)
                            .ThenInclude(s => s!.Movie)
                    .OrderByDescending(t => t.Id)
                    .ToList();
                
                FilterTickets();
            }
        }

        /// <summary>
        /// Asynchroniczne ładowanie biletów z bazy danych.
        /// </summary>
        private async Task LoadTicketsAsync()
        {
            using (var context = new CinemaDbContext())
            {
                _allTickets = await context.Tickets
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r!.Screening)
                            .ThenInclude(s => s!.Hall)
                    .Include(t => t.Reservation)
                        .ThenInclude(r => r!.Screening)
                            .ThenInclude(s => s!.Movie)
                    .OrderByDescending(t => t.Id)
                    .ToListAsync();
                
                FilterTickets();
            }
        }

        partial void OnSearchTextChanged(string value) => FilterTickets();
        partial void OnSelectedDateChanged(DateTime? value) => FilterTickets();

        /// <summary>
        /// Uruchamia tryb edycji dla wybranego biletu.
        /// </summary>
        [RelayCommand]
        private void StartEdit(Ticket ticket)
        {
            if (ticket == null || ticket.Reservation == null) return;
            
            _ticketBeingEdited = ticket;
            EditCustomerName = ticket.Reservation.CustomerName;
            EditCustomerEmail = ticket.Reservation.CustomerEmail;
            IsEditing = true; // To pokaże panel edycji w widoku
        }

        /// <summary>
        /// Zapisuje zmiany danych klienta w rezerwacji (async z transakcją).
        /// </summary>
        [RelayCommand]
        private async Task SaveEdit()
        {
            if (_ticketBeingEdited == null) return;

            using (var context = new CinemaDbContext())
            using (var transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == _ticketBeingEdited.ReservationId);
                    if (reservation != null)
                    {
                        reservation.CustomerName = EditCustomerName;
                        reservation.CustomerEmail = EditCustomerEmail;
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        CustomMessageBox.Show("Sukces", "Zaktualizowano dane klienta.");
                    }
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    CustomMessageBox.Show("Błąd", "Inny użytkownik zmienił te dane. Spróbuj ponownie.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    CustomMessageBox.Show("Błąd", $"Błąd zapisywania: {ex.Message}");
                }
            }
            IsEditing = false;
            await LoadTicketsAsync(); // Odśwież listę
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            _ticketBeingEdited = null;
        }

        [RelayCommand]
        private void ResendTicket(Ticket ticket)
        {
            if (ticket?.Reservation == null) return;
            
            // Tutaj w przyszłości będzie logika wysyłania e-maila (SMTP)
            // Na razie symulacja:
            string email = ticket.Reservation.CustomerEmail ?? "brak adresu";
            CustomMessageBox.Show("Wysłano", $"Bilet został ponownie wysłany na adres: {email}");
        }
        private void FilterTickets()
        {
            // Filtrujemy tylko aktywne bilety do wyświetlenia na liście zarzadzania, 
            // lub pokazujemy wszystkie z oznaczeniem statusu (zależnie od preferencji)
            var filtered = _allTickets.Where(t => t.Status != "Cancelled");

            if (SelectedDate.HasValue)
            {
                filtered = filtered.Where(t => 
                    t.Reservation != null && 
                    DateTime.TryParse(t.Reservation.CreatedAt, out var date) && 
                    date.Date == SelectedDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToUpper();
                filtered = filtered.Where(t => 
                    (t.TransactionNumber?.ToUpper().Contains(search) ?? false) || 
                    (t.Reservation?.CustomerName?.ToUpper().Contains(search) ?? false) ||
                    t.Id.ToString() == search
                );
            }

            FilteredTickets = new ObservableCollection<Ticket>(filtered.ToList());
        }

        /// <summary>
        /// Zwracającymi całą rezerwację - blokuje zwroty w ostatnich 10 minutach przed seansem (async).
        /// </summary>
        [RelayCommand]
        private async Task RefundFullReservation(Ticket ticket)
        {
            if (ticket?.Reservation == null) return;

            var resId = ticket.ReservationId;
            var customer = ticket.Reservation.CustomerName ?? "Klient";

            // 1. Pytamy o potwierdzenie
            bool confirm = CustomMessageBox.Show("ZWROT CAŁOŚCI", 
                $"Czy na pewno chcesz zwrócić CAŁE zamówienie klienta: {customer}?\n" +
                "Miejsca zostaną zwolnione, a bilety oznaczone jako zwrócone.", 
                isConfirmation: true);

            if (confirm)
            {
                using (var context = new CinemaDbContext())
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 2. Pobieramy rezerwację z bazy
                        var reservation = await context.Reservations
                            .Include(r => r.Tickets)
                            .Include(r => r.Screening) 
                            .FirstOrDefaultAsync(r => r.Id == resId);

                        if (reservation != null)
                        {
                            // 3. Sprawdzamy czas do seansu (logika 10 minut)
                            if (reservation.Screening != null)
                            {
                                var timeToScreening = reservation.Screening.Start - DateTime.Now;

                                if (timeToScreening.TotalMinutes < 10)
                                {
                                    System.Windows.MessageBox.Show(
                                        "Nie można zwrócić zamówienia.\nDo seansu zostało mniej niż 10 minut (lub seans już się odbył).",
                                        "Błąd zwrotu",
                                        System.Windows.MessageBoxButton.OK,
                                        System.Windows.MessageBoxImage.Warning);
                                    return; // Przerywamy operację
                                }
                            }

                            // 4. Jeśli czas jest OK, anulujemy wszystko
                            reservation.Status = "Cancelled";
                            
                            foreach (var t in reservation.Tickets)
                            {
                                t.Status = "Cancelled";
                            }

                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            CustomMessageBox.Show("SUKCES", "Całe zamówienie zostało anulowane.");
                            await LoadTicketsAsync();
                        }
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                    {
                        await transaction.RollbackAsync();
                        CustomMessageBox.Show("Błąd", "Konflikt dostępu: inny użytkownik zmienił dane. Spróbuj ponownie.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        CustomMessageBox.Show("Błąd", $"Błąd zwrotu: {ex.Message}");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task RefundSingleTicket(Ticket ticket)
        {
            if (ticket == null) return;

            // 1. Najpierw pytamy użytkownika o potwierdzenie
            bool confirm = CustomMessageBox.Show("POTWIERDZENIE", 
                $"Czy zwrócić bilet: Rząd {ticket.Row}, Miejsce {ticket.SeatNumber}?", isConfirmation: true);

            if (confirm)
            {
                using (var context = new CinemaDbContext())
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 2. Pobieramy bilet z bazy (musimy dołączyć Screening, żeby znać datę seansu!)
                        var ticketInDb = await context.Tickets
                            .Include(t => t.Reservation)
                                .ThenInclude(r => r!.Screening) // Ważne: musimy załadować seans
                            .FirstOrDefaultAsync(t => t.Id == ticket.Id);

                        if (ticketInDb != null)
                        {
                            // 3. Tutaj sprawdzamy czas (zmienna ticketInDb już istnieje)
                            if (ticketInDb.Reservation?.Screening != null)
                            {
                                var screeningTime = ticketInDb.Reservation.Screening.Start;
                                var timeToScreening = screeningTime - DateTime.Now;

                                if (timeToScreening.TotalMinutes < 10)
                                {
                                    System.Windows.MessageBox.Show(
                                        "Nie można zwrócić biletu.\nDo seansu zostało mniej niż 10 minut (lub seans już się odbył).", 
                                        "Błąd zwrotu", 
                                        System.Windows.MessageBoxButton.OK, 
                                        System.Windows.MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            // 4. Jeśli czas jest OK, oznaczamy jako zwrócony
                            ticketInDb.Status = "Cancelled";
                            
                            await context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            await LoadTicketsAsync();
                            CustomMessageBox.Show("SUKCES", "Bilet został oznaczony jako zwrócony.");
                        }
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                    {
                        await transaction.RollbackAsync();
                        CustomMessageBox.Show("Błąd", "Konflikt dostępu: inny użytkownik zmienił bilet. Spróbuj ponownie.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        CustomMessageBox.Show("Błąd", $"Błąd zwrotu biletu: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Zmienia miejsce dla wybranego biletu - otwiera SeatSelectionViewModel w trybie edycji.
        /// </summary>
        [RelayCommand]
        private void ChangeSeat(Ticket ticket)
        {
            if (ticket?.Reservation?.Screening == null) return;

            _dashboardViewModel.CurrentContent = new SeatSelectionViewModel(
                ticket.Reservation.Screening, 
                _dashboardViewModel, 
                ticketToEdit: ticket);
        }
    }
}