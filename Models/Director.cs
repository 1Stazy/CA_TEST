using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca reżysera filmów w systemie.
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o reżyserze, który może być powiązany z jednym lub wieloma filmami.
    /// 
    /// Powiązania:
    /// - Jeden reżyser może być przypisany do wielu filmów (relacja jeden-do-wielu).
    /// </remarks>
    [Table("Directors")]
    public class Director
    {
        /// <summary>
        /// Unikalny identyfikator reżysera.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Imię i nazwisko reżysera.
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
