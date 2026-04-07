import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TicketResponse } from '../models/models';
import { buildTicketEmailHtml } from '../utils/ticket-email.template';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Ticket`;

  generate(eventId: number, paymentId?: number) {
    return this.http.post<TicketResponse>(`${this.base}/generate`, { eventId, paymentId });
  }

  getByEvent(eventId: number) {
    return this.http.get<TicketResponse>(`${this.base}/event/${eventId}`);
  }

  getMyTickets() {
    return this.http.get<TicketResponse[]>(`${this.base}/my`);
  }

  sendTicketEmail(ticket: TicketResponse, toEmail: string) {
    const htmlBody = buildTicketEmailHtml(ticket);
    return this.http.post(`${this.base}/send-email`, {
      toEmail,
      toName: ticket.userName,
      subject: `Your Ticket for ${ticket.eventTitle}`,
      htmlBody
    });
  }
}
