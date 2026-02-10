using System.Linq;
using CinemaSystem.Desktop.Models;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis do weryfikacji danych logowania użytkownika.
    /// </summary>
    /// <remarks>
    /// Sprawdza poprawność nazwy użytkownika i hasła w bazie danych.
    /// W przyszłości można tu dodać haszowanie haseł (bcrypt, PBKDF2 itp.).
    /// </remarks>
    public class AuthenticationService
    {
        /// <summary>
        /// Sprawdza dane logowania i zwraca użytkownika jeśli są poprawne.
        /// </summary>
        /// <param name="username">Nazwa użytkownika (login).</param>
        /// <param name="password">Hasło w postaci jawnej (TODO: haszowanie).</param>
        /// <returns>Obiekt użytkownika jeśli autentykacja powiedzie się, null w przeciwnym razie.</returns>
        public User? ValidateUser(string username, string password)
        {
            using (var context = new CinemaDbContext())
            {
                // Szukamy użytkownika o podanym loginie
                var user = context.Users.FirstOrDefault(u => u.Login == username);

                if (user == null) return null;

                // Sprawdzamy hasło (w Twojej bazie kolumna nazywa się PasswordHash)
                // Zakładamy proste porównanie tekstowe
                if (user.PasswordHash == password)
                {
                    return user;
                }

                return null;
            }
        }
    }
}
