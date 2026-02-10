using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Klasa reprezentująca reklamę w systemie kinematograficznym.
    /// </summary>
    /// <remarks>
    /// Model danych dla modułu reklam w aplikacji. Przechowuje informacje o pojedynczej reklamie,
    /// takie jak tytuł, zawartość, ścieżkę do obrazka oraz ustawienia wyświetlania (sidebar, popup).
    /// Każda reklama może być aktywna lub nieaktywna, co pozwala na łatwe włączanie/wyłączanie
    /// bez usuwania z bazy danych.
    ///
    /// Relacje i powiązania:
    /// - Nie ma bezpośrednich relacji do innych encji Entity Framework.
    /// - Reklamy są pobierane i wyświetlane przez widoki (Views) na stronach aplikacji.
    /// - Moduł reklam działa niezależnie, ale może współpracować z innymi modułami
    ///   (np. wyświetlanie reklam na stronie listy filmów).
    ///
    /// Przeznaczenie:
    /// - Przechowywanie danych o reklamach w bazie danych.
    /// - Filtrowanie aktywnych reklam do wyświetlenia.
    /// - Zarządzanie wizualnym umiejscowieniem reklam (sidebar vs. popup).
    /// </remarks>
    public class Advertisement
    {
        /// <summary>
        /// Unikalny identyfikator reklamy.
        /// </summary>
        /// <remarks>
        /// Klucz główny w bazie danych. Generowany automatycznie.
        /// </remarks>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tytuł lub nazwa reklamy.
        /// </summary>
        /// <remarks>
        /// Wyświetlany użytkownikowi w interfejsie. Zazwyczaj krótki, łatwy do zapamiętania tekst.
        /// </remarks>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Ścieżka do pliku obrazka reklamy.
        /// </summary>
        /// <remarks>
        /// Przechowuje względną lub bezwzględną ścieżkę do obrazka (np. "Assets/Images/ad_banner.png").
        /// Może być używana do załadowania obrazka w widoku (binding do Image kontrolki w XAML).
        /// </remarks>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Zawartość tekstowa lub opis reklamy.
        /// </summary>
        /// <remarks>
        /// Główny tekst reklamy wyświetlany użytkownikowi. Może zawierać opis produktu, ofertę,
        /// lub dodatkowe informacje na temat promowanego elementu.
        /// </remarks>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Flaga określająca, czy reklama powinna być wyświetlana w bocznym panelu.
        /// </summary>
        /// <remarks>
        /// Jeśli true, reklama pojawia się w bocznym panelu (sidebar) na stronach aplikacji.
        /// Może być używane do kontroli rozmieszczenia reklam na interfejsie.
        /// </remarks>
        public bool ShowOnSidebar { get; set; }

        /// <summary>
        /// Flaga określająca, czy reklama powinna wyskakiwać jako popup.
        /// </summary>
        /// <remarks>
        /// Jeśli true, reklama jest wyświetlana jako popup w trakcie korzystania z aplikacji.
        /// Może być używane dla wyróżnionych, ważnych reklam wymagających uwagi użytkownika.
        /// </remarks>
        public bool ShowAsPopup { get; set; }

        /// <summary>
        /// Flaga określająca, czy reklama jest aktywna.
        /// </summary>
        /// <remarks>
        /// Domyślnie true. Jeśli false, reklama jest nieaktywna i nie będzie wyświetlana użytkownikowi.
        /// Pozwala na "miękie" usuwanie reklam bez konieczności usuwania ich z bazy danych.
        /// </remarks>
        public bool IsActive { get; set; } = true;
    }
}