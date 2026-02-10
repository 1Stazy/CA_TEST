using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca użytkownika/pracownika systemu kinematograficznego.
    /// </summary>
    /// <remarks>
    /// Przechowuje dane logowania i informacje profilu dla pracowników kina.
    /// Każdy użytkownik ma przypisaną rolę, która określa jego uprawnienia w systemie.
    /// 
    /// Role:
    /// - "Admin" - pełen dostęp do wszystkich funkcji
    /// - "Cashier" - dostęp do zarządzania rezerwacjami i sprzedażą biletów
    /// </remarks>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Unikalny identyfikator użytkownika.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Login/nazwa użytkownika do logowania się do systemu.
        /// </summary>
        [Required]
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Haszowana wersja hasła użytkownika (nigdy nie przechowuj hasła w czystej postaci).
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Rola użytkownika określająca jego uprawnienia (np. "Admin", "Cashier").
        /// </summary>
        [Required]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Pełne imię i nazwisko użytkownika/pracownika.
        /// </summary>
        public string FullName { get; set; } = string.Empty;
    }
}
