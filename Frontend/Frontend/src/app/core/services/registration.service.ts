import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RegisterEventRequest, RegistrationWrapper, EventRegistrationResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/EventRegistration`;

  /**
   * POST /api/EventRegistration
   * Backend validates: event approved, active, not started, not ended,
   * seats available (for free events), no duplicate registration
   */
  register(dto: RegisterEventRequest) {
    return this.http.post<RegistrationWrapper>(this.base, dto);
  }

  /**
   * PUT /api/EventRegistration/{id}/cancel
   * Backend: validates ownership, event not started, auto-refunds payment if paid
   */
  cancel(registrationId: number) {
    return this.http.put<RegistrationWrapper>(`${this.base}/${registrationId}/cancel`, {});
  }

  /** GET /api/EventRegistration/my */
  getMyRegistrations() {
    return this.http.get<EventRegistrationResponse[]>(`${this.base}/my`);
  }

  /** GET /api/EventRegistration/event/{eventId} — ORGANIZER/ADMIN */
  getByEvent(eventId: number) {
    return this.http.get<EventRegistrationResponse[]>(`${this.base}/event/${eventId}`);
  }
}
