using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca rezerwację seansu filmowego przez klienta.
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o rezerwacji - powiązanie klienta ze seansu filmowym i lista biletów.
    /// 
    /// Powiązania:
    /// - Rezerwacja dotyczy jednego konkretnego seansu (Screening).
    /// - Rezerwacja zawiera listę biletów (jeden bilet = jedno miejsce w sali).
    /// - Każda rezerwacja ma status (Active, Cancelled itp.).
    /// </remarks>
    [Table("Reservations")]
    public class Reservation
    {
        /// <summary>
        /// Unikalny identyfikator rezerwacji.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Wersja wiersza (row versioning dla optimistic concurrency control).
        /// Automatycznie aktualizuje się przy każdej modyfikacji.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Identyfikator seansu, dla którego jest złożona rezerwacja.
        /// </summary>
        public int ScreeningId { get; set; }

        /// <summary>
        /// Adres e-mail klienta dokonującego rezerwacji.
        /// </summary>
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Referencja do obiektu Screening (seans).
        /// </summary>
        [ForeignKey("ScreeningId")]
        public Screening? Screening { get; set; }

        /// <summary>
        /// Data i czas utworzenia rezerwacji w formacie tekstowym.
        /// </summary>
        [Required]
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Pełne imię i nazwisko klienta.
        /// </summary>
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Lista biletów należących do tej rezerwacji.
        /// </summary>
        public List<Ticket> Tickets { get; set; } = new();

        /// <summary>
        /// Status rezerwacji (Active, Cancelled itp.).
        /// </summary>
        public string Status { get; set; } = "Active";
    }
}