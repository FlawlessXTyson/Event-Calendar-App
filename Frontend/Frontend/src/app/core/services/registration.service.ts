import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RegisterEventRequest, RegistrationWrapper, EventRegistrationResponse, PagedResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/EventRegistration`;

  register(dto: RegisterEventRequest) {
    return this.http.post<RegistrationWrapper>(this.base, dto);
  }

  cancel(registrationId: number) {
    return this.http.put<RegistrationWrapper>(`${this.base}/${registrationId}/cancel`, {});
  }

  getMyRegistrations() {
    return this.http.get<EventRegistrationResponse[]>(`${this.base}/my`);
  }

  getByEvent(eventId: number) {
    return this.http.get<EventRegistrationResponse[]>(`${this.base}/event/${eventId}`);
  }

  /** GET /api/EventRegistration/event/{eventId}/paged */
  getByEventPaged(eventId: number, pageNumber: number, pageSize: number, filterDate?: string) {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    if (filterDate) params = params.set('filterDate', filterDate);
    return this.http.get<PagedResult<EventRegistrationResponse>>(`${this.base}/event/${eventId}/paged`, { params });
  }
}
