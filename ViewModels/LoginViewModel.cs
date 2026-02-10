using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Services;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel odpowiedzialny za obsługę ekranu logowania.
    /// </summary>
    /// <remarks>
    /// Autoryzuje użytkownika na podstawie loginu i hasła.
    /// Emituje zdarzenie LoginSuccess po udanym uwierzytelnieniu.
    /// 
    /// Powiązania:
    /// - Pobiera dane użytkownika z bazy danych CinemaDbContext.
    /// - Emituje zdarzenie LoginSuccess wraz z obiektem zalogowanego użytkownika.
    /// </remarks>
    public partial class LoginViewModel : ViewModelBase
    {
        /// <summary>
        /// Wprowadzony login użytkownika.
        /// </summary>
        [ObservableProperty]
        private string _username = "";

        /// <summary>
        /// Komunikat błędu wyświetlany użytkownikowi.
        /// </summary>
        [ObservableProperty]
        private string _errorMessage = "";

        /// <summary>
        /// Zdarzenie wywoływane po pomyślnym zalogowaniu.
        /// </summary>
        public event Action<User>? LoginSuccess;

        /// <summary>
        /// Wykonuję logowanie użytkownika - weryfikuję dane i emituję zdarzenie.
        /// </summary>
        /// <param name="parameter">PasswordBox z interfejsu (do pobrania hasła).</param>
        [RelayCommand]
        private async Task Login(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Podaj login i hasło.";
                return;
            }

            try
            {
                var user = await Task.Run(() =>
                {
                    using var context = new CinemaDbContext();
                    return context.Users.FirstOrDefault(u => u.Login == Username && u.PasswordHash == password);
                });

                if (user != null)
                {
                    LoginSuccess?.Invoke(user);
                }
                else
                {
                    ErrorMessage = "Błędne dane logowania.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Błąd: " + ex.Message;
            }
        }
    }
}