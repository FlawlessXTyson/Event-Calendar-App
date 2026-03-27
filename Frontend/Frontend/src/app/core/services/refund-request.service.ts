import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RefundRequestResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class RefundRequestService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/RefundRequest`;

  /** POST /api/RefundRequest/{paymentId} — user requests refund */
  create(paymentId: number) {
    return this.http.post<RefundRequestResponse>(`${this.base}/${paymentId}`, {});
  }

  /** GET /api/RefundRequest/pending — ADMIN only */
  getPending() {
    return this.http.get<RefundRequestResponse[]>(`${this.base}/pending`);
  }

  /** PUT /api/RefundRequest/{id}/approve — ADMIN only */
  approve(id: number, refundPercentage: number) {
    return this.http.put<RefundRequestResponse>(`${this.base}/${id}/approve`, { refundPercentage });
  }

  /** PUT /api/RefundRequest/{id}/reject — ADMIN only */
  reject(id: number) {
    return this.http.put<RefundRequestResponse>(`${this.base}/${id}/reject`, {});
  }
}
