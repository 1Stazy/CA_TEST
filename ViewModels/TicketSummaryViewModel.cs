using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using CinemaSystem.Desktop.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// Klasa pomocnicza reprezentująca pojedynczy bilet w koszyku.
    /// </summary>
    public partial class TicketCartItem : ObservableObject
    {
        /// <summary>
        /// Miejsce w sali dla tego biletu.
        /// </summary>
        public Seat Seat { get; }

        private readonly Screening _screening;
        private readonly Action _recalculateTotalAction;

        /// <summary>
        /// Czy ten bilet jest wybrany do edycji zniżki.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Wybrany typ biletu (Normalny, Ulgowy, Student itp.).
        /// </summary>
        [ObservableProperty]
        private TicketTypeDefinition _selectedType;

        /// <summary>
        /// Cena biletu po zastosowaniu typu i rabatów.
        /// </summary>
        [ObservableProperty]
        private decimal _price;

        /// <summary>
        /// Inicjalizuje element koszyka z miejscem, seansem i typem biletu.
        /// </summary>
        public TicketCartItem(Seat seat, Screening screening, TicketTypeDefinition defaultType, Action recalculateTotalAction)
        {
            Seat = seat;
            _screening = screening;
            _recalculateTotalAction = recalculateTotalAction;
            SelectedType = defaultType;
        }

        /// <summary>
        /// Przelicza cenę biletu po zmianie typu.
        /// </summary>
        partial void OnSelectedTypeChanged(TicketTypeDefinition value)
        {
            if (value == null) return;
            Price = _screening.Price * (decimal)value.PriceMultiplier;
            _recalculateTotalAction?.Invoke();
        }
    }

    /// <summary>
    /// ViewModel podsumowania rezerwacji - ostatni krok przed finalizacją płatności.
    /// </summary>
    /// <remarks>
    /// Zarządza koszykiem biletów, rabatami, kodami promocyjnymi i finalizacją transakcji.
    /// Generuje PDF z biletami i wysyła do klienta.
    /// 
    /// Powiązania:
    /// - CartItems: lista wybranych miejsc z typami biletów.
    /// - Rabaty: kody promocyjne (procentowe lub kwotowe).
    /// - PDF: generuje dokumenty na pulpicie.
    /// </remarks>
    public partial class TicketSummaryViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly Screening _screening;

        /// <summary>
        /// Koszyk biletów do rezerwacji.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TicketCartItem> _cartItems = new();

        /// <summary>
        /// Dostępne typy biletów do wyboru.
        /// </summary>
        [ObservableProperty]
        private List<TicketTypeDefinition> _availableTicketTypes = new();

        /// <summary>
        /// Całkowita cena przed rabatami.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FinalPrice))]
        private decimal _totalPrice;

        /// <summary>
        /// Nazwa klienta dokonującego rezerwacji.
        /// </summary>
        [ObservableProperty]
        private string _customerName = "";

        /// <summary>
        /// Email klienta do wysłania biletu.
        /// </summary>
        [ObservableProperty]
        private string _customerEmail = "";

        /// <summary>
        /// Wpisany kod promocyjny.
        /// </summary>
        [ObservableProperty]
        private string _promoCodeInput = "";

        private PromoCode? _appliedPromoCode;

        /// <summary>
        /// Kwota rabatu (stały lub procentowy).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FinalPrice))]
        [NotifyPropertyChangedFor(nameof(DiscountVisibility))]
        private decimal _discountAmount = 0;

        /// <summary>
        /// Czy transakcja się zakończyła.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotFinished))]
        private bool _isTransactionFinished = false;

        /// <summary>
        /// Czy trwa przetwarzanie transakcji.
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Wartość paska postępu (0-100).
        /// </summary>
        [ObservableProperty]
        private double _progressValue;

        /// <summary>
        /// Widoczność informacji o rabacie.
        /// </summary>
        public Visibility DiscountVisibility => DiscountAmount > 0 ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Cena końcowa do zapłaty (TotalPrice - DiscountAmount).
        /// </summary>
        public decimal FinalPrice => Math.Max(0, TotalPrice - DiscountAmount);

        /// <summary>
        /// Czy transakcja trwa (odwrotnie od IsTransactionFinished).
        /// </summary>
        public bool IsNotFinished => !IsTransactionFinished;

        public string MovieTitle => _screening.Movie?.Title ?? "Tytuł";
        public string ScreeningTime => _screening.Start.ToString("dd.MM.yyyy HH:mm");
        public string HallName => _screening.Hall?.Name ?? "Sala";
        public string PosterUri => _screening.Movie?.PosterUri ?? "";

        private Reservation? _lastSavedReservation;

        /// <summary>
        /// Inicjalizuje podsumowanie zamówienia z wybranym seansem i miejscami.
        /// </summary>
        public TicketSummaryViewModel(DashboardViewModel dashboard, Screening screening, List<Seat> selectedSeats)
        {
            _dashboardViewModel = dashboard;
            _screening = screening;

            LoadTicketTypes();

            var defaultType = AvailableTicketTypes.FirstOrDefault(t => t.Name == "Normalny")
                              ?? AvailableTicketTypes.FirstOrDefault();

            if (defaultType == null)
            {
                CustomMessageBox.Show("Błąd", "Brak zdefiniowanych typów biletów w bazie danych!");
            }
            else
            {
                foreach (var seat in selectedSeats)
                {
                    CartItems.Add(new TicketCartItem(seat, _screening, defaultType, RecalculateTotal));
                }
            }

            RecalculateTotal();
        }

        /// <summary>
        /// Ładuje dostępne typy biletów z bazy danych.
        /// </summary>
        private void LoadTicketTypes()
        {
            try
            {
                using var context = new CinemaDbContext();
                AvailableTicketTypes = context.TicketTypes
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.PriceMultiplier)
                    .ToList();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("BŁĄD SYSTEMU", "Wystąpił problem podczas ładowania typów biletów:\n" + ex.Message);
            }
        }

        /// <summary>
        /// Przelicza sumę koszyka i aktualizuje rabat procentowy.
        /// Jeśli kod promocyjny ustawiony na procent, rabat zmieni się automatycznie.
        /// </summary>
        private void RecalculateTotal()
        {
            TotalPrice = CartItems.Sum(x => x.Price);
            
            // Jeśli mamy aktywny kod procentowy, rabat musi się zmienić wraz z ceną biletów
            if (_appliedPromoCode != null && _appliedPromoCode.IsPercentage)
            {
                DiscountAmount = TotalPrice * (decimal)_appliedPromoCode.DiscountValue;
            }
            // Jeśli kod jest kwotowy (np. 10zł), DiscountAmount pozostaje stały
            
            OnPropertyChanged(nameof(FinalPrice)); // Wymuszenie odświeżenia UI
        }

        /// <summary>
        /// Validuje i stosuje kod promocyjny - oblicza rabat procentowy lub kwotowy.
        /// </summary>
        [RelayCommand]
        private void ApplyPromoCode()
        {
            if (string.IsNullOrWhiteSpace(PromoCodeInput)) return;

            try
            {
                using var context = new CinemaDbContext();
                
                // Pobieramy kod z bazy (uwzględniając wielkość liter lub nie, zależnie od bazy)
                var code = context.PromoCodes
                    .FirstOrDefault(p => p.Code == PromoCodeInput && p.IsActive);

                if (code != null)
                {
                    _appliedPromoCode = code;

                    if (code.IsPercentage)
                    {
                        // np. 0.2 to 20%
                        // Rzutujemy double na decimal
                        DiscountAmount = TotalPrice * (decimal)code.DiscountValue;
                        CustomMessageBox.Show("SUKCES", $"Naliczono rabat: {(code.DiscountValue * 100):0}%");
                    }
                    else
                    {
                        // np. 10.0 to 10 PLN
                        DiscountAmount = (decimal)code.DiscountValue;
                        CustomMessageBox.Show("SUKCES", $"Naliczono rabat: {code.DiscountValue:N2} zł");
                    }
                    
                    // Odświeżamy UI
                    OnPropertyChanged(nameof(FinalPrice));
                    OnPropertyChanged(nameof(DiscountVisibility));
                }
                else
                {
                    DiscountAmount = 0;
                    _appliedPromoCode = null;
                    CustomMessageBox.Show("BŁĄD", "Kod nieprawidłowy lub nieaktywny.");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("BŁĄD BAZY", ex.Message);
            }
        }

        /// <summary>
        /// Stosuje wybrany typ biletu do zaznaczonych pozycji w koszyku.
        /// </summary>
        [RelayCommand]
        private void ApplyDiscount(TicketTypeDefinition selectedType)
        {
            var selectedItems = CartItems.Where(x => x.IsSelected).ToList();

            if (!selectedItems.Any())
            {
                CustomMessageBox.Show("BRAK WYBORU", "Zaznacz bilety na liście, aby zmienić im zniżkę.");
                return;
            }

            foreach (var item in selectedItems)
            {
                item.SelectedType = selectedType;
                item.IsSelected = false;
            }

            RecalculateTotal();
        }

        /// <summary>
        /// Finalizuje transakcję: tworzy rezerwację, bilety, rozdziela rabaty, generuje PDF.
        /// </summary>
        [RelayCommand]
        private async Task FinalizeTransaction()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                CustomMessageBox.Show("BRAK DANYCH", "Proszę podać imię i nazwisko.");
                return;
            }

            IsBusy = true;
            ProgressValue = 10;

            try
            {
                var result = await CreateReservationWithTransactionAsync();
                
                if (result == null)
                    return;

                ProgressValue = 50;

                string pdfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Bilet_{result.Res.Id}.pdf");
                await Task.Run(() =>
                {
                    var pdfService = new TicketPdfGenerator();
                    result.Res.Screening = _screening;
                    pdfService.GenerateTicketPdf(result.Res, result.Tickets, pdfPath);
                });

                ProgressValue = 100;
                IsTransactionFinished = true;
                _lastSavedReservation = result.Res;

                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    new BookingConfirmationWindow(pdfPath).ShowDialog();
                });
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("BŁĄD TRANSAKCJI", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Cofnięcie do poprzedniego widoku lub edycji miejsc.
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            if (!IsBusy) _dashboardViewModel.CurrentContent = new SeatSelectionViewModel(_screening, _dashboardViewModel);
        }

        /// <summary>
        /// Tworzy rezerwację z transakcją (ACID compliance dla PostgreSQL).
        /// Obsługuje ConcurrencyException jeśli inny użytkownik zmienił dane.
        /// </summary>
        private async Task<dynamic?> CreateReservationWithTransactionAsync()
        {
            using var context = new CinemaDbContext();
            
            // Włącz transakcję - wszystko albo nic
            using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                var res = new Reservation
                {
                    ScreeningId = _screening.Id,
                    CustomerName = CustomerName,
                    CustomerEmail = CustomerEmail,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "Active"
                };
                context.Reservations.Add(res);
                await context.SaveChangesAsync();

                var tickets = new List<Ticket>();
                string tid = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                decimal discountRemaining = 0;
                bool isPercentage = false;

                if (_appliedPromoCode != null)
                {
                    isPercentage = _appliedPromoCode.IsPercentage;
                    if (!isPercentage)
                        discountRemaining = (decimal)_appliedPromoCode.DiscountValue;
                }

                foreach (var item in CartItems)
                {
                    decimal finalTicketPrice = item.Price;

                    if (_appliedPromoCode != null)
                    {
                        if (isPercentage)
                        {
                            decimal multiplier = 1.0m - (decimal)_appliedPromoCode.DiscountValue;
                            finalTicketPrice = item.Price * multiplier;
                        }
                        else
                        {
                            if (discountRemaining > 0)
                            {
                                decimal deduction = Math.Min(finalTicketPrice, discountRemaining);
                                finalTicketPrice -= deduction;
                                discountRemaining -= deduction;
                            }
                        }
                    }

                    var t = new Ticket
                    {
                        ReservationId = res.Id,
                        Row = item.Seat.Row,
                        SeatNumber = item.Seat.Number,
                        PricePaid = Math.Max(0, finalTicketPrice),
                        TicketType = item.SelectedType.Name,
                        TransactionNumber = tid,
                        Status = "Active",
                        IssuedAt = DateTime.Now.ToString("g")
                    };
                    context.Tickets.Add(t);
                    tickets.Add(t);
                }
                
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new { Res = res, Tickets = tickets };
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                System.Windows.MessageBox.Show("Konflikt dostępu: inny użytkownik zmienił dane. Spróbuj ponownie.", "Błąd współbieżności");
                return null;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Windows.MessageBox.Show($"Błąd bazy danych: {ex.Message}", "Błąd");
                return null;
            }
        }
        [RelayCommand]
        private void FinishAndExit()
        {
            if (!IsBusy) _dashboardViewModel.CurrentContent = new MoviesViewModel(_dashboardViewModel);
        }

        /// <summary>
        /// Otwiera bilet PDF z ostatniej transakcji w Eksploratorem Plików.
        /// </summary>
        [RelayCommand]
        private void DownloadVoucher()
        {
            if (_lastSavedReservation != null)
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Bilet_{_lastSavedReservation.Id}.pdf");
                if (File.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true });
            }
        }
    }
}