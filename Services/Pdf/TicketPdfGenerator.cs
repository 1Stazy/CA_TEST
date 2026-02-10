using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CinemaSystem.Desktop.Models;
using QRCoder; 

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis generowania potwierdzenia rezerwacji jako PDF z kodem QR.
    /// </summary>
    /// <remarks>
    /// Generuje indywidualny bilet PDF dla każdej rezerwacji z:
    /// - Kodami QR dla numeru transakcji (weryfikacja wejścia)
    /// - Szczegółami seansu (film, data, sala, miejsca)
    /// - Automatycznym voucherem na darmowy zestaw zarobowy (2 bilety = 1 voucher).
    /// Format: A4 portrait biał, wykorzystuje bibliotekę QuestPDF i QRCoder.
    /// Patrz: GenerateTicketPdf(reservation, tickets, filePath).
    /// </remarks>
    public class TicketPdfGenerator
    {
        private static readonly string ColorAccent = "#D32F2F"; 
        private static readonly string ColorGold = "#B8860B";   
        private static readonly string ColorTextMuted = Colors.Grey.Darken2;

        public TicketPdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            // Usunięto problematyczną linię CheckIfAllTextIsVisible
        }

        /// <summary>
        /// Generuje PDF potwierdzenia rezerwacji biletów z weryfikacją QR.
        /// </summary>
        /// <param name="reservation">Rezerwacja zawierająca dane seansu, hali, aktorstwa.</param>
        /// <param name="tickets">Lista biletów do drukowania (miejsca, rzedy).</param>
        /// <param name="filePath">Miejsce zapisu pliku PDF (np. "bilet_123.pdf").</param>
        /// <remarks>
        /// Nagłówek: Logo "KINO SYSTEM" + kod QR transakcji.
        /// Zawartość: Szczegóły seansu (film, godzina, sala), tabela miejsc, ceny biletów.
        /// Dolne sekcja: Automatycznie generowany voucher na darmowy zestaw (1 voucher na 2 bilety).
        /// Rodząj: A4 portrait, białe tło (w kontraście do raportów z dark theme).
        /// </remarks>
        public void GenerateTicketPdf(Reservation reservation, List<Ticket> tickets, string filePath)
        {
            var screening = reservation.Screening;
            var movie = screening?.Movie;
            var hall = screening?.Hall;
            decimal totalPrice = tickets.Count * 25.00m; 

            // Zabezpieczenie danych wejściowych
            string transactionNum = tickets.FirstOrDefault()?.TransactionNumber 
                                    ?? reservation?.Id.ToString() 
                                    ?? "000000";
            
            byte[] qrBytes = GenerateQrCode(transactionNum);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial").FontColor(Colors.Black));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("KINO SYSTEM").FontSize(28).Bold().FontColor(ColorAccent);
                            col.Item().Text("POTWIERDZENIE REZERWACJI").FontSize(14).FontColor(ColorGold);
                            col.Item().PaddingTop(5).Text($"Data: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(ColorTextMuted);
                            col.Item().Text($"Nr transakcji: {transactionNum}").FontSize(9).Italic();
                        });
                        
                        // Zastosowanie stałej szerokości zamiast wysokości zapobiega błędom overflow
                        row.ConstantItem(85).Column(c => 
                        {
                            c.Item().Width(85).Image(qrBytes);
                            c.Item().AlignCenter().Text(transactionNum).FontSize(6);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Background(Colors.Grey.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(c => {
                            c.Item().Text("SEANS").FontSize(8).FontColor(ColorTextMuted).SemiBold();
                            c.Item().Text(movie?.Title.ToUpper() ?? "FILM").FontSize(20).Bold();
                            c.Item().PaddingTop(5).Text($"{screening?.DateString} | {screening?.TimeString} | Sala: {hall?.Name}").FontSize(12);
                        });

                        col.Item().PaddingTop(15).Text("MIEJSCA").FontSize(12).Bold().FontColor(ColorGold);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                            });

                            table.Header(header => {
                                header.Cell().BorderBottom(1).PaddingVertical(5).Text("LP");
                                header.Cell().BorderBottom(1).PaddingVertical(5).Text("RZĄD / MIEJSCE");
                                header.Cell().BorderBottom(1).PaddingVertical(5).AlignRight().Text("CENA");
                            });

                            foreach (var t in tickets)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).Text($"{tickets.IndexOf(t) + 1}.");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).Text($"Rząd: {t.Row} | Miejsce: {t.SeatNumber}").Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5).AlignRight().Text("25,00 zł");
                            }
                        });

                        col.Item().PaddingTop(10).PaddingBottom(20).AlignRight().Text($"SUMA: {totalPrice:F2} PLN").FontSize(18).Bold().FontColor(ColorAccent);

                        int voucherAmount = tickets.Count / 2;
                        if (voucherAmount >= 1)
                        {
                            // Użycie .ShowEntire() gwarantuje, że voucher nie zostanie "rozcięty" między strony
                            col.Item().PaddingTop(10).ShowEntire().Element(c => ComposeVoucherPage(c, transactionNum, voucherAmount));
                        }
                    });

                    page.Footer().AlignCenter().Column(column =>
                    {
                        column.Item().AlignCenter().PaddingBottom(5).Text("Bilet wygenerowany systemowo. Prosimy o okazanie kodu QR przy wejściu.")
                            .FontSize(8).FontColor(Colors.Grey.Medium);

                        column.Item().AlignCenter().Text("Kino System")
                            .FontSize(8).FontColor(Colors.Black);

                        column.Item().AlignCenter().Text("Filmowo, 12-345")
                            .FontSize(8).FontColor(Colors.Black);

                        column.Item().AlignCenter().Text("ul. Seansów 123/45")
                            .FontSize(8).FontColor(Colors.Black);
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        /// <summary>
        /// Komponuje sekcję voucheru na darmowy zestaw barowy (popcorn + napój).
        /// </summary>
        private void ComposeVoucherPage(IContainer container, string transactionNum, int voucherCount)
        {
            string imagePath = @"C:\Users\Szymon Tkaczyk\Documents\CAv2\CA\Resources\Assets\popcorn.png";

            container
                .PaddingTop(10)
                .Border(1)
                .BorderColor(ColorGold)
                .Layers(layers =>
                {
                    // Warstwa tła z popcornem
                    if (File.Exists(imagePath))
                    {
                        layers.Layer().AlignRight().AlignBottom().PaddingRight(75).PaddingBottom(1).Width(100).Image(imagePath).FitArea();
                    }

                    // Główna warstwa treści
                    layers.PrimaryLayer().Column(col =>
                    {
                        col.Item().Background(Colors.Amber.Lighten5).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c => {
                                c.Item().Text("GOLDEN TICKET").FontSize(24).Bold().FontColor(ColorGold);
                                c.Item().Text("VOUCHER NA ZESTAW BAROWY").FontSize(11).SemiBold().FontColor(Colors.Amber.Darken3);
                            });
                            
                            row.ConstantItem(55).Column(qrCol => {
                                qrCol.Item().Width(55).Image(GenerateQrCode($"VOUCHER-{transactionNum}"));
                                qrCol.Item().AlignCenter().Text(transactionNum).FontSize(5);
                            });
                        });

                        col.Item().Padding(15).Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("W NAGRODĘ ZA ZAKUP BILETÓW:").FontSize(10);
                                inner.Item().Text("DARMOWY ZESTAW").FontSize(20).Bold();
                                inner.Item().Text("(MAŁY POPCORN + NAPÓJ)").FontSize(14).FontColor(ColorTextMuted);
                            });

                            row.ConstantItem(80).AlignCenter().Row(r => {
                                r.AutoItem().Text("x").FontSize(28).Bold().FontColor(ColorGold);
                                r.AutoItem().PaddingLeft(5).Text(voucherCount.ToString()).FontSize(45).Bold().FontColor(ColorGold);
                            });
                        });
                    });
                });
        }

        /// <summary>
        /// Generuje kod QR z pomocą biblioteki QRCoder (format PNG).
        /// </summary>
        /// <param name="text">Treść do zakodowania w QR (zwykle numer transakcji).</param>
        /// <returns>Tablica bajtów zawierająca obraz PNG kodu QR.</returns>
        private byte[] GenerateQrCode(string text)
        {
            if (string.IsNullOrEmpty(text)) text = "000000";

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                return qrCode.GetGraphic(20);
            }
        }
        /// <summary>
        /// Generuje raport sprzedaży dziennej z podsumowaniem utargu i estatystyk filmów.
        /// </summary>
        /// <param name="reportDate">Data raportu (dzień do podsumowania).</param>
        /// <param name="tickets">Lista wszystkich sprzedanych biletów w tym dniu.</param>
        /// <param name="filePath">Scieżka zapisu pliku PDF raportu.</param>
        /// <remarks>
        /// Format: A4 portrait.
        /// Zawiera: Łączny przychód, liczbę sprzedanych biletów, analizę sprzedaży wg filmów.
        /// Użęcie: Dzienने podsumowania dla kierownika/rachunkowego.
        /// </remarks>
        public void GenerateDailyReportPdf(DateTime reportDate, List<Ticket> tickets, string filePath)
{
    decimal totalRevenue = tickets.Count * 25.00m;
    var movieStats = tickets
        .Where(t => t?.Reservation?.Screening?.Movie?.Title != null)
        .GroupBy(t => t.Reservation?.Screening?.Movie?.Title ?? "Unknown")
        .Select(g => new { Title = g.Key, Count = g.Count(), Total = g.Count() * 25.00m })
        .OrderByDescending(x => x.Total);

    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            // NAGŁÓWEK RAPORTU
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RAPORT SPRZEDAŻY DZIENNEJ").FontSize(22).Bold().FontColor(ColorAccent);
                    col.Item().Text($"KINO SYSTEM - {reportDate:dd.MM.yyyy}").FontSize(12).SemiBold();
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Wygenerowano: {DateTime.Now:HH:mm:ss}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            page.Content().PaddingVertical(20).Column(col =>
            {
                // SEKCHJA 1: PODSUMOWANIE OGÓLNE
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(c => {
                        c.Item().Text("ŁĄCZNY PRZYCHÓD").FontSize(10).FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{totalRevenue:C}").FontSize(24).Bold().FontColor(Colors.Green.Medium);
                    });
                    row.RelativeItem().Column(c => {
                        c.Item().Text("SPRZEDANE BILETY").FontSize(10).FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{tickets.Count} szt.").FontSize(24).Bold();
                    });
                });

                // SEKCJA 2: ANALIZA WEDŁUG FILMÓW
                col.Item().PaddingTop(30).Text("SPRZEDAŻ WEDŁUG TYTUŁÓW").FontSize(14).Bold();
                
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).Padding(5).Text("TYTUŁ FILMU").Bold();
                        header.Cell().BorderBottom(1).Padding(5).AlignCenter().Text("ILOŚĆ").Bold();
                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("WARTOŚĆ").Bold();
                    });

                    foreach (var stat in movieStats)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(stat.Title);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).AlignCenter().Text(stat.Count.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{stat.Total:C}");
                    }
                });

                col.Item().PaddingTop(40).AlignCenter().Text("--- Koniec Raportu ---").Italic().FontColor(Colors.Grey.Medium);
            });

            page.Footer().AlignCenter().Text(x => {
                x.Span("Strona ").FontSize(9);
                x.CurrentPageNumber().FontSize(9);
            });
        });
    }).GeneratePdf(filePath);
}
    }
}