using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// Reprezentuje pojedynczy element gatunku filmu w liście filtrów.
    /// </summary>
    /// <remarks>
    /// Klasa pomocnicza do obsługi checkboxów gatunków w widoku MoviesViewModel.
    /// Gdy użytkownik zmieni stan zaznaczenia gatunku, wywoływany jest callback _onSelectionChanged.
    /// </remarks>
    public partial class GenreItem : ObservableObject
    {
        /// <summary>
        /// Nazwa gatunku (np. "Drama", "Komedia", "Akacja").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Flaga czy gatunek jest wybrany/zaznaczony.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        private readonly Action _onSelectionChanged;

        /// <summary>
        /// Inicjalizuje element gatunku z callback'iem na zmianę.
        /// </summary>
        /// <param name="name">Nazwa gatunku.</param>
        /// <param name="onSelectionChanged">Callback wywoływany po zmianie stanu zaznaczenia.</param>
        public GenreItem(string name, Action onSelectionChanged)
        {
            Name = name;
            _onSelectionChanged = onSelectionChanged;
        }

        /// <summary>
        /// Wywoływane automatycznie przez CommunityToolkit gdy zmieni się IsSelected.
        /// Odświeża listę filmów poprzez callback.
        /// </summary>
        partial void OnIsSelectedChanged(bool value)
        {
            _onSelectionChanged?.Invoke();
        }
    }
}