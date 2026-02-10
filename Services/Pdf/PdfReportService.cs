using CinemaSystem.Desktop.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis generowania raportów PDF w stylu ciemnym (dark theme).
    /// </summary>
    /// <remarks>
    /// Wykorzystuje bibliotekę QuestPDF (licencja Community) do generowania dokumentów PDF.
    /// Raporty zawierają: KPI (KKE), wykresy sprzedaży, statystyki sal, gatunkowaniem filmów.
    /// Format: A4 landscape, kolory dostune do darkowego themat interfejsu UI.
    /// Po generacji PDF automatycznie się otwiera w systemowej aplikacji.
    /// </remarks>
    public static class PdfReportService
    {
        // --- KOLORY (Identyczne z UI dark theme) ---
        private static readonly string ColorBg = "#121212";
        private static readonly string ColorCard = "#1E1E1E";
        private static readonly string ColorTextMain = "#FFFFFF";
        private static readonly string ColorTextSec = "#AAAAAA";
        private static readonly string ColorAccent = "#FFD700";
        private static readonly string ColorGreen = "#66BB6A";
        private static readonly string ColorRed = "#EF5350";
        private static readonly string ColorBarBlue = "#00ACC1";
        private static readonly string ColorBarGold = "#FFD700";
        private static readonly string ColorProgressBg = "#2A2A2A";
        private static readonly string ColorPurple = "#AB47BC";
        private static readonly string ColorOrange = "#FFA726";

        /// <summary>
        /// Generuje plik PDF z raportem statystyk sprzedazy za wybrany okres.
        /// </summary>
        /// <param name="stats">ViewModel zawierający wymaganego obliczone dane (utarg, frekwencję, wykresy).</param>
        /// <param name="filePath">Ŝieżka do pliku wyjściowego PDF (np. raport.pdf).</param>
        /// <remarks>
        /// Strona 1: KPI cards + tabela z wykresami (godziny, sale, gatunki, top filmy).
        /// Strona 2: Szczegółowy rozkład sprzedazy wg dnó tygodnia i godzin.
        /// Po generacji plik ułatwi się domyslnym programem (PDF readerem).
        /// </remarks>
        public static void GeneratePdf(ReportsViewModel stats, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(12); // Zoptymalizowany margines
                    page.PageColor(ColorBg);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.SegoeUI).FontColor(ColorTextMain).FontSize(8));

                    page.Header().Element(header => ComposeHeader(header, stats));
                    page.Content().Element(content => ComposeContent(content, stats));

                    page.Footer().PaddingTop(5).AlignCenter().Text(x =>
                    {
                        x.Span("Wygenerowano dla Kino System: ").FontColor(ColorTextSec).FontSize(7);
                        x.Span($"{DateTime.Now:g}").FontColor(ColorAccent).FontSize(7);
                    });
                });
            });

            try
            {
                document.GeneratePdf(filePath);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // To tu trafiał błąd, który widziałeś w debugerze
                throw new Exception("Błąd generowania PDF. Sprawdź czy plik nie jest otwarty i czy dane są poprawne.", ex);
            }
        }

        /// <summary>
        /// Komponuje nagłówek raportu z tytułem i zakresem dat.
        /// </summary>
        private static void ComposeHeader(IContainer container, ReportsViewModel stats)
        {
            container.PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("STATYSTYKI KINOWE").FontSize(20).Bold().FontColor(ColorAccent);
                    col.Item().Text($"Okres raportu: {stats.StartDate:dd.MM.yyyy} - {stats.EndDate:dd.MM.yyyy}").FontSize(10).FontColor(ColorTextSec);
                });
            });
        }

        /// <summary>
        /// Komponuje główną treść raportu z KPI, wykresami i statystykami.
        /// </summary>
        private static void ComposeContent(IContainer container, ReportsViewModel stats)
        {
            container.Column(col =>
            {
                col.Spacing(10);

                // --- 1. KPI (GÓRNY PASEK) ---
                col.Item().Row(row =>
                {
                    row.Spacing(8);
                    DrawKpi(row.RelativeItem(), "CAŁKOWITY UTARG", $"{stats.TotalRevenue:N2} zł", stats.RevenueTrend, true);
                    DrawKpi(row.RelativeItem(), "ŚR. CENA", $"{stats.AvgTicketPrice:N2} zł", "PLN/Bilet", null);
                    DrawKpi(row.RelativeItem(), "SPRZEDANE", stats.SoldTickets.ToString(), "Sztuk", null);
                    DrawKpi(row.RelativeItem(), "ZWROTY", stats.ReturnedTickets.ToString(), "Anulowane", false);
                    DrawKpi(row.RelativeItem(), "FREKWENCJA", $"{stats.AvgOccupancy:F1}%", "Zajętość", null, (float)stats.AvgOccupancy);
                });

                // --- 2. DASHBOARD (STABILNA TABELA) ---
                // Usunąłem sztywne wysokości Height(85) z komórek - to one wywoływały DocumentLayoutException
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f); 
                        columns.RelativeColumn(1f);   
                        columns.RelativeColumn(1f);   
                    });

                    // RZĄD 1
                    table.Cell().PaddingRight(8).PaddingBottom(8).Element(c => DrawCard(c, "ROZKŁAD GODZINOWY", content => DrawBarChart(content, stats.HourlySales, ColorBarBlue, 55)));
                    table.Cell().PaddingRight(8).PaddingBottom(8).Element(c => DrawCard(c, "OBŁOŻENIE SAL", content => {
                        content.Column(cc => { foreach (var h in stats.HallStats.Take(3)) DrawProgressRow(cc.Item(), h.Name, h.Occupancy, h.OccupancyText, ColorAccent); });
                    }));
                    table.Cell().PaddingBottom(8).Element(c => DrawCard(c, "TOP BILETY", content => DrawSimpleTable(content, stats.TopMoviesByTickets.Take(5), ColorBarBlue, true)));

                    // RZĄD 2
                    table.Cell().PaddingRight(8).PaddingBottom(8).Element(c => DrawCard(c, "DNI TYGODNIA", content => DrawWeeklyChart(content, stats.WeeklySales, 55)));
                    table.Cell().PaddingRight(8).PaddingBottom(8).Element(c => DrawCard(c, "GATUNKI", content => {
                        content.Column(cc => { foreach (var g in stats.GenrePopularity.Take(3)) DrawProgressRow(cc.Item(), g.Name, g.Percentage, $"{g.Percentage:F0}%", ColorPurple); });
                    }));
                    table.Cell().PaddingBottom(8).Element(c => DrawCard(c, "TOP PRZYCHODY", content => DrawSimpleTable(content, stats.TopMovies.Take(5), ColorGreen, false)));

                    // RZĄD 3
                    table.Cell().PaddingRight(8).Element(c => DrawCard(c, "STRUKTURA KLIENTÓW", content => {
                        content.Column(cc => { foreach (var stat in stats.TicketTypeStats.Take(3)) DrawProgressRow(cc.Item(), stat.Name, stat.Percentage, $"{stat.Count} szt.", ColorOrange); });
                    }));
                });

                // --- 3. SZCZEGÓŁY DNI (NA KOLEJNEJ STRONIE) ---
                col.Item().PageBreak();
                col.Item().PaddingVertical(5).Text("SZCZEGÓŁOWY ROZKŁAD GODZINOWY WG DNI").FontSize(12).Bold().FontColor(ColorAccent);
                col.Item().Element(c => DrawDailyHourlyCharts(c, stats));
            });
        }

        /// <summary>
        /// Rysuje szczegółowy rozkład sprzedaży godzinowej dla każdego dnia tygodnia.
        /// </summary>
        private static void DrawDailyHourlyCharts(IContainer container, ReportsViewModel stats)
        {
            var plNames = new[] { "PN", "WT", "ŚR", "CZ", "PT", "SB", "ND" };
            var days = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            container.Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });

                for (int i = 0; i < days.Length; i++)
                {
                    var day = days[i];
                    var cached = stats.CachedActiveTickets ?? new List<CinemaSystem.Desktop.Models.Ticket>();
                    var dayTickets = cached.Where(t => t.Reservation?.Screening?.Start.DayOfWeek == day).ToList();

                    var groups = dayTickets.GroupBy(t => t.Reservation!.Screening!.Start.Hour)
                        .Select(g => new ChartItem { Label = $"{g.Key}", Value = g.Count() }).OrderBy(x => int.Parse(x.Label)).ToList();

                    table.Cell().Padding(4).Element(cell => DrawCard(cell, plNames[i], content => {
                        if (groups.Any()) DrawBarChart(content, new ObservableCollection<ChartItem>(groups), (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday) ? ColorAccent : ColorBarBlue, 40);
                        else content.AlignCenter().AlignMiddle().Text("Brak seansów").FontColor(ColorTextSec).FontSize(7);
                    }));
                }
            });
        }

        /// <summary>
        /// Rysuje kafelek z zaokrąglonymi rogami, pasek tytułowy i zawartość.
        /// </summary>
        private static void DrawCard(IContainer container, string title, Action<IContainer> content)
        {
            container.Background(ColorCard).CornerRadius(4).Padding(6).Column(col => {
                col.Item().Text(title).FontSize(7.5f).Bold().FontColor(ColorTextSec);
                col.Item().PaddingTop(4).Element(content);
            });
        }

        /// <summary>
        /// Rysuje kafelek KPI (Key Performance Indicator) z wartością, paskiem postępu i trendem.
        /// </summary>
        private static void DrawKpi(IContainer container, string title, string value, string subtext, bool? isPositive, float? progress = null)
        {
            container.Background(ColorCard).CornerRadius(4).Padding(6).Column(col => {
                col.Item().Text(title).FontSize(6.5f).FontColor(ColorTextSec).Bold();
                col.Item().Text(value).FontSize(14).Bold().FontColor(title.Contains("UTARG") ? ColorAccent : title.Contains("ZWROTY") ? ColorRed : ColorTextMain);
                if (progress.HasValue) {
                    col.Item().PaddingTop(2).Height(3).Row(r => {
                        r.RelativeItem(Math.Max(0.01f, progress.Value)).Background(ColorGreen).CornerRadius(1);
                        r.RelativeItem(Math.Max(0.01f, 100 - progress.Value)).Background(ColorProgressBg).CornerRadius(1);
                    });
                } else {
                    col.Item().Text(subtext).FontSize(6).FontColor(isPositive == false ? ColorRed : ColorTextSec);
                }
            });
        }

        /// <summary>
        /// Rysuje wiersz z etykietą i paskiem postępu (np. frekwencja sali, popularność gatunku).
        /// </summary>
        private static void DrawProgressRow(IContainer container, string label, double percent, string rightText, string barColor)
        {
            container.Column(c => {
                c.Item().Row(r => {
                    r.RelativeItem().Text(label).FontSize(7).FontColor(ColorTextMain);
                    r.AutoItem().Text(rightText).FontSize(6).FontColor(ColorTextSec);
                });
                c.Item().PaddingTop(1).Height(3).Row(row => {
                    row.RelativeItem(Math.Max(0.01f, (float)percent)).Background(barColor).CornerRadius(1);
                    row.RelativeItem(Math.Max(0.01f, (float)(100 - percent))).Background(ColorProgressBg).CornerRadius(1);
                });
            });
        }

        /// <summary>
        /// Rysuje wykres słupkowy (histogram) dla danych zbiorowych (np. sprzedaz wg godzin).
        /// </summary>
        private static void DrawBarChart(IContainer container, ObservableCollection<ChartItem> data, string color, float chartHeight)
        {
            if (!data.Any()) return;
            double max = data.Max(x => x.Value); if (max == 0) max = 1;
            container.Height(chartHeight).Row(row => {
                row.Spacing(3);
                foreach (var item in data) {
                    float hPct = (float)(item.Value / max);
                    row.RelativeItem().Column(col => {
                        // Używamy Math.Max(0, ...), aby uniknąć ujemnych wysokości przy hPct = 1
                        col.Item().Height(Math.Max(0, (chartHeight - 12) * (1f - hPct)));
                        col.Item().Height(Math.Max(0, (chartHeight - 12) * hPct)).Background(color).CornerRadius(1);
                        col.Item().AlignCenter().Text(item.Label.Split(':')[0]).FontSize(5.5f).FontColor(ColorTextSec);
                    });
                }
            });
        }

        /// <summary>
        /// Rysuje wykres słupkowy dla dnów tygodnia (weekday/weekend z różnymi kolorami).
        /// </summary>
        private static void DrawWeeklyChart(IContainer container, ObservableCollection<DayOfWeekStat> data, float chartHeight)
        {
            if (!data.Any()) return;
            double max = data.Max(x => x.Value); if (max == 0) max = 1;
            container.Height(chartHeight).Row(row => {
                row.Spacing(4);
                foreach (var day in data) {
                    float hPct = (float)(day.Value / max);
                    string barColor = day.IsWeekend ? ColorBarGold : ColorBarBlue;
                    row.RelativeItem().Column(col => {
                        col.Item().AlignCenter().Text(day.Value.ToString()).FontSize(5.5f).FontColor(ColorTextSec);
                        col.Item().Height(Math.Max(0, (chartHeight - 22) * (1f - hPct)));
                        col.Item().Height(Math.Max(0, (chartHeight - 22) * hPct)).Background(barColor).CornerRadius(1);
                        col.Item().AlignCenter().Text(day.DayName).FontSize(6).Bold();
                    });
                }
            });
        }

        /// <summary>
        /// Rysuje prostą tabelę top filmów (ranking, tytuł, liczba biletów/przychód).
        /// </summary>
        private static void DrawSimpleTable(IContainer container, IEnumerable<object> items, string valColor, bool isTicket)
        {
            container.Table(t => {
                t.ColumnsDefinition(cd => { cd.ConstantColumn(12); cd.RelativeColumn(); cd.ConstantColumn(40); });
                foreach (var item in items) {
                    var rank = item.GetType().GetProperty("Rank")?.GetValue(item, null)?.ToString() ?? "0";
                    var title = item.GetType().GetProperty("Title")?.GetValue(item, null)?.ToString() ?? "-";
                    string val = isTicket ? (item.GetType().GetProperty("TicketCount")?.GetValue(item, null)?.ToString() ?? "0") : 
                        $"{((decimal)(item.GetType().GetProperty("Revenue")?.GetValue(item, null) ?? 0m)):N0} zł";

                    t.Cell().Text(rank).FontSize(7).FontColor(ColorAccent).Bold();
                    t.Cell().Text(title).FontSize(7).ClampLines(1);
                    t.Cell().AlignRight().Text(val).FontSize(7).FontColor(valColor).Bold();
                }
            });
        }
    }
}