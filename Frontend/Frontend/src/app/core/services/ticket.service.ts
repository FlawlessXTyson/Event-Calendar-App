import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TicketResponse } from '../models/models';

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
}
