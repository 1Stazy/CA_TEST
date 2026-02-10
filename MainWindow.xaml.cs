using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CinemaSystem.Desktop;

/// <summary>
/// Główne okno aplikacji dla kasjera - wyświetla nawigację i widoki (logowanie, kina, rezerwacje, raporty).
/// </summary>
/// <remarks>
/// Okno maksymalizowane bez ramki okna (WindowStyle="None").
/// Zawiera ContentControl powiązany z MainViewModel.CurrentView do dynamicznej zmiany widoków.
/// Datatemplate mapują ViewModele na Views.
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Inicjalizuje główne okno aplikacji.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}