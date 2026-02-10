using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca film w systemie kinematograficznym.
    /// </summary>
    /// <remarks>
    /// Przechowuje pełne informacje o filmie, w tym metadane, parametry wyświetlania i powiązania.
    /// 
    /// Powiązania:
    /// - Film jest przypisany do jednego reżysera (relacja jeden-do-jeden lub wiele-do-jeden).
    /// - Film może mieć wiele pokazów (screeningów) w różnych salach i czasach.
    /// - Każdy seans filmu może mieć rezerwacje od klientów.
    /// </remarks>
    [Table("Films")]
    public class Film
    {
        /// <summary>
        /// Unikalny identyfikator filmu.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tytuł filmu.
        /// </summary>
        [Required]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Opis lub streszczenie fabuły filmu.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Czas trwania filmu w minutach.
        /// </summary>
        public int Duration { get; set; } = 120;

        /// <summary>
        /// Gatunek filmu (np. drama, komedia, thriller).
        /// </summary>
        public string Genre { get; set; } = "Inny";

        /// <summary>
        /// Data premiery lub premiery światowej w formacie tekstowym.
        /// </summary>
        public string ReleaseDate { get; set; } = string.Empty;

        /// <summary>
        /// Ścieżka do pliku lub URL plakatu/okładki filmu.
        /// </summary>
        public string PosterPath { get; set; } = string.Empty;

        /// <summary>
        /// Ocena IMDB lub pochodzi z innego źródła ocen (na skali 0.0-10.0).
        /// </summary>
        public double ImdbRating { get; set; }

        /// <summary>
        /// Liczba dni, przez które film będzie dostępny do wynajęcia lub wyświetlania.
        /// </summary>
        public int AvailabilityDays { get; set; }

        /// <summary>
        /// Identyfikator reżysera filmu (klucz obcy).
        /// </summary>
        public int? DirectorId { get; set; }

        /// <summary>
        /// Referencja do obiektu reżysera.
        /// </summary>
        [ForeignKey("DirectorId")]
        public Director? Director { get; set; }

        /// <summary>
        /// Flaga określająca, czy film jest aktywny i dostępny do wyświetlania.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Obliczana właściwość zwracająca pełną ścieżkę URI do plakatu w oparciu o PosterPath.
        /// Jeśli PosterPath jest pusty, zwraca placeholder.png.
        /// </summary>
        [NotMapped]
        public string PosterUri
        {
            get
            {
                string fileName = "placeholder.png";
                if (!string.IsNullOrEmpty(PosterPath))
                {
                    fileName = System.IO.Path.GetFileName(PosterPath);
                }
                return System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", fileName);
            }
        }
    }
}