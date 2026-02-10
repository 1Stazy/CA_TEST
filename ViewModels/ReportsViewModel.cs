using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Services; // <--- Dodano namespace serwisu
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// Statystyka sprzedaży dla jednego dnia tygodnia.
    /// </summary>
    public class DayOfWeekStat
    {
        /// <summary>Skrócona polska nazwa dnia (PN, WT, ŚR itd.).</summary>
        public string DayName { get; set; } = string.Empty;
        /// <summary>Liczba sprzedanych biletów w tym dniu.</summary>
        public int Value { get; set; }
        /// <summary>Znormalizowana wartość do skalowania wysokości słupka (0-100).</summary>
        public double NormalizedValue { get; set; }
        /// <summary>Czy jest to dzień weekendu (sobota lub niedziela).</summary>
        public bool IsWeekend { get; set; }
    }

    /// <summary>
    /// Element wykresu - para (etykieta, wartość).
    /// </summary>
    public class ChartItem
    {
        /// <summary>Etykieta na osi X (godzina, dzień, itp.).</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Wartość do wykresu.</summary>
        public int Value { get; set; }
        /// <summary>Znormalizowana wysokość dla rysowania.</summary>
        public double NormalizedValue { get; set; }
    }

    /// <summary>
    /// Statystyka popularności gatunku filmowego.
    /// </summary>
    public class GenreStat
    {
        /// <summary>Nazwa gatunku (Akcja, Komedia, itp.).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Liczba sprzedanych biletów tego gatunku.</summary>
        public int Count { get; set; }
        /// <summary>Procent udziału w całkowitej sprzedaży.</summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Statystyka obłożenia jednej sali kinowej.
    /// </summary>
    public class HallStat
    {
        /// <summary>Nazwa sali (Sala 1, Sala 2, itp.).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Procent obłożenia sali (0-100%).</summary>
        public double Occupancy { get; set; }
        /// <summary>Sformatowany tekst obłożenia do wyświetlenia.</summary>
        public string OccupancyText => $"{Occupancy:F1}%";
    }

    /// <summary>
    /// Ranking filmów wg przychodu (kasa pochodząca ze sprzedaży biletów).
    /// </summary>
    public class MovieRank
    {
        /// <summary>Pozycja w rankingu (1, 2, 3, itd.).</summary>
        public int Rank { get; set; }
        /// <summary>Tytuł filmu.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Przychód z biletów tego filmu.</summary>
        public decimal Revenue { get; set; }
        /// <summary>Liczba sprzedanych biletów.</summary>
        public int Tickets { get; set; }
    }

    /// <summary>
    /// Ranking filmów wg wolumenu - liczby sprzedanych biletów.
    /// </summary>
    public class MovieTicketStat
    {
        /// <summary>Pozycja w rankingu.</summary>
        public int Rank { get; set; }
        /// <summary>Tytuł filmu.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Liczba sprzedanych biletów tego filmu.</summary>
        public int TicketCount { get; set; }
    }

    /// <summary>
    /// Statystyka rozpowszechnienia typu biletu (Normalny, Ulgowy, Student itd.).
    /// </summary>
    public class TicketTypeStat
    {
        /// <summary>Nazwa typu biletu.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Liczba biletów tego typu sprzedanych.</summary>
        public int Count { get; set; }
        /// <summary>Procent udziału w ogólnej sprzedaży.</summary>
        public double Percentage { get; set; }
        /// <summary>Sformatowany tekst procentu do wyświetlenia.</summary>
        public string DisplayPercentage => $"{Percentage:F1}%";
    }

    /// <summary>
    /// ViewModel raportów i statystyk kinowych.
    /// </summary>
    /// <remarks>
    /// Zarządza wszystkimi danymi analitycznymi: KPI (przychody, bilety, obłożenie),
    /// rankingi filmów, mapy ciepła sprzedaży, statystyki gatunków i sal.
    /// Wspiera drill-down - klikniecie w dzień pokazuje szczegóły godzinowe.
    /// 
    /// Powiązania:
    /// - Pobiera dane z Tickets z powiązanymi Screening i Rezerwacjami.
    /// - Wykorzystuje ReportSettingsService do zapamiętywania wybranego widoku.
    /// - PdfReportService generuje raport PDF.
    /// </remarks>
    public partial class ReportsViewModel : ObservableObject

    {
        [ObservableProperty] private ObservableCollection<MovieTicketStat> _topMoviesByTickets = new();

        // NOWA FLAGA WIDOCZNOŚCI
        [ObservableProperty] private bool _showMoviesTicketCount = true;

        [ObservableProperty] private ObservableCollection<DayOfWeekStat> _weeklySales = new();
        [ObservableProperty] private bool _showWeeklyChart = true; // Flaga do konfiguracji
        // --- ZAKRES DAT ---
        [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-7);
        [ObservableProperty] private DateTime _endDate = DateTime.Today;

        // --- DANE ---
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private int _soldTickets;
        [ObservableProperty] private int _returnedTickets;
        [ObservableProperty] private double _avgOccupancy;
        [ObservableProperty] private decimal _avgTicketPrice;
        [ObservableProperty] private string _revenueTrend = "0%";
        [ObservableProperty] private bool _isRevenueUp;

        [ObservableProperty] private ObservableCollection<ChartItem> _hourlySales = new();
        [ObservableProperty] private ObservableCollection<GenreStat> _genrePopularity = new();
        [ObservableProperty] private ObservableCollection<HallStat> _hallStats = new();
        [ObservableProperty] private ObservableCollection<MovieRank> _topMovies = new();

        // --- ZARZĄDZANIE WIDOKIEM ---
        [ObservableProperty] private bool _isConfigOpen;

        // Flagi widoczności
        [ObservableProperty] private bool _showRevenue = true;
        [ObservableProperty] private bool _showSoldTickets = true;
        [ObservableProperty] private bool _showReturns = true;
        [ObservableProperty] private bool _showOccupancy = true;
        [ObservableProperty] private bool _showATP = true;

        [ObservableProperty] private bool _showHourlyChart = true;
        [ObservableProperty] private bool _showGenreChart = true;
        [ObservableProperty] private bool _showHallStats = true;
        [ObservableProperty] private bool _showTopMovies = true;
        [ObservableProperty] private ObservableCollection<TicketTypeStat> _ticketTypeStats = new();
        [ObservableProperty] private bool _showTicketTypeChart = true; // Flaga widoczności (opcjonalnie do configu)

        // Zmieniamy na public property, żeby PDF mógł z tego korzystać
        public List<Ticket> CachedActiveTickets { get; private set; } = new();



        public ReportsViewModel()
        {
            // 1. Najpierw ładujemy zapisany układ kafelków
            LoadLayoutSettings();

            // 2. Potem ładujemy dane z bazy
            RefreshData();
        }


        /// <summary>
        /// Wczytuje zapisane ustawienia widoczności kafelków z pliku.
        /// </summary>
        private void LoadLayoutSettings()
        {
            var settings = ReportSettingsService.Load();
            ShowRevenue = settings.ShowRevenue;
            ShowATP = settings.ShowATP;
            ShowSoldTickets = settings.ShowSoldTickets;
            ShowReturns = settings.ShowReturns;
            ShowOccupancy = settings.ShowOccupancy;
            ShowHourlyChart = settings.ShowHourlyChart;
            ShowWeeklyChart = settings.ShowWeeklyChart;
            ShowGenreChart = settings.ShowGenreChart;
            ShowHallStats = settings.ShowHallStats;
            ShowTopMovies = settings.ShowTopMovies;
            ShowMoviesTicketCount = settings.ShowMoviesTicketCount;
            ShowTicketTypeChart = settings.ShowTicketTypeChart;
        }
        /// <summary>
        /// Generuje raport PDF z aktualnymi statystykami do pobrania na komputer.
        /// </summary>
        [RelayCommand]
        private void GeneratePdf()
        {
            // 1. Konfiguracja okna zapisu pliku
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"Raport_Kino_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".pdf",
                Filter = "Dokument PDF (.pdf)|*.pdf"
            };

            // 2. Otwarcie okna zapisu
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 3. Wywołanie serwisu z DWOMA parametrami
                    PdfReportService.GeneratePdf(this, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    // Wyświetlenie błędu w Twoim CustomMessageBox
                    string title = "BŁĄD PDF";
                    string message = ex.Message;

                    if (ex.InnerException is System.IO.IOException)
                    {
                        title = "PLIK OTWARTY";
                        message = "Zamknij plik PDF w innym programie przed zapisem.";
                    }

                    CinemaSystem.Desktop.Views.CustomMessageBox.Show(title, message);
                }
            }
        }
        [RelayCommand]

        public void ToggleConfig()
        {
            // Jeśli właśnie ZAMYKAMY okno (czyli było otwarte), to ZAPISUJEMY ustawienia
            if (IsConfigOpen)
            {
                var settings = new ReportSettingsData
                {
                    ShowRevenue = ShowRevenue,
                    ShowATP = ShowATP,
                    ShowSoldTickets = ShowSoldTickets,
                    ShowReturns = ShowReturns,
                    ShowOccupancy = ShowOccupancy,
                    ShowHourlyChart = ShowHourlyChart,
                    ShowWeeklyChart = ShowWeeklyChart,
                    ShowGenreChart = ShowGenreChart,
                    ShowHallStats = ShowHallStats,
                    ShowTopMovies = ShowTopMovies,
                    ShowMoviesTicketCount = ShowMoviesTicketCount,
                    ShowTicketTypeChart = ShowTicketTypeChart
                };
                ReportSettingsService.Save(settings);
            }

            IsConfigOpen = !IsConfigOpen;
        }

        /// <summary>
        /// Odświerza wszystkie dane raportów i przelicza statystyki.
        /// </summary>
        [RelayCommand]
        public void RefreshData()
        {
            LoadStats();
        }

        // ... Reszta metod LoadStats, Calculate... bez zmian (skopiuj z poprzedniego kodu lub zostaw jak masz) ...
        // Poniżej wklejam skróconą wersję LoadStats dla kompletności pliku, ale logika jest ta sama
        /// <summary>
        /// Przygotowuje dane sprzedazy rozlozonych po dniach tygodnia do wykresu.
        /// </summary>
        private void PrepareWeeklyChart(List<Ticket> tickets)
        {
            WeeklySales.Clear();
            if (!tickets.Any()) return;

            // Grupujemy po dniu tygodnia
            var salesByDay = tickets
                .Where(t => t.Reservation?.Screening != null)
                .GroupBy(t => t.Reservation!.Screening!.Start.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToList();

            int max = salesByDay.Any() ? salesByDay.Max(x => x.Count) : 1;

            // Kolejność dni w Polsce: Poniedziałek (1) -> Niedziela (0)
            var polishOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            foreach (var day in polishOrder)
            {
                var data = salesByDay.FirstOrDefault(x => x.Day == day);
                int count = data?.Count ?? 0;

                WeeklySales.Add(new DayOfWeekStat
                {
                    DayName = TranslateDay(day), // Skrócona nazwa PL
                    Value = count,
                    NormalizedValue = (double)count / max * 100, // Skala do 100
                    IsWeekend = day == DayOfWeek.Saturday || day == DayOfWeek.Sunday
                });
            }
        }

        /// <summary>
        /// Tłumaczy DayOfWeek na polskie skrócone nazwy (PN, WT, ŚR itd.).
        /// </summary>
        private string TranslateDay(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "PN",
                DayOfWeek.Tuesday => "WT",
                DayOfWeek.Wednesday => "ŚR",
                DayOfWeek.Thursday => "CZ",
                DayOfWeek.Friday => "PT",
                DayOfWeek.Saturday => "SB",
                DayOfWeek.Sunday => "ND",
                _ => "??"
            };
        }
        /// <summary>
        /// Ładuje wszystkie statystyki z bazy danych dla wybranego zakresu dat.
        /// Oblicza KPI, rankingi filmow, statystyki gatunkow, sal i typow biletow.
        /// </summary>
        private void LoadStats()
        {
            try
            {
                using (var context = new CinemaDbContext())
                {
                    // 1. Pobieranie biletów
                    var allTickets = context.Tickets.AsNoTracking()
                        .Include(t => t.Reservation)
                        .Include(t => t.Reservation!.Screening)
                        .ThenInclude(s => s!.Movie)
                        .ToList();

                    // 2. Filtrowanie po dacie
                    var ticketsInRange = allTickets.Where(t =>
                        t.Reservation != null &&
                        DateTime.TryParse(t.Reservation.CreatedAt, out var pDate) &&
                        pDate.Date >= StartDate.Date &&
                        pDate.Date <= EndDate.Date).ToList();

                    var active = ticketsInRange.Where(t => t.Status != "Cancelled").ToList();

                    // === DODAJ TĘ LINIJKĘ TUTAJ ===
                    CachedActiveTickets = active;
                    // ==============================

                    var cancelled = ticketsInRange.Where(t => t.Status == "Cancelled").ToList();

                    //

                    // 3. KPI (Główne liczniki)
                    SoldTickets = active.Count;
                    ReturnedTickets = cancelled.Count;
                    TotalRevenue = active.Sum(t => t.PricePaid);
                    AvgTicketPrice = SoldTickets > 0 ? TotalRevenue / SoldTickets : 0;

                    // 4. Gatunki
                    var genreGroups = active.Where(t => t.Reservation?.Screening?.Movie != null)
                        .GroupBy(t => t.Reservation!.Screening!.Movie!.Genre)
                        .Select(g => new GenreStat { Name = g.Key ?? "Inne", Count = g.Count(), Percentage = (double)g.Count() / (active.Count > 0 ? active.Count : 1) * 100 })
                        .OrderByDescending(x => x.Count).ToList();
                    GenrePopularity = new ObservableCollection<GenreStat>(genreGroups);

                    // =========================================================
                    // 5. NAPRAWA: RANKING PRZYCHODOWY (TopMovies) - TO CI ZNIKNĘŁO
                    // =========================================================
                    var revenueList = active
                        .Where(t => t.Reservation?.Screening?.Movie != null)
                        .GroupBy(t => t.Reservation!.Screening!.Movie!.Title)
                        .Select((g) => new MovieRank
                        {
                            Title = g.Key ?? "Nieznany",
                            Tickets = g.Count(),
                            Revenue = g.Sum(t => t.Reservation!.Screening!.Price)
                        })
                        .OrderByDescending(x => x.Revenue) // Sortujemy po kasie
                        .Take(30)
                        .ToList();

                    for (int i = 0; i < revenueList.Count; i++) revenueList[i].Rank = i + 1;
                    TopMovies = new ObservableCollection<MovieRank>(revenueList);

                    // =========================================================
                    // 6. RANKING WOLUMENOWY (TopMoviesByTickets) - NOWY
                    // =========================================================
                    var volumeList = active
                        .Where(t => t.Reservation?.Screening?.Movie != null)
                        .GroupBy(t => t.Reservation!.Screening!.Movie!.Title)
                        .Select((g) => new MovieTicketStat
                        {
                            Title = g.Key ?? "Nieznany",
                            TicketCount = g.Count()
                        })
                        .OrderByDescending(x => x.TicketCount) // Sortujemy po ilości
                        .Take(30)
                        .ToList();

                    for (int i = 0; i < volumeList.Count; i++) volumeList[i].Rank = i + 1;
                    TopMoviesByTickets = new ObservableCollection<MovieTicketStat>(volumeList);


                    // 7. Reszta wykresów
                    CalculateHallStats(context, active);
                    PrepareHourlyChart(active);
                    PrepareWeeklyChart(active);
                    CalculateTrend(allTickets);
                    CalculateOccupancy(context);
                    CalculateTicketTypes(active);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd LoadStats: {ex.Message}");
            }
        }

        /// <summary>
        /// Oblicza statystyki rozkładu typów biletów (Normalny, Ulgowy, Student itd.).
        /// </summary>
        private void CalculateTicketTypes(List<Ticket> activeTickets)
        {
            if (!activeTickets.Any())
            {
                TicketTypeStats.Clear();
                return;
            }

            int total = activeTickets.Count;

            var stats = activeTickets
                .GroupBy(t => t.TicketType ?? "Nieokreślony") // Grupowanie po typie
                .Select(g => new TicketTypeStat
                {
                    Name = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / total * 100.0
                })
                .OrderByDescending(x => x.Count) // Najpopularniejsze na górze
                .ToList();

            TicketTypeStats = new ObservableCollection<TicketTypeStat>(stats);
        }
        /// <summary>
        /// Oblicza obłożenie (frekwencję) każdej sali kinowej.
        /// </summary>
        private void CalculateHallStats(CinemaDbContext context, List<Ticket> active)
        {
            var halls = context.Halls.AsNoTracking().ToList();
            var list = new List<HallStat>();
            foreach (var h in halls)
            {
                var scrCount = context.Screenings.Count(s => s.HallId == h.Id && s.Start >= StartDate && s.Start <= EndDate);
                int cap = h.Rows * h.SeatsPerRow * scrCount;
                int sold = active.Count(t => t.Reservation?.Screening?.HallId == h.Id);
                list.Add(new HallStat { Name = h.Name, Occupancy = cap > 0 ? (double)sold / cap * 100 : 0 });
            }
            HallStats = new ObservableCollection<HallStat>(list.OrderByDescending(x => x.Occupancy));
        }


        /// <summary>
        /// Przygotowuje dane godzinowych sprzedaży do wykresu słupkowego.
        /// </summary>
        private void PrepareHourlyChart(List<Ticket> tickets)
        {
            HourlySales.Clear();
            var groups = tickets.Where(t => t.Reservation?.Screening != null)
                .GroupBy(t => t.Reservation!.Screening!.Start.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() }).OrderBy(x => x.Hour).ToList();
            int max = groups.Any() ? groups.Max(x => x.Count) : 1;
            foreach (var g in groups) HourlySales.Add(new ChartItem { Label = $"{g.Hour}:00", Value = g.Count, NormalizedValue = (double)g.Count / max * 180 });
        }

        /// <summary>
        /// Oblicza trend przychodu porównując obecny okres z poprzednim.
        /// </summary>
        private void CalculateTrend(List<Ticket> all)
        {
            var days = (EndDate - StartDate).Days + 1;
            var start = StartDate.AddDays(-days);
            var end = StartDate.AddDays(-1);
            var prevRev = all.Where(t => t.Status != "Cancelled" && DateTime.TryParse(t.Reservation?.CreatedAt, out var d) && d.Date >= start.Date && d.Date <= end.Date).Sum(t => t.Reservation?.Screening?.Price ?? 0);
            if (prevRev > 0) { var ch = ((TotalRevenue - prevRev) / prevRev) * 100; RevenueTrend = $"{(ch >= 0 ? "+" : "")}{ch:F1}%"; IsRevenueUp = ch >= 0; }
            else { RevenueTrend = "Nowy okres"; IsRevenueUp = true; }
        }

        /// <summary>
        /// Oblicza średnie obłożenie sali w wybranym okresie.
        /// </summary>
        private void CalculateOccupancy(CinemaDbContext ctx)
        {
            var scr = ctx.Screenings.Include(s => s.Hall).Where(s => s.Start >= StartDate && s.Start <= EndDate).ToList();
            int cap = scr.Sum(s => s.Hall?.Rows * s.Hall?.SeatsPerRow ?? 0);
            using (var c = new CinemaDbContext())
            {
                var sold = c.Tickets.Count(t => t.Status != "Cancelled" && t.Reservation != null && t.Reservation.Screening != null && t.Reservation.Screening.Start >= StartDate && t.Reservation.Screening.Start <= EndDate);
                AvgOccupancy = cap > 0 ? (double)sold / cap * 100 : 0;
            }
        }
        /// <summary>
        /// Czy panel szczegółów dnia (drill-down) jest otwarty.
        /// </summary>
        [ObservableProperty] private bool _isDayDetailsOpen;
        /// <summary>
        /// Nazwa wybranego dnia w panelu drill-down.
        /// </summary>
        [ObservableProperty] private string _selectedDayName = "";

        /// <summary>
        /// Godzinowe sprzedaże dla wybranego konkretnego dnia.
        /// </summary>
        [ObservableProperty] private ObservableCollection<ChartItem> _selectedDayHourlySales = new();

        /// <summary>
        /// Otwiera panel szczegółów dla wybranego dnia tygodnia.
        /// </summary>
        [RelayCommand]
        public void OpenDayDetails(DayOfWeekStat dayStat)
        {
            if (dayStat == null) return;

            // 1. Ustawiamy tytuł
            SelectedDayName = $"SZCZEGÓŁY: {dayStat.DayName} (Pełna nazwa)";

            // 2. Pobieramy dane tylko dla tego dnia
            // Musimy mieć dostęp do surowych biletów lub je ponownie pobrać. 
            // Najprościej: wywołajmy metodę pomocniczą, która przefiltruje dane.
            LoadHourlyStatsForDay(dayStat.DayName);

            // 3. Otwieramy popup
            IsDayDetailsOpen = true;
        }

        /// <summary>
        /// Zamyka panel szczegółów dnia.
        /// </summary>
        [RelayCommand]
        public void CloseDayDetails()
        {
            IsDayDetailsOpen = false;
        }

        /// <summary>
        /// Ładuje statystyki godzinowe dla konkretnego dnia tygodnia.
        /// </summary>
        private void LoadHourlyStatsForDay(string dayShortName)
        {
            // 1. Mapowanie skrótu na DayOfWeek oraz na Pełną Nazwę
            DayOfWeek? targetDay = null;
            string fullName = dayShortName;

            switch (dayShortName)
            {
                case "PN": targetDay = DayOfWeek.Monday; fullName = "PONIEDZIAŁEK"; break;
                case "WT": targetDay = DayOfWeek.Tuesday; fullName = "WTOREK"; break;
                case "ŚR": targetDay = DayOfWeek.Wednesday; fullName = "ŚRODA"; break;
                case "CZ": targetDay = DayOfWeek.Thursday; fullName = "CZWARTEK"; break;
                case "PT": targetDay = DayOfWeek.Friday; fullName = "PIĄTEK"; break;
                case "SB": targetDay = DayOfWeek.Saturday; fullName = "SOBOTA"; break;
                case "ND": targetDay = DayOfWeek.Sunday; fullName = "NIEDZIELA"; break;
            }

            if (targetDay == null) return;

            // Ustawiamy ładny tytuł w nagłówku okna
            SelectedDayName = $"SZCZEGÓŁY: {fullName}";

            // 2. Filtrujemy z PAMIĘCI (z listy, którą załadował LoadStats)
            // To gwarantuje, że dane będą identyczne jak na głównym wykresie
            var dayTickets = CachedActiveTickets
                .Where(t => t.Reservation?.Screening?.Start.DayOfWeek == targetDay)
                .ToList();

            // 3. Grupujemy godzinowo
            SelectedDayHourlySales.Clear();

            if (!dayTickets.Any()) return; // Jeśli pusto, kończymy (wyświetli się komunikat w XAML)

            var groups = dayTickets
                .GroupBy(t => t.Reservation!.Screening!.Start.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToList();

            int max = groups.Any() ? groups.Max(x => x.Count) : 1;

            foreach (var g in groups)
            {
                SelectedDayHourlySales.Add(new ChartItem
                {
                    Label = $"{g.Hour}:00",
                    Value = g.Count,
                    NormalizedValue = (double)g.Count / max * 150 // Skalujemy np. do 150px wysokości
                });
            }
        }
        /// <summary>
        /// Eksportuje statystyki raportu do pliku CSV na pulpit.
        /// </summary>
        [RelayCommand]
        private void ExportToCSV()
        {
            try
            {
                // Ścieżka do pliku na pulpicie
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Raport_Kino_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv");

                using (var sw = new StreamWriter(path))
                {
                    // --- NAGŁÓWEK RAPORTU ---
                    sw.WriteLine("RAPORT STATYSTYK KINOWYCH");
                    sw.WriteLine($"Okres raportu;{StartDate:dd.MM.yyyy};do;{EndDate:dd.MM.yyyy}");
                    sw.WriteLine($"Data wygenerowania;{DateTime.Now:g}");
                    sw.WriteLine("");

                    // --- GŁÓWNE WSKAŹNIKI (KPI) ---
                    sw.WriteLine("--- GLOWNE WSKAZNIKI (KPI) ---");
                    sw.WriteLine("Wskaznik;Wartosc;Dodatkowe info");
                    sw.WriteLine($"Calkowity Utarg;{TotalRevenue:F2} PLN;{(IsRevenueUp ? "+" : "")}{RevenueTrend} vs poprz. okres");
                    sw.WriteLine($"Srednia Cena Biletu (ATP);{AvgTicketPrice:F2} PLN;Przychod / Sprzedane bilety");
                    sw.WriteLine($"Sprzedane Bilety;{SoldTickets};sztuk");
                    sw.WriteLine($"Zwroty;{ReturnedTickets};anulowane transakcje");
                    sw.WriteLine($"Srednia Frekwencja;{AvgOccupancy:F2}%;zajetosc sal");
                    sw.WriteLine("");

                    // --- RANKING PRZYCHODOWY (TOP 30) ---
                    sw.WriteLine("--- RANKING FILMOW (WG PRZYCHODU) ---");
                    sw.WriteLine("Miejsce;Tytul;Liczba Biletow;Przychod (PLN)");
                    foreach (var m in TopMovies)
                    {
                        sw.WriteLine($"{m.Rank};{m.Title};{m.Tickets};{m.Revenue:F2}");
                    }
                    sw.WriteLine("");

                    // --- RANKING WOLUMENOWY (TOP 30 BILETÓW) ---
                    sw.WriteLine("--- RANKING FILMOW (WG ILOSCI BILETOW) ---");
                    sw.WriteLine("Miejsce;Tytul;Liczba Biletow");
                    foreach (var m in TopMoviesByTickets)
                    {
                        sw.WriteLine($"{m.Rank};{m.Title};{m.TicketCount}");
                    }
                    sw.WriteLine("");

                    // --- STATYSTYKI GATUNKÓW ---
                    sw.WriteLine("--- POPULARNOSC GATUNKOW ---");
                    sw.WriteLine("Gatunek;Liczba Biletow;Udzial Procentowy");
                    foreach (var g in GenrePopularity)
                    {
                        sw.WriteLine($"{g.Name};{g.Count};{g.Percentage:F2}%");
                    }
                    sw.WriteLine("");

                    // --- STATYSTYKI SAL ---
                    sw.WriteLine("--- OBLOZENIE SAL ---");
                    sw.WriteLine("Sala;Zajetosc (%)");
                    foreach (var h in HallStats)
                    {
                        sw.WriteLine($"{h.Name};{h.Occupancy:F2}%");
                    }
                }

                System.Windows.MessageBox.Show($"Pomyślnie wyeksportowano raport do pliku:\n{path}", "Eksport zakończony", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Wystąpił błąd podczas eksportu CSV:\n{ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Automatycznie odświeża dane gdy zmieni się data początkowa.
        /// </summary>
        partial void OnStartDateChanged(DateTime value) => RefreshData();

        /// <summary>
        /// Automatycznie odświeża dane gdy zmieni się data końcowa.
        /// </summary>
        partial void OnEndDateChanged(DateTime value) => RefreshData();
    }
}