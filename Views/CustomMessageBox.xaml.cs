using System.Windows;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Niestandardowe okno dialogowe z obsługą komunikatów potwierdzenia i informacji.
    /// </summary>
    /// <remarks>
    /// Tryb potwierdzenia: pokazuje przyciski TAK/NIE.
    /// Tryb informacji: pokazuje przycisk OK.
    /// Zwraca true/false w zależności od klikniętego przycisku.
    /// </remarks>
    public partial class CustomMessageBox : Window
    {
        /// <summary>
        /// Wynik kliknięcia: True = Tak/OK, False = Nie
        /// </summary>
        public bool Result { get; private set; } = false;

        /// <summary>
        /// Konstruktor prywatny - okno twozy się poprzez metodę statyczną Show().
        /// </summary>
        private CustomMessageBox(string title, string message, bool isConfirmation)
        {
            InitializeComponent();
            TitleText.Text = title.ToUpper();
            MessageText.Text = message;

            if (isConfirmation)
            {
                // Tryb Pytania (Tak/Nie)
                BtnYes.Content = "TAK";
                BtnNo.Visibility = Visibility.Visible;
            }
            else
            {
                // Tryb Informacji (Tylko OK)
                BtnYes.Content = "OK";
                BtnNo.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Tak" lub "OK".
        /// </summary>
        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Nie".
        /// </summary>
        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        /// <summary>
        /// Statyczna metoda pomocnicza - łatwo wywoływalna w kodzie.
        /// </summary>
        /// <param name="title">Tytuł okna dialogowego.</param>
        /// <param name="message">Treść wiadomości.</param>
        /// <param name="isConfirmation">True = tryb potwierdzenia (TAK/NIE), False = tryb informacji (OK).</param>
        /// <returns>True jeśli użytkownik kliknął "Tak"/"OK", False dla "Nie".</returns>
        public static bool Show(string title, string message, bool isConfirmation = false)
        {
            var msgBox = new CustomMessageBox(title, message, isConfirmation);
            
            // Ustawienie właściciela, żeby okno było na środku głównej aplikacji
            if (System.Windows.Application.Current.MainWindow.IsVisible)
            {
                msgBox.Owner = System.Windows.Application.Current.MainWindow;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}