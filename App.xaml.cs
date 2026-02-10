using System.Windows;
using System.Globalization; 
using System.Threading;     
using System.Windows.Markup; 
using CinemaSystem.Desktop.Services;
using CinemaSystem.Desktop.ViewModels;
using CinemaSystem.Desktop.Views;

namespace CinemaSystem.Desktop
{
    /// <summary>
    /// Aplikacja główna - punkt startowy systemu zarządzania kinem.
    /// </summary>
    /// <remarks>
    /// Inicjalizuje kulturę (pl-PL), bazę danych, okno kasjera i klienta.
    /// Oba okna korzystają z tego samego MainViewModel do synchronizacji stanu.
    /// </remarks>
    public partial class App : System.Windows.Application
    {
        private MainWindow _mainWindow = null!;
        private CustomerWindow _customerWindow = null!;
        private MainViewModel _mainViewModel = null!;

        /// <summary>
        /// Metoda wywoływana podczas uruchomienia aplikacji.
        /// </summary>
        /// <remarks>
        /// 1. Ustawia kulturę na polski (pl-PL) dla formatowania liczb, dat itp.
        /// 2. Inicjalizuje bazę danych (tworzy jeśli nie istnieje)
        /// 3. Generuje dane historyczne do raportów
        /// 4. Tworzy okno kasjera (MainWindow) i okno klienta
        /// 5. Przenosi okno klienta na drugi monitor (jeśli dostępny)
        /// </remarks>
        protected override void OnStartup(StartupEventArgs e)
        {
            // --- 1. KONFIGURACJA JĘZYKA/KULTURY (PL) ---
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = 
                System.Diagnostics.SourceLevels.Critical | System.Diagnostics.SourceLevels.Error;
            
            var cultureInfo = new CultureInfo("pl-PL");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag)));

            base.OnStartup(e);

            // --- 2. INICJALIZACJA DANYCH ---
            // Generowanie bazy (jeśli nie istnieje)
            DatabaseGenerator.GenerateFinalDatabase(); 
            
            // --- 3. TWORZENIE GŁÓWNEGO VIEWMODELU ---
            // W nowej wersji MainViewModel sam tworzy wewnątrz MoviesViewModel.
            // Dlatego używamy konstruktora BEZPARAMETROWEGO.
            _mainViewModel = new MainViewModel();

            // --- 4. TWORZENIE OKNA KASJERA ---
            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _mainViewModel;
            _mainWindow.Show();

            // --- 5. TWORZENIE OKNA KLIENTA ---
            _customerWindow = new CustomerWindow();
            
            // Oba okna korzystają z TEGO SAMEGO MainViewModel, dzięki czemu są zsynchronizowane
            _customerWindow.DataContext = _mainViewModel; 

            // Przeniesienie okna na drugi monitor (Twoja metoda)
            ScreenManager.OpenCustomerWindow(_customerWindow);
        }
    }
}