using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa definiująca typy biletów i ich ceny w systemie.
    /// </summary>
    /// <remarks>
    /// Przechowuje definicje typów biletów (np. Normalny, Ulgowy, Student) wraz z ich mnożnikami
    /// ceny lub stałymi cenami. Pozwala na łatwe zarządzanie taryfami biletów.
    /// 
    /// Każdy typ może mieć:
    /// - Mnożnik ceny (np. 1.0 dla normalnego, 0.5 dla ulgowego)
    /// - Stałą cenę (jeśli FixedPrice jest ustawione, jest używana zamiast mnożnika)
    /// </remarks>
    public class TicketTypeDefinition
    {
        /// <summary>
        /// Unikalny identyfikator typu biletu.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nazwa typu biletu (np. "Normalny", "Ulgowy", "Student").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mnożnik ceny w stosunku do ceny bazowej seansu.
        /// Np. 1.0 = pełna cena, 0.5 = połowa ceny, 1.5 = 150% ceny.
        /// </summary>
        public double PriceMultiplier { get; set; }

        /// <summary>
        /// Opcjonalna stała cena dla tego typu biletu.
        /// Jeśli ustawiona, zastępuje PriceMultiplier.
        /// </summary>
        public double? FixedPrice { get; set; }

        /// <summary>
        /// Flaga określająca, czy typ biletu jest aktywny i dostępny do użytku.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}