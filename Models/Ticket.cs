using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca bilet do seansu filmowego.
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o pojedynczym bilecie na konkretne miejsce w sali na konkretny seans.
    /// 
    /// Powiązania:
    /// - Bilet należy do jednej rezerwacji (Reservation).
    /// - Każdy bilet wskazuje na konkretne miejsce (Row, SeatNumber).
    /// - Bilet ma przypisany typ (np. Normalny, Ulgowy, Student).
    /// </remarks>
    [Table("Tickets")]
    public class Ticket
    {
        /// <summary>
        /// Unikalny identyfikator biletu.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Wersja wiersza (row versioning dla optimistic concurrency control).
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Identyfikator rezerwacji, do której należy ten bilet.
        /// </summary>
        public int ReservationId { get; set; }

        /// <summary>
        /// Referencja do obiektu Reservation.
        /// </summary>
        [ForeignKey("ReservationId")]
        public Reservation? Reservation { get; set; }

        /// <summary>
        /// Numer rzędu, na który jest bilet.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Numer miejsca w rzędzie, na które jest bilet.
        /// </summary>
        public int SeatNumber { get; set; }

        /// <summary>
        /// Unikalny numer transakcji/biletu do identyfikacji i weryfikacji.
        /// </summary>
        [Required]
        public string TransactionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Data i czas wystawienia biletu w formacie tekstowym.
        /// </summary>
        [Required]
        public string IssuedAt { get; set; } = string.Empty;

        /// <summary>
        /// Status biletu (Active, Used, Cancelled itp.).
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Cena zapłacona za bilet (w walucie lokalnej).
        /// </summary>
        public decimal PricePaid { get; set; }

        /// <summary>
        /// Typ biletu (np. Normalny, Ulgowy, Student). Determinuje cenę i rabaty.
        /// </summary>
        public string TicketType { get; set; } = "Normalny";
    }
}