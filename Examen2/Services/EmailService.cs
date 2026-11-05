using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Examen2.Services;

public class EmailService
{
    public async Task<(bool Success, string ErrorDetail)> Send2FACodeAsync(string toEmail, string code)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return (false, "El correo del usuario destino está vacío.");

        try
        {
            var mailTask = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(15000); // 15 segundos para latencia móvil
                using var client = new SmtpClient();
                client.Timeout = 15000;
                client.ServerCertificateValidationCallback = (s, c, ch, e) => true;

                await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect, cts.Token);
                await client.AuthenticateAsync("soporte.workflowapp@gmail.com", "vjzemngobuvcysrp", cts.Token);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("WorkFlow", "soporte.workflowapp@gmail.com"));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = "Código de Verificación - WorkFlow";
                message.Body = new TextPart("plain")
                {
                    Text = $"Hola,\n\nTu código de verificación para ingresar a WorkFlow es: {code}"
                };

                await client.SendAsync(message, cts.Token);
                await client.DisconnectAsync(true, cts.Token);
                return (true, string.Empty);
            });

            var completedTask = await Task.WhenAny(mailTask, Task.Delay(16000));
            if (completedTask == mailTask)
            {
                return await mailTask;
            }

            return (false, "Tiempo de espera agotado tras 15 segundos.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EmailService Exception]: {ex.Message}");
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }
}