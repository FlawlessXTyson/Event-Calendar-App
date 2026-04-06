import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  PaymentRequest, PaymentResponse,
  CommissionSummary, OrganizerEarnings, EventWiseEarnings, PagedResult
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Payment`;

  /**
   * POST /api/Payment
   * Backend validates:
   *  - must be registered first
   *  - event must be paid, active, approved
   *  - event not started, not ended
   *  - seats not full (for paid events)
   *  - no duplicate payment
   *  - calculates commission = price * commissionPercentage / 100
   */
  create(req: PaymentRequest) {
    return this.http.post<PaymentResponse>(this.base, req);
  }

  /** GET /api/Payment/my-payments */
  getMyPayments() {
    return this.http.get<PaymentResponse[]>(`${this.base}/my-payments`);
  }

  /** GET /api/Payment/event/{eventId} — ORGANIZER/ADMIN */
  getByEvent(eventId: number) {
    return this.http.get<PaymentResponse[]>(`${this.base}/event/${eventId}`);
  }

  /** GET /api/Payment/all — ADMIN only */
  getAll() {
    return this.http.get<PaymentResponse[]>(`${this.base}/all`);
  }

  /** PUT /api/Payment/{paymentId}/refund — ADMIN only, event must not have started */
  refund(paymentId: number) {
    return this.http.put<PaymentResponse>(`${this.base}/${paymentId}/refund`, {});
  }

  /** GET /api/Payment/commission-summary — ADMIN only */
  getCommissionSummary() {
    return this.http.get<CommissionSummary>(`${this.base}/commission-summary`);
  }

  /**
   * GET /api/Payment/organizer-earnings — ORGANIZER only
   * Aggregates: totalRevenue, totalCommission, netEarnings, totalTransactions
   */
  getOrganizerEarnings() {
    return this.http.get<OrganizerEarnings>(`${this.base}/organizer-earnings`);
  }

  /**
   * GET /api/Payment/organizer-event-earnings — ORGANIZER only
   * Per-event breakdown sorted by totalRevenue desc
   */
  getEventWiseEarnings() {
    return this.http.get<EventWiseEarnings[]>(`${this.base}/organizer-event-earnings`);
  }

  /** GET /api/Payment/organizer-refunds — ORGANIZER only, paginated */
  getOrganizerRefunds(pageNumber: number, pageSize: number) {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<PaymentResponse>>(`${this.base}/organizer-refunds`, { params });
  }
}
