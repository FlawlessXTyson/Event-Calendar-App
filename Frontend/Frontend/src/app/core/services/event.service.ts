import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  EventResponse, CreateEventRequest, PagedResult, RefundSummary
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Event`;

  // ── Public / Anonymous ──────────────────────────────────────────────────
  /** GET /api/Event — only ACTIVE + APPROVED events */
  getAll()                      { return this.http.get<EventResponse[]>(this.base); }

  /** GET /api/Event/{id} */
  getById(id: number)           { return this.http.get<EventResponse>(`${this.base}/${id}`); }

  /** GET /api/Event/search?keyword= */
  search(keyword: string)       {
    return this.http.get<EventResponse[]>(`${this.base}/search`, { params: { keyword } });
  }

  /** GET /api/Event/range?start=&end= */
  getByRange(start: string, end: string) {
    const params = new HttpParams().set('start', start).set('end', end);
    return this.http.get<EventResponse[]>(`${this.base}/range`, { params });
  }

  /** GET /api/Event/paged?pageNumber=&pageSize= */
  getPaged(pageNumber: number, pageSize: number) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<EventResponse>>(`${this.base}/paged`, { params });
  }

  // ── ORGANIZER / ADMIN ────────────────────────────────────────────────────
  /** POST /api/Event — backend sets category=PUBLIC, visibility=PUBLIC, createdByUserId from JWT */
  create(dto: CreateEventRequest) { return this.http.post<EventResponse>(this.base, dto); }

  /** GET /api/Event/my — events created by the logged-in organizer */
  getMyEvents()                 { return this.http.get<EventResponse[]>(`${this.base}/my`); }

  /** GET /api/Event/my/paged — paginated + optional date filter */
  getMyEventsPaged(pageNumber: number, pageSize: number, filterDate?: string) {
    let params = `pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (filterDate) params += `&filterDate=${filterDate}`;
    return this.http.get<{ pageNumber: number; pageSize: number; totalRecords: number; data: EventResponse[] }>(`${this.base}/my/paged?${params}`);
  }

  /** PUT /api/Event/{id}/cancel — auto-refunds all payments */
  cancel(id: number)            { return this.http.put<EventResponse>(`${this.base}/${id}/cancel`, {}); }

  /** GET /api/Event/{id}/refund-summary */
  getRefundSummary(id: number)  { return this.http.get<RefundSummary>(`${this.base}/${id}/refund-summary`); }

  // ── ADMIN ONLY ───────────────────────────────────────────────────────────
  /** POST /api/Event/{id}/approve */
  approve(id: number)           { return this.http.post<EventResponse>(`${this.base}/${id}/approve`, {}); }

  /** POST /api/Event/{id}/reject */
  reject(id: number)            { return this.http.post<EventResponse>(`${this.base}/${id}/reject`, {}); }

  /** DELETE /api/Event/{id} */
  delete(id: number)            { return this.http.delete<EventResponse>(`${this.base}/${id}`); }

  /** GET /api/Event/pending */
  getPending()                  { return this.http.get<EventResponse[]>(`${this.base}/pending`); }
  getPendingPaged(p: number, s: number) { return this.http.get<PagedResult<EventResponse>>(`${this.base}/pending/paged?pageNumber=${p}&pageSize=${s}`); }

  /** GET /api/Event/rejected */
  getRejected()                 { return this.http.get<EventResponse[]>(`${this.base}/rejected`); }
  getRejectedPaged(p: number, s: number) { return this.http.get<PagedResult<EventResponse>>(`${this.base}/rejected/paged?pageNumber=${p}&pageSize=${s}`); }

  /** GET /api/Event/approved */
  getApproved()                 { return this.http.get<EventResponse[]>(`${this.base}/approved`); }
  getApprovedPaged(p: number, s: number) { return this.http.get<PagedResult<EventResponse>>(`${this.base}/approved/paged?pageNumber=${p}&pageSize=${s}`); }

  /** GET /api/Event/expired — ADMIN only */
  getExpired()                  { return this.http.get<EventResponse[]>(`${this.base}/expired`); }
  getExpiredPaged(p: number, s: number) { return this.http.get<PagedResult<EventResponse>>(`${this.base}/expired/paged?pageNumber=${p}&pageSize=${s}`); }

  getCancelledPaged(p: number, s: number) { return this.http.get<PagedResult<EventResponse>>(`${this.base}/cancelled/paged?pageNumber=${p}&pageSize=${s}`); }

  /** GET /api/Event/all/paged — ADMIN all events with optional search */
  getAllPaged(p: number, s: number, search?: string) {
    let url = `${this.base}/all/paged?pageNumber=${p}&pageSize=${s}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    return this.http.get<PagedResult<EventResponse>>(url);
  }

  // ── USER ONLY ────────────────────────────────────────────────────────────
  /** GET /api/Event/registered — events user is registered for */
  getRegistered()               { return this.http.get<EventResponse[]>(`${this.base}/registered`); }
}
