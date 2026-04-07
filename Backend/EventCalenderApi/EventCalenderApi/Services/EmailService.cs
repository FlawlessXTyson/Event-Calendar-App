using EventCalenderApi.Interfaces.ServiceInterfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EventCalenderApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var smtp = _config.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtp["SenderName"] ?? "Event Calendar App",
                smtp["SenderEmail"]!));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["Host"]!,
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(smtp["Username"]!, smtp["Password"]!);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
