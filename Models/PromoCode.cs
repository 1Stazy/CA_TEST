using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca kod rabatowy w systemie.
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o kodach promocyjnych/rabatowych, które mogą być stosowane do rezerwacji.
    /// Rabat może być stały (kwota) lub procentowy (procent od ceny).
    /// 
    /// Powiązania:
    /// - Kod promocyjny jest opcjonalnie stosowany do rezerwacji klienta.
    /// </remarks>
    public class PromoCode
    {
        /// <summary>
        /// Unikalny identyfikator kodu promocyjnego.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Ciąg tekstowy reprezentujący kod (np. "SUMMER20", "FIRSTBUY10").
        /// </summary>
        [Required]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Wartość rabatu - może być kwotą (np. 5.0) lub procentem (np. 20.0).
        /// </summary>
        public double DiscountValue { get; set; }

        /// <summary>
        /// Flaga określająca, czy DiscountValue jest wartością procentową (true) czy stałą kwotą (false).
        /// </summary>
        public bool IsPercentage { get; set; }

        /// <summary>
        /// Flaga określająca, czy kod promocyjny jest aktywny i można go używać.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}