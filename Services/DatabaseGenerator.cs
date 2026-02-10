using System;
using System.Collections.Generic;
using System.Linq;
using CinemaSystem.Desktop.Models;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Statyczny generator bazy danych - tworzy nowa baze z przykładowymi danymi testowymi.
    /// </summary>
    /// <remarks>
    /// Metoda GenerateFinalDatabase() czyści starą bazę i tworzy nową zawierającą:
    /// - Użytkowników (admin, kasjer)
    /// - Kody rabatowe i promocyjne
    /// - Sale kinowe z różnymi rozmiarami
    /// - 30+ filmów z reżyseriami
    /// - Seanse dla każdego filmu
    /// 
    /// UWAGA: Ta metoda usuwa całą istniejącą bazę danych!
    /// </remarks>
    public static class DatabaseGenerator
    {
        /// <summary>
        /// Tworzy nową bazę danych od zera ze wszystkimi danymi testowymi.
        /// </summary>
        public static void GenerateFinalDatabase()
        {
            using (var context = new CinemaDbContext())
            {
                // 1. CZYSTY START
                // Usuwamy starą bazę i tworzymy nową, idealnie pasującą do kodu C#
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                Console.WriteLine("Generowanie bazy danych...");

                // 2. UŻYTKOWNICY
                var users = new List<User>
                {
                    new User { Login = "admin", PasswordHash = "admin", Role = "Administrator", FullName = "System Administrator" },
                    new User { Login = "kasjer", PasswordHash = "kasjer", Role = "Kasjer", FullName = "Jan Sprzedawca" }
                };
                context.Users.AddRange(users);

                // 3. KODY RABATOWE I KARNETY
                var promoCodes = new List<PromoCode>
                {
                    new PromoCode { Code = "START2026", DiscountValue = 0.20, IsPercentage = true, IsActive = true }, // -20%
                    new PromoCode { Code = "STUDENT", DiscountValue = 0.50, IsPercentage = true, IsActive = true },   // -50%
                    new PromoCode { Code = "VIP_KARNET", DiscountValue = 1.00, IsPercentage = true, IsActive = true }, // 100% zniżki (Darmowe)
                    new PromoCode { Code = "KWOTA10", DiscountValue = 10.00, IsPercentage = false, IsActive = true }   // -10 PLN
                };
                context.PromoCodes.AddRange(promoCodes);

                // 4. SALE KINOWE
                var halls = new List<Hall>
                {
                    new Hall { Name = "Sala 1 (IMAX)", Rows = 10, SeatsPerRow = 14, EntrancePosition = "Right" },
                    new Hall { Name = "Sala 2 (Standard)", Rows = 8, SeatsPerRow = 12, EntrancePosition = "Left" },
                    new Hall { Name = "Sala 3 (VIP)", Rows = 5, SeatsPerRow = 8, EntrancePosition = "Center" }
                };
                context.Halls.AddRange(halls);

                // 5. REŻYSERZY I FILMY (Baza 30 tytułów)
                // Najpierw tworzymy listę reżyserów, żeby mieć ich ID
                var directorsList = new List<Director>
                {
                    new Director { Name = "James Cameron" },          // 0
                    new Director { Name = "Anthony & Joe Russo" },    // 1
                    new Director { Name = "Robert Zemeckis" },        // 2
                    new Director { Name = "Nieznany / Biblijny" },    // 3 (David)
                    new Director { Name = "Aditya Dhar" },            // 4
                    new Director { Name = "James L. Brooks" },        // 5
                    new Director { Name = "David Freyne" },           // 6 (Eternity - POPRAWIONE)
                    new Director { Name = "Emma Tammi" },             // 7
                    new Director { Name = "Chloé Zhao" },             // 8
                    new Director { Name = "Quentin Tarantino" },      // 9
                    new Director { Name = "Paolo Sorrentino" },       // 10 (La Grazia - POPRAWIONE)
                    new Director { Name = "Josh Safdie" },            // 11
                    new Director { Name = "Richard Linklater" },      // 12
                    new Director { Name = "Ruben Fleischer" },        // 13
                    new Director { Name = "James Vanderbilt" },       // 14
                    new Director { Name = "Guillermo del Toro" },     // 15
                    new Director { Name = "Dan Trachtenberg" },       // 16
                    new Director { Name = "Joaquim Dos Santos" },     // 17
                    new Director { Name = "George Lucas" },           // 18
                    new Director { Name = "Joss Whedon" },            // 19
                    new Director { Name = "Christopher Nolan" },      // 20
                    new Director { Name = "Paul Feig" },              // 21
                    new Director { Name = "Edgar Wright" },           // 22
                    new Director { Name = "Derek Drymon" },           // 23
                    new Director { Name = "Jon M. Chu" },             // 24
                    new Director { Name = "Jared Bush" },             // 25
                    new Director { Name = "Phil Cunningham" }         // 26 (David - POPRAWIONE)
                };
                context.Directors.AddRange(directorsList);
                context.SaveChanges(); // Zapisujemy, żeby dostać ID
                var dirs = context.Directors.ToList(); // Pobieramy z ID

                    var films = new List<Film>
                {
                    // --- AVATAR UNIVERSE ---
                    new Film { Title = "Avatar", ReleaseDate = "2009", Duration = 162, Genre = "Sci-Fi", ImdbRating = 7.9, PosterPath = "/Resources/Images/avatar-2009.jpg", DirectorId = dirs[0].Id, Description = "Były marine Jake Sully wyrusza na misję na Pandorę." },
                    new Film { Title = "Avatar: Istota Wody", ReleaseDate = "2022", Duration = 192, Genre = "Sci-Fi", ImdbRating = 7.6, PosterPath = "/Resources/Images/avatar-the-way-of-water-2022.jpg", DirectorId = dirs[0].Id, Description = "Jake i Neytiri muszą opuścić dom i eksplorować oceany." },
                    new Film { Title = "Avatar: Fire and Ash", ReleaseDate = "2025", Duration = 180, Genre = "Sci-Fi", ImdbRating = 7.8, PosterPath = "/Resources/Images/avatar-fire-and-ash.jpg", DirectorId = dirs[0].Id, Description = "Kontynuacja sagi rodu Sully i spotkanie z Ludem Popiołu." },

                    // --- MARVEL & HEROES ---
                    new Film { Title = "Avengers: Infinity War", ReleaseDate = "2018", Duration = 149, Genre = "Akcja", ImdbRating = 8.4, PosterPath = "/Resources/Images/avengers-infinity-war.jpg", DirectorId = dirs[1].Id, Description = "Avengersi łączą siły, by powstrzymać Thanosa." },
                    new Film { Title = "The Avengers", ReleaseDate = "2012", Duration = 143, Genre = "Akcja", ImdbRating = 8.0, PosterPath = "/Resources/Images/the-avengers.jpg", DirectorId = dirs[19].Id, Description = "Superbohaterowie jednoczą się przeciwko Lokiemu." },
                    new Film { Title = "Captain America: Winter Soldier", ReleaseDate = "2014", Duration = 136, Genre = "Akcja", ImdbRating = 7.8, PosterPath = "/Resources/Images/captain-america-the-winter-soldier.jpg", DirectorId = dirs[1].Id, Description = "Steve Rogers walczy z tajemniczym zimowym żołnierzem." },
                    new Film { Title = "The Dark Knight", ReleaseDate = "2008", Duration = 152, Genre = "Akcja", ImdbRating = 9.0, PosterPath = "/Resources/Images/the-dark-knight-2008.jpg", DirectorId = dirs[20].Id, Description = "Batman stawia czoła chaosowi sianemu przez Jokera." },
                    new Film { Title = "Spider-Man: Across Spider-Verse", ReleaseDate = "2023", Duration = 140, Genre = "Animacja", ImdbRating = 8.6, PosterPath = "/Resources/Images/spider-man-across-the-spider-verse-part-one.jpg", DirectorId = dirs[17].Id, Description = "Miles Morales podróżuje przez multiwersum pająków." },

                    // --- SCI-FI CLASSICS ---
                    new Film { Title = "Back to the Future", ReleaseDate = "1985", Duration = 116, Genre = "Sci-Fi", ImdbRating = 8.5, PosterPath = "/Resources/Images/back-to-the-future.jpg", DirectorId = dirs[2].Id, Description = "Marty McFly przenosi się w czasie do lat 50." },
                    new Film { Title = "Star Wars: A New Hope", ReleaseDate = "1977", Duration = 121, Genre = "Sci-Fi", ImdbRating = 8.6, PosterPath = "/Resources/Images/Star Wars A New Hope-1977.jpg", DirectorId = dirs[18].Id, Description = "Luke Skywalker dołącza do Rebelii przeciwko Imperium." },
                    new Film { Title = "Pacific Rim", ReleaseDate = "2013", Duration = 131, Genre = "Sci-Fi", ImdbRating = 6.9, PosterPath = "/Resources/Images/pacific-rim.jpg", DirectorId = dirs[15].Id, Description = "Wielkie roboty Jaeger walczą z potworami Kaiju." },
                    new Film { Title = "Predator: Badlands", ReleaseDate = "2025", Duration = 110, Genre = "Sci-Fi", ImdbRating = 7.2, PosterPath = "/Resources/Images/predator-badlands.jpg", DirectorId = dirs[16].Id, Description = "Nowa odsłona łowcy w futurystycznym świecie." },
                    new Film { Title = "The Running Man", ReleaseDate = "2025", Duration = 120, Genre = "Sci-Fi", ImdbRating = 7.4, PosterPath = "/Resources/Images/the-running-man.jpeg", DirectorId = dirs[22].Id, Description = "Remake klasyka o dystopijnym turnieju na śmierć i życie." },

                    // --- 2025/2026 NOWOŚCI & POPRAWIONE ---
                    new Film { Title = "Eternity", ReleaseDate = "2025", Duration = 110, Genre = "Komedia Rom.", ImdbRating = 7.1, PosterPath = "/Resources/Images/eternity.jpg", DirectorId = dirs[6].Id, Description = "Komedia romantyczna fantasy z Elizabeth Olsen i Milesem Tellerem." },
                    new Film { Title = "David", ReleaseDate = "2025", Duration = 120, Genre = "Animacja/Musical", ImdbRating = 7.5, PosterPath = "/Resources/Images/david-2025.jpeg", DirectorId = dirs[26].Id, Description = "Animowany musical biblijny o królu Dawidzie." },
                    new Film { Title = "La Grazia", ReleaseDate = "2025", Duration = 100, Genre = "Dramat", ImdbRating = 7.3, PosterPath = "/Resources/Images/la-grazia.jpg", DirectorId = dirs[10].Id, Description = "Włoski dramat Paolo Sorrentino o miłości i poświęceniu." },
                    
                    new Film { Title = "Dhurandhar", ReleaseDate = "2025", Duration = 150, Genre = "Akcja", ImdbRating = 6.8, PosterPath = "/Resources/Images/dhurandhar.jpeg", DirectorId = dirs[4].Id, Description = "Epicka opowieść szpiegowska o indyjskim wywiadzie." },
                    new Film { Title = "Ella McCay", ReleaseDate = "2025", Duration = 115, Genre = "Komedia", ImdbRating = 7.2, PosterPath = "/Resources/Images/ella-mccay-2025.jpg", DirectorId = dirs[5].Id, Description = "Idealistyczna polityk próbuje pogodzić życie z karierą." },
                    new Film { Title = "Five Nights at Freddy's 2", ReleaseDate = "2025", Duration = 110, Genre = "Horror", ImdbRating = 5.8, PosterPath = "/Resources/Images/five-nights-at-freddys-2.jpg", DirectorId = dirs[7].Id, Description = "Kolejna noc w pizzerii pełnej morderczych animatroników." },
                    new Film { Title = "Hamnet", ReleaseDate = "2025", Duration = 120, Genre = "Dramat", ImdbRating = 7.7, PosterPath = "/Resources/Images/hamnet.jpeg", DirectorId = dirs[8].Id, Description = "Historia żony Williama Shakespeare'a i śmierci ich syna." },
                    new Film { Title = "Marty Supreme", ReleaseDate = "2025", Duration = 120, Genre = "Biograficzny", ImdbRating = 7.4, PosterPath = "/Resources/Images/marty-supreme-movie.jpg", DirectorId = dirs[11].Id, Description = "Historia legendy ping-ponga, Marty'ego Reismana." },
                    new Film { Title = "Merrily We Roll Along", ReleaseDate = "2025", Duration = 150, Genre = "Musical", ImdbRating = 8.1, PosterPath = "/Resources/Images/merrily-we-roll-along.jpg", DirectorId = dirs[12].Id, Description = "Historia kompozytora opowiedziana w odwróconej chronologii." },
                    new Film { Title = "Now You See Me 3", ReleaseDate = "2025", Duration = 120, Genre = "Thriller", ImdbRating = 6.5, PosterPath = "/Resources/Images/now-you-see-me-now-you-dont.jpg", DirectorId = dirs[13].Id, Description = "Czterej Jeźdźcy powracają z największą iluzją w karierze." },
                    new Film { Title = "Nuremberg", ReleaseDate = "2025", Duration = 130, Genre = "Dramat", ImdbRating = 7.6, PosterPath = "/Resources/Images/nuremberg.jpg", DirectorId = dirs[14].Id, Description = "Kulisy procesów norymberskich po II wojnie światowej." },
                    new Film { Title = "The Housemaid", ReleaseDate = "2025", Duration = 110, Genre = "Thriller", ImdbRating = 6.9, PosterPath = "/Resources/Images/the-housemaid-2025.jpg", DirectorId = dirs[21].Id, Description = "Mroczne sekrety zamożnej rodziny odkryte przez pokojówkę." },
                    
                    // --- ANIMACJE & INNE ---
                    new Film { Title = "SpongeBob: Search for SquarePants", ReleaseDate = "2025", Duration = 90, Genre = "Animacja", ImdbRating = 6.8, PosterPath = "/Resources/Images/the-spongebob-movie-search-for-squarepants.jpeg", DirectorId = dirs[23].Id, Description = "SpongeBob wyrusza na dno oceanu w nowej przygodzie." },
                    new Film { Title = "Wicked", ReleaseDate = "2024", Duration = 160, Genre = "Musical", ImdbRating = 7.9, PosterPath = "/Resources/Images/wicked.jpg", DirectorId = dirs[24].Id, Description = "Historia Elphaby, przyszłej Złej Czarownicy z Zachodu." },
                    new Film { Title = "Wicked: Part Two", ReleaseDate = "2025", Duration = 160, Genre = "Musical", ImdbRating = 7.9, PosterPath = "/Resources/Images/wicked-for-good.jpg", DirectorId = dirs[24].Id, Description = "Finał historii czarownic z krainy Oz." },
                    new Film { Title = "Zootopia 2", ReleaseDate = "2025", Duration = 100, Genre = "Animacja", ImdbRating = 8.0, PosterPath = "/Resources/Images/zootopia-2.png", DirectorId = dirs[25].Id, Description = "Judy Hopps i Nick Wilde wracają, by rozwiązać nową zagadkę." },
                    new Film { Title = "Kill Bill: Whole Bloody Affair", ReleaseDate = "2011", Duration = 247, Genre = "Akcja", ImdbRating = 8.8, PosterPath = "/Resources/Images/kill-bill-the-whole-bloody-affair.jpg", DirectorId = dirs[9].Id, Description = "Pełna, 4-godzinna wersja krwawej zemsty Panny Młodej." }
                };

                context.Films.AddRange(films);
                context.SaveChanges();

                // 6. GENEROWANIE HARMONOGRAMU (5 miesięcy, rotacja)
                Console.WriteLine("Generowanie harmonogramu na 150 dni...");
                
                var screenings = new List<Screening>();
                var random = new Random();
                var startDate = DateTime.Today;
                var allFilms = context.Films.ToList();

                // Pętla na 22 tygodnie (ok. 5 miesięcy)
                for (int week = 0; week < 22; week++)
                {
                    // Na każdy tydzień wybieramy losowo 8 filmów "w repertuarze"
                    // (Żeby nie grać wszystkiego naraz)
                    var weeklyRepertoire = allFilms.OrderBy(x => random.Next()).Take(8).ToList();

                    // Dla każdego dnia w tygodniu
                    for (int day = 0; day < 7; day++)
                    {
                        DateTime currentDate = startDate.AddDays((week * 7) + day);

                        // W każdej z 3 sal gramy 3-4 seanse dziennie
                        foreach (var hall in halls)
                        {
                            // Startujemy od godziny 10:00 - 12:00
                            int currentHour = random.Next(10, 13); 
                            
                            // Planujemy 3 do 5 seansów w tej sali
                            int seancesCount = random.Next(3, 6);

                            for (int s = 0; s < seancesCount; s++)
                            {
                                // Wybieramy losowy film z repertuaru tygodnia
                                var film = weeklyRepertoire[random.Next(weeklyRepertoire.Count)];
                                
                                DateTime startDateTime = currentDate.AddHours(currentHour).AddMinutes(random.Next(0, 4) * 15); // np. 12:00, 12:15, 12:30...

                                // Ustalanie ceny w zależności od pory
                                decimal price = 25.00m;
                                if (startDateTime.Hour < 14) price = 20.00m; // Tanie poranki
                                else if (startDateTime.Hour >= 18 || startDateTime.DayOfWeek == DayOfWeek.Saturday || startDateTime.DayOfWeek == DayOfWeek.Sunday) price = 32.00m; // Weekendy i wieczory

                                screenings.Add(new Screening 
                                { 
                                    MovieId = film.Id, 
                                    HallId = hall.Id, 
                                    Start = startDateTime, 
                                    Price = price 
                                });

                                // Następny seans: Czas trwania filmu + 30 min sprzątania
                                int totalDurationMinutes = film.Duration + 30;
                                currentHour += totalDurationMinutes / 60;
                                
                                // Jeśli wyszliśmy poza 23:00, kończymy na dziś w tej sali
                                if (currentHour >= 23) break;
                            }
                        }
                    }
                }
                context.Screenings.AddRange(screenings);
                // 7. TYPY BILETÓW (Zamiast hardcodowanych w kodzie)
                var ticketTypes = new List<TicketTypeDefinition>
                {
                    new TicketTypeDefinition { Name = "Normalny", PriceMultiplier = 1.0 },
                    new TicketTypeDefinition { Name = "Ulgowy", PriceMultiplier = 0.70 }, // -30%
                    new TicketTypeDefinition { Name = "Senior", PriceMultiplier = 0.60 }, // -40%
                    new TicketTypeDefinition { Name = "Rodzinny", PriceMultiplier = 0.80 }, // -20%
                    new TicketTypeDefinition { Name = "Voucher", PriceMultiplier = 0.0 }  // 0 PLN
                };
                context.TicketTypes.AddRange(ticketTypes);
                // 8. REKLAMY (Przykładowe)
                var ads = new List<Advertisement>
                {
                    new Advertisement { Title = "Popcorn + Cola", Content = "Zestaw Duży tylko 25 PLN!", ImagePath = "/Resources/Images/popcorn_promo.jpg", ShowOnSidebar = true, IsActive = true },
                    new Advertisement { Title = "Karta Stałego Klienta", Content = "Zbieraj punkty i wymieniaj na bilety.", ShowOnSidebar = true, IsActive = true }
                };
                context.Advertisements.AddRange(ads);

                context.SaveChanges();
            }
        }
    }
}