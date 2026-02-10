using CommunityToolkit.Mvvm.ComponentModel;

namespace CinemaSystem.Desktop.Core
{
    /// <summary>
    /// Klasa bazowa dla wszystkich ViewModeli w aplikacji.
    /// </summary>
    /// <remarks>
    /// Dziedziczy po <see cref="ObservableObject"/> z CommunityToolkit.Mvvm, co oznacza, że
    /// implementuje mechanizm powiadamiania o zmianie właściwości (INotifyPropertyChanged).
    /// Dzięki temu dowolny ViewModel dziedziczący po tej klasie może używać metody
    /// <c>SetProperty(ref field, value)</c> w setterach właściwości lub generatora atrybutów
    /// (np. [ObservableProperty]) aby automatycznie wysyłać powiadomienia do widoku.
    ///
    /// Przykład użycia:
    /// <code>
    /// private string _name;
    /// public string Name
    /// {
    ///     get => _name;
    ///     set => SetProperty(ref _name, value); // notifikuje UI po zmianie
    /// }
    /// </code>
    ///
    /// Powiązania:
    /// - ViewModels dziedziczące po tej klasie będą współpracować z widokami (XAML)
    ///   dzięki mechanizmowi powiadomień o zmianie właściwości.
    /// - CommunityToolkit dostarcza dodatkowe narzędzia jak RelayCommand i atrybuty,
    ///   które ułatwiają implementację poleceń i właściwości obserwowalnych.
    /// - Klasa służy jako centralne miejsce do ewentualnego dodania wspólnych funkcji
    ///   dla wszystkich ViewModeli (np. logowanie, wspólne serwisy).
    /// </remarks>
    public class ViewModelBase : ObservableObject
    {
    }
}
