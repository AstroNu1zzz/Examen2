using System.Net;
using System.Net.Mail;

namespace Examen2.Services
{
    public class EmailService
    {
        // Configuración SMTP (Ejemplo con Gmail)
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;

        // Coloca aquí un correo de salida real y su contraseña de aplicación
        private const string SenderEmail = "nicolassepulveda.lopez2009@gmail.com";
        private const string SenderPassword = "ujqwvdcrjrlrjpmv";

        public async Task<bool> Send2FACodeAsync(string recipientEmail, string code)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(SenderEmail, "Google Classroom - Verificación 2FA");
                message.To.Add(recipientEmail);
                message.Subject = $"{code} es tu código de verificación de Classroom";
                message.Body = $@"
                    <h2>Autenticación de dos factores</h2>
                    <p>Has solicitado iniciar sesión en Google Classroom Clone.</p>
                    <p>Tu código de verificación es: <strong style='font-size:24px; color:#1A73E8;'>{code}</strong></p>
                    <p>Si no solicitaste este código, ignora este mensaje.</p>";
                message.IsBodyHtml = true;

                using var smtp = new SmtpClient(SmtpServer, SmtpPort)
                {
                    Credentials = new NetworkCredential(SenderEmail, SenderPassword),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al enviar correo: {ex.Message}");
                return false;
            }
        }
    }
}