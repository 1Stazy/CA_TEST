using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca seans filmowy (wyświetlanie konkretnego filmu w konkretnej sali i czasie).
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o seansie - powiązanie filmu, sali i czasu wyświetlania.
    /// 
    /// Powiązania:
    /// - Jeden seans dotyczy jednego filmu (Film).
    /// - Jeden seans odbywa się w jednej sali (Hall).
    /// - Seans może mieć wiele rezerwacji od różnych klientów.
    /// - Każda rezerwacja dla seansu zawiera bilety na konkretne miejsca.
    /// </remarks>
    public partial class Screening : ObservableObject
    {
        /// <summary>
        /// Unikalny identyfikator seansu.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identyfikator filmu wyświetlanego w tym seansie.
        /// </summary>
        public int? MovieId { get; set; }

        /// <summary>
        /// Identyfikator sali, w której odbywać się będzie seans.
        /// </summary>
        public int? HallId { get; set; }

        /// <summary>
        /// Cena biletu dla tego seansu. Domyślnie 25.00.
        /// </summary>
        public decimal Price { get; set; } = 25.00m;

        /// <summary>
        /// Referencja do obiektu Film.
        /// </summary>
        [ForeignKey("MovieId")]
        public Film? Movie { get; set; }

        /// <summary>
        /// Referencja do obiektu Hall (sala).
        /// </summary>
        [ForeignKey("HallId")]
        public Hall? Hall { get; set; }

        /// <summary>
        /// Data i czas rozpoczęcia seansu.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Obliczana właściwość - godzina i minuta seansu w formacie "HH:mm".
        /// </summary>
        public string TimeString => Start.ToString("HH:mm");

        /// <summary>
        /// Obliczana właściwość - data seansu w formacie "dd.MM.yyyy".
        /// </summary>
        public string DateString => Start.ToString("dd.MM.yyyy");
    }
}