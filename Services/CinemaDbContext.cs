using Microsoft.EntityFrameworkCore;
using CinemaSystem.Desktop.Models;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Główny kontekst bazy danych Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// Zarządza połączeniem z bazą SQLite (plik kino.db) i zawiera definicje wszystkich
    /// tabel (DbSet) reprezentujących modele domeny aplikacji.
    /// 
    /// Tabele:
    /// - Films: katalog filmów
    /// - Users: użytkownicy aplikacji (administratorzy, kasjerzy)
    /// - Directors: reżyserowie
    /// - Halls: sale kinowe
    /// - Screenings: seanse filmowe
    /// - Reservations: rezerwacje biletów
    /// - Tickets: poszczególne bilety
    /// - PromoCodes: kody rabatowe
    /// - TicketTypes: typy biletów (Normalny, Ułgowy, Student)
    /// - Advertisements: reklamy wyświetlane w systemie
    /// </remarks>
    public class CinemaDbContext : DbContext
    {
        public DbSet<Film> Films { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Director> Directors { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Screening> Screenings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<TicketTypeDefinition> TicketTypes { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }

        /// <summary>
        /// Konfiguruje opcje bazy danych - typ (PostgreSQL) i parametry conexji.
        /// </summary>
        /// <remarks>
        /// Obsługuje dwie konfiguracje:
        /// 1. Zmienna środowiskowa: DATABASE_URL (cloud, np. Heroku, Railway, Render)
        /// 2. Connection string w kodzie (local development)
        /// 
        /// Parametry PostgreSQL:
        /// - Connection Pooling: włączone automatycznie przez Npgsql
        /// - Min Pool Size: 5
        /// - Max Pool Size: 100
        /// - Retry on Failure: 3 próby z opóźnieniem 10 sekund
        /// </remarks>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Odbierz connection string ze zmiennej środowiskowej (production)
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // Development: użyj lokalne domyślne
                connectionString = "Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432";
            }
            
            // PostgreSQL z automatycznym connection pooling'iem
            optionsBuilder.UseNpgsql(
                connectionString,
                options => options
                    .EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), new[] { "42P01" }) // 3 retries, 10 sec delay, retry on specific errors
                    .CommandTimeout(30)
            );
            
            // Debug: Enable query logging (tylko w Development)
            #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging(true);
            #endif
        }
    }
}
