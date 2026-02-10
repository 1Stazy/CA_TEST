using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca salę kinową w systemie.
    /// </summary>
    /// <remarks>
    /// Przechowuje informacje o sali, w tym jej layout (liczba rzędów i miejsc), nazwę i pozycję wejścia.
    /// 
    /// Powiązania:
    /// - Sala jest używana w halach (screeningach) - każdy seans to konkretny film w konkretnej sali.
    /// - Wszystkie miejsca w sali mogą być rezerwowane przez klientów.
    /// </remarks>
    public partial class Hall : ObservableObject
    {
        /// <summary>
        /// Unikalny identyfikator sali.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nazwa sali (np. "Sala 1", "Sala 2").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Liczba rzędów w sali.
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// Liczba miejsc w każdym rzędzie sali.
        /// </summary>
        public int SeatsPerRow { get; set; }

        /// <summary>
        /// Pozycja wejścia do sali (Right, Left lub Back).
        /// </summary>
        public string EntrancePosition { get; set; } = "Right";
    }
}