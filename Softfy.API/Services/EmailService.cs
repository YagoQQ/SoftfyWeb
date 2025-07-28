using MailKit.Net.Smtp;
using MimeKit;

namespace SoftfyWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarEmailAsync(string destinatario, string asunto, string contenido)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration["Smtp:FromName"], _configuration["Smtp:FromEmail"]));
            message.To.Add(new MailboxAddress(destinatario, destinatario));
            message.Subject = asunto;

            message.Body = new TextPart("plain")
            {
                Text = contenido
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["Smtp:Host"], int.Parse(_configuration["Smtp:Port"]), false);
            await client.AuthenticateAsync(_configuration["Smtp:UserName"], _configuration["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
