using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Ticket;
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

        public async Task SendTicketEmailAsync(string toEmail, string toName, TicketResponseDTO ticket)
        {
            var smtp = _config.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtp["SenderName"] ?? "Event Calendar App",
                smtp["SenderEmail"]!));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"Your Ticket for {ticket.EventTitle}";

            message.Body = new TextPart("html")
            {
                Text = BuildTicketHtml(ticket)
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["Host"]!,
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(smtp["Username"]!, smtp["Password"]!);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildTicketHtml(TicketResponseDTO t)
        {
            var eventType = t.IsPaidEvent ? $"Paid Event — ₹{t.AmountPaid:F2}" : "Free Event";
            var paymentRow = t.IsPaidEvent
                ? $"<tr><td style='padding:8px;color:#555;'>Amount Paid</td><td style='padding:8px;font-weight:bold;'>₹{t.AmountPaid:F2}</td></tr>"
                : string.Empty;

            return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;'>
  <div style='max-width:600px;margin:auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);'>
    <div style='background:#4f46e5;padding:24px;text-align:center;'>
      <h1 style='color:#fff;margin:0;font-size:22px;'>🎟 Event Ticket Confirmed</h1>
    </div>
    <div style='padding:24px;'>
      <p style='font-size:16px;color:#333;'>Hi <strong>{t.UserName}</strong>,</p>
      <p style='color:#555;'>Your booking is confirmed. Here are your ticket details:</p>
      <table style='width:100%;border-collapse:collapse;margin-top:16px;'>
        <tr style='background:#f9f9f9;'>
          <td style='padding:8px;color:#555;'>Ticket ID</td>
          <td style='padding:8px;font-weight:bold;'>#{t.TicketId}</td>
        </tr>
        <tr>
          <td style='padding:8px;color:#555;'>Event</td>
          <td style='padding:8px;font-weight:bold;'>{t.EventTitle}</td>
        </tr>
        <tr style='background:#f9f9f9;'>
          <td style='padding:8px;color:#555;'>Description</td>
          <td style='padding:8px;'>{t.EventDescription}</td>
        </tr>
        <tr>
          <td style='padding:8px;color:#555;'>Location</td>
          <td style='padding:8px;'>{t.EventLocation}</td>
        </tr>
        <tr style='background:#f9f9f9;'>
          <td style='padding:8px;color:#555;'>Date</td>
          <td style='padding:8px;'>{t.EventDate:dddd, MMMM dd, yyyy}</td>
        </tr>
        <tr>
          <td style='padding:8px;color:#555;'>Time</td>
          <td style='padding:8px;'>{t.StartTime ?? "TBD"} – {t.EndTime ?? "TBD"}</td>
        </tr>
        <tr style='background:#f9f9f9;'>
          <td style='padding:8px;color:#555;'>Event Type</td>
          <td style='padding:8px;'>{eventType}</td>
        </tr>
        {paymentRow}
        <tr style='background:#f9f9f9;'>
          <td style='padding:8px;color:#555;'>Ticket Generated</td>
          <td style='padding:8px;'>{t.GeneratedAt:MMM dd, yyyy HH:mm} UTC</td>
        </tr>
      </table>
      <p style='margin-top:24px;color:#555;'>Please carry this email or your ticket ID at the event entrance.</p>
      <p style='color:#555;'>See you there!</p>
    </div>
    <div style='background:#f4f4f4;padding:16px;text-align:center;'>
      <p style='color:#aaa;font-size:12px;margin:0;'>Event Calendar App — This is an automated email, please do not reply.</p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
