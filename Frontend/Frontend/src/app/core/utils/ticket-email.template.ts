import { TicketResponse } from '../models/models';

export function buildTicketEmailHtml(t: TicketResponse): string {
  const eventType = t.isPaidEvent ? `Paid Event — ₹${t.amountPaid.toFixed(2)}` : 'Free Event';
  const paymentRow = t.isPaidEvent
    ? `<tr><td style="padding:8px;color:#555;">Amount Paid</td><td style="padding:8px;font-weight:bold;">₹${t.amountPaid.toFixed(2)}</td></tr>`
    : '';

  const eventDate = new Date(t.eventDate).toLocaleDateString('en-IN', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
  });

  const generatedAt = new Date(t.generatedAt).toLocaleString('en-IN', {
    month: 'short', day: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });

  return `<!DOCTYPE html>
<html>
<head><meta charset="utf-8"/></head>
<body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;">
  <div style="max-width:600px;margin:auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);">
    <div style="background:#4f46e5;padding:24px;text-align:center;">
      <h1 style="color:#fff;margin:0;font-size:22px;">🎟 Event Ticket Confirmed</h1>
    </div>
    <div style="padding:24px;">
      <p style="font-size:16px;color:#333;">Hi <strong>${t.userName}</strong>,</p>
      <p style="color:#555;">Your booking is confirmed. Here are your ticket details:</p>
      <table style="width:100%;border-collapse:collapse;margin-top:16px;">
        <tr style="background:#f9f9f9;">
          <td style="padding:8px;color:#555;">Ticket ID</td>
          <td style="padding:8px;font-weight:bold;">#${t.ticketId}</td>
        </tr>
        <tr>
          <td style="padding:8px;color:#555;">Event</td>
          <td style="padding:8px;font-weight:bold;">${t.eventTitle}</td>
        </tr>
        <tr style="background:#f9f9f9;">
          <td style="padding:8px;color:#555;">Description</td>
          <td style="padding:8px;">${t.eventDescription}</td>
        </tr>
        <tr>
          <td style="padding:8px;color:#555;">Location</td>
          <td style="padding:8px;">${t.eventLocation}</td>
        </tr>
        <tr style="background:#f9f9f9;">
          <td style="padding:8px;color:#555;">Date</td>
          <td style="padding:8px;">${eventDate}</td>
        </tr>
        <tr>
          <td style="padding:8px;color:#555;">Time</td>
          <td style="padding:8px;">${t.startTime ?? 'TBD'} – ${t.endTime ?? 'TBD'}</td>
        </tr>
        <tr style="background:#f9f9f9;">
          <td style="padding:8px;color:#555;">Event Type</td>
          <td style="padding:8px;">${eventType}</td>
        </tr>
        ${paymentRow}
        <tr style="background:#f9f9f9;">
          <td style="padding:8px;color:#555;">Ticket Generated</td>
          <td style="padding:8px;">${generatedAt} UTC</td>
        </tr>
      </table>
      <p style="margin-top:24px;color:#555;">Please carry this email or your ticket ID at the event entrance.</p>
      <p style="color:#555;">See you there!</p>
    </div>
    <div style="background:#f4f4f4;padding:16px;text-align:center;">
      <p style="color:#aaa;font-size:12px;margin:0;">Event Calendar App — This is an automated email, please do not reply.</p>
    </div>
  </div>
</body>
</html>`;
}
