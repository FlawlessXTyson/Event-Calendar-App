import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserDto, CreateUserRequest, UpdateUserRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/User`;

  /** GET /api/User/me */
  getMe() {
    return this.http.get<UserDto>(`${this.base}/me`);
  }

  /** PUT /api/User/me — name required, email checked for duplicates */
  updateMe(dto: UpdateUserRequest) {
    return this.http.put<UserDto>(`${this.base}/me`, dto);
  }

  /** GET /api/User — ADMIN only */
  getAll() {
    return this.http.get<UserDto[]>(this.base);
  }

  /** POST /api/User — ADMIN only */
  create(dto: CreateUserRequest) {
    return this.http.post<UserDto>(this.base, dto);
  }

  /** DELETE /api/User/{id} — ADMIN only, returns 204 */
  delete(id: number) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
