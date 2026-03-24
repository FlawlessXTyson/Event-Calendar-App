import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { RoleChangeRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class RoleRequestService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/RoleRequest`;

  /**
   * POST /api/RoleRequest/request-organizer
   * Backend: checks not already organizer, no pending request
   */
  requestOrganizer() {
    return this.http.post<string>(`${this.base}/request-organizer`, {});
  }

  /** GET /api/RoleRequest/pending — ADMIN only */
  getPending() {
    return this.http.get<RoleChangeRequest[]>(`${this.base}/pending`);
  }

  /** PUT /api/RoleRequest/{id}/approve — ADMIN only, updates user role to ORGANIZER */
  approve(id: number) {
    return this.http.put<string>(`${this.base}/${id}/approve`, {});
  }

  /** PUT /api/RoleRequest/{id}/reject — ADMIN only */
  reject(id: number) {
    return this.http.put<string>(`${this.base}/${id}/reject`, {});
  }
}
