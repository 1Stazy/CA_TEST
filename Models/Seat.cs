using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Enum reprezentujący możliwe stany miejsca w sali kinowej.
    /// </summary>
    public enum SeatStatus
    {
        /// <summary>
        /// Miejsce jest wolne i dostępne do rezerwacji.
        /// </summary>
        Available,

        /// <summary>
        /// Miejsce jest zajęte (już zarezerwowane lub sprzedane).
        /// </summary>
        Taken,

        /// <summary>
        /// Miejsce jest wybrany/zaznaczone przez użytkownika (w trakcie rezerwacji).
        /// </summary>
        Selected,

        /// <summary>
        /// Miejsce jest w trakcie edycji (np. zmiana typu biletu).
        /// </summary>
        Editing
    }

    /// <summary>
    /// Klasa reprezentująca pojedyncze miejsce (fotel) w sali kinowej.
    /// </summary>
    /// <remarks>
    /// Każde miejsce ma pozycję (rząd i numer) oraz status.
    /// Miejsca są wyświetlane w GUI i mogą być klikane do rezerwacji.
    /// Dziedziczy po ObservableObject z CommunityToolkit.Mvvm.
    /// </remarks>
    public partial class Seat : ObservableObject
    {
        /// <summary>
        /// Status bieżącego miejsca (Wolne, Zajęte, Wybrane, Edytowane).
        /// </summary>
        [ObservableProperty]
        private SeatStatus _status;

        /// <summary>
        /// Numer rzędu, w którym znajduje się to miejsce.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Numer miejsca w rzędzie.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Obliczana właściwość zwracająca sformatowaną etykietę dla wyświetlenia w GUI.
        /// Format: "R: X | M: Y" gdzie X to rząd, Y to numer miejsca.
        /// </summary>
        public string DisplayName => $"R: {Row} | M: {Number}";
    }
}