using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using System;
using System.Linq;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis generujący historyczne dane do testowania raportów i statystyk.
    /// </summary>
    /// <remarks>
    /// Tworzy seanse, rezerwacje i bilety z dat z przeszłości
    /// aby zapełnić wykresy i tabele statystyk w raporcie.
    /// Dane są zupełnie losowe i mają charakter czystoTestowy oraz nie zawieraja się w aktywnych biletaach.
    /// </remarks>
    public static class DataSeeder
    {
        /// <summary>
        /// Generuje dane historyczne z ostatniego miesiaca do celów testowania raportów.
        /// </summary>
        public static void GeneratePastMonthData()
        {
            using (var context = new CinemaDbContext())
            {
                // 1. Sprawdzamy, czy w bazie są jakieś filmy i sale
                var movie = context.Films.FirstOrDefault();
                var hall = context.Halls.FirstOrDefault();

                if (movie == null || hall == null) 
                {
                    System.Windows.MessageBox.Show("Błąd: Dodaj najpierw przynajmniej jeden film i jedną salę w bazie!");
                    return;
                }

                var random = new Random();
                var startDate = DateTime.Today.AddDays(-40); // Generujemy dane od 40 dni wstecz

                // 2. Pętla generująca dane dzień po dniu
                for (int i = 0; i < 40; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    
                    // Czasami pomijamy dzień, żeby wykres był ciekawszy (symulacja braku seansów)
                    if (random.Next(0, 10) > 8) continue; 

                    // --- KROK A: Tworzymy SEANS ---
                    var screening = new Screening
                    {
                        MovieId = movie.Id,
                        HallId = hall.Id,
                        Start = currentDate.AddHours(18).AddMinutes(30), // Np. godzina 18:30
                        Price = 28.00m // Cena biletu
                    };
                    context.Screenings.Add(screening);

                    // --- KROK B: Tworzymy REZERWACJĘ (Transakcję) ---
                    var reservation = new Reservation
                    {
                        Screening = screening,
                        CustomerName = "Klient Generator",
                        CustomerEmail = "dane@historyczne.pl",
                        // Ważne: data utworzenia rezerwacji zgodna z datą seansu
                        CreatedAt = currentDate.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    context.Reservations.Add(reservation);

                    // --- KROK C: Generujemy losową liczbę BILETÓW (od 5 do 20) ---
                    int ticketsCount = random.Next(5, 20);

                    for (int j = 1; j <= ticketsCount; j++)
                    {
                        var ticket = new Ticket
                        {
                            Reservation = reservation,
                            Row = 5,
                            SeatNumber = j,
                            Status = "Active", // Żeby statystyki je zliczyły
                            

                            TransactionNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(), // Unikalny numer
                            IssuedAt = currentDate.ToString("yyyy-MM-dd HH:mm:ss") // Data sprzedaży
                        };
                        context.Tickets.Add(ticket);
                    }
                }

                context.SaveChanges();
                System.Windows.MessageBox.Show("Pomyślnie wygenerowano dane historyczne (Seanse, Rezerwacje, Bilety)!");
            }
        }
    }
}