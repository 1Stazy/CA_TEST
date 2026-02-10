using System;
using System.IO;
using System.Text.Json;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Model przechowujący ustawienia widoczności kafelków raportu.
    /// </summary>
    /// <remarks>
    /// Zawiera flagi boolean dla każdego wykresu i wskaźnika,
    /// umożliwiając użytkownikowi dostosowanie widoku raportu.
    /// </remarks>
    public class ReportSettingsData
    {
        /// <summary>Czy wyświetlać wskaźnik całkowitego utargu.</summary>
        public bool ShowRevenue { get; set; } = true;
        /// <summary>Czy wyświetlać średnią cenę biletu.</summary>
        public bool ShowATP { get; set; } = true;
        /// <summary>Czy wyświetlać liczbę sprzedanych biletów.</summary>
        public bool ShowSoldTickets { get; set; } = true;
        /// <summary>Czy wyświetlać statystyki zwrotów.</summary>
        public bool ShowReturns { get; set; } = true;
        /// <summary>Czy wyświetlać średnie obłożenie sal.</summary>
        public bool ShowOccupancy { get; set; } = true;
        /// <summary>Czy wyświetlać godzinowy wykres sprzedaży.</summary>
        public bool ShowHourlyChart { get; set; } = true;
        /// <summary>Czy wyświetlać tygodniowy wykres sprzedaży.</summary>
        public bool ShowWeeklyChart { get; set; } = true;
        /// <summary>Czy wyświetlać popularność gatuneków filmowych.</summary>
        public bool ShowGenreChart { get; set; } = true;
        /// <summary>Czy wyświetlać obłożenie sal kinowych.</summary>
        public bool ShowHallStats { get; set; } = true;
        /// <summary>Czy wyświetlać ranking najlepszych filmów (wg przychodu).</summary>
        public bool ShowTopMovies { get; set; } = true;
        /// <summary>Czy wyświetlać ranking filmów wg liczby sprzedanych biletów.</summary>
        public bool ShowMoviesTicketCount { get; set; } = true;
        /// <summary>Czy wyświetlać rozkład typów biletów.</summary>
        public bool ShowTicketTypeChart { get; set; } = true;
    }

    /// <summary>
    /// Serwis do zapamiętywania i wczytywania preferencji widoczności raportu.
    /// </summary>
    /// <remarks>
    /// Przechowuje ustawienia użytkownika w pliku JSON w folderze Dokumenty,
    /// aby zachować wybór widoczności kafelków między sesjami aplikacji.
    /// </remarks>
    public static class ReportSettingsService
    {
        /// <summary>
        /// Ścieżka do pliku konfiguracyjnego w folderze użytkownika.
        /// </summary>
        private static string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "cinema_report_layout.json");

        /// <summary>
        /// Zapisuje ustawienia widoczności kafelków raportu do pliku JSON.
        /// </summary>
        public static void Save(ReportSettingsData settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(_filePath, json);
            }
            catch { /* Ignorujemy błędy zapisu */ }
        }

        /// <summary>
        /// Wczytuje ustawienia widoczności raportu z pliku JSON.
        /// Jeśli plik nie istnieje, zwraca domyślne ustawienia (wszystko widoczne).
        /// </summary>
        public static ReportSettingsData Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<ReportSettingsData>(json) ?? new ReportSettingsData();
                }
            }
            catch { /* Ignorujemy błędy odczytu */ }

            return new ReportSettingsData(); // Zwracamy domyślne (wszystko włączone)
        }
    }
}