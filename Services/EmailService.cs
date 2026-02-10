using System;
using System.Net;
using System.Net.Mail;
using System.IO;

namespace CinemaSystem.Desktop.Services
{
    /// <summary>
    /// Serwis wysyłania biletów i powiadomień emailem.
    /// </summary>
    /// <remarks>
    /// Korzysta z serwera SMTP Gmail do wysyłania maili.
    /// Wysyła bilet PDF jako załącznik na email klienta.
    /// 
    /// UWAGA: Hasło aplikacji jest widoczne w kodzie - w produkcji należy
    /// przechowywać je w zmiennych środowiskowych lub konfiguracji.
    /// </remarks>
    public class EmailService
    {
        /// <summary>Serwer SMTP Google Gmail.</summary>
        private readonly string _smtpServer = "smtp.gmail.com";
        /// <summary>Port TLS dla Gmail SMTP.</summary>
        private readonly int _port = 587;
        /// <summary>Adres email konta nadawcy.</summary>
        private readonly string _senderEmail = "szymon.tkaczyk2001@gmail.com";
        /// <summary>Hasło aplikacji Gmail (16-znakowe, wygenerowane w Google Account).</summary>
        private readonly string _appPassword = "rqzbtrdiarnbcznt";

        /// <summary>
        /// Wysyła bilet w postaci załącznika PDF na email klienta.
        /// </summary>
        /// <param name="recipientEmail">Adres email odbiorcy.</param>
        /// <param name="userName">Imię i nazwisko klienta.</param>
        /// <param name="pdfPath">Ścieżka do wygenerowanego pliku PDF biletu.</param>
        public void SendTicketEmail(string recipientEmail, string userName, string pdfPath)
        {
            try
            {
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(_senderEmail, "Kino System");
                    mail.To.Add(recipientEmail);
                    mail.Subject = "Twój bilet do Kina System";
                    
                    mail.Body = $"Witaj {userName}!\n\n" +
                                "Dziękujemy za zakup biletów w naszym kinie. " +
                                "W załączniku znajdziesz swój bilet oraz voucher barowy.\n\n" +
                                "Zapraszamy na seans!";

                    Attachment? attachment = null;
                    if (File.Exists(pdfPath))
                    {
                        attachment = new Attachment(pdfPath);
                        mail.Attachments.Add(attachment);
                    }

                    using (var smtp = new SmtpClient(_smtpServer, _port))
                    {
                        smtp.Credentials = new NetworkCredential(_senderEmail, _appPassword);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }

                    // Zwolnienie pliku, aby nie był zablokowany
                    attachment?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd SMTP: {ex.Message}");
                throw;
            }
        }
    }
}