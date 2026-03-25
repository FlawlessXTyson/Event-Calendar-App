import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuditLog } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/AuditLog`;

  getAll()                        { return this.http.get<AuditLog[]>(this.base); }
  getByUser(userId: number)       { return this.http.get<AuditLog[]>(`${this.base}/user/${userId}`); }
  getByEntity(entity: string)     { return this.http.get<AuditLog[]>(`${this.base}/entity/${entity}`); }
  getByAction(action: string)     { return this.http.get<AuditLog[]>(`${this.base}/action/${action}`); }
}
