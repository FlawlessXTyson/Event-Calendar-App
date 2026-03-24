import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CreateTodoRequest, UpdateTodoRequest, TodoResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Todo`;

  /** POST /api/Todo — userId set from JWT, dueDate cannot be in past */
  create(dto: CreateTodoRequest) {
    return this.http.post<TodoResponse>(this.base, dto);
  }

  /** GET /api/Todo/me — ordered by createdAt desc */
  getMyTodos() {
    return this.http.get<TodoResponse[]>(`${this.base}/me`);
  }

  /** PUT /api/Todo/{id}/complete — returns 204 NoContent */
  complete(id: number) {
    return this.http.put<void>(`${this.base}/${id}/complete`, {});
  }

  /** PUT /api/Todo/{id} — title required, dueDate cannot be past */
  update(id: number, dto: UpdateTodoRequest) {
    return this.http.put<TodoResponse>(`${this.base}/${id}`, dto);
  }

  /** DELETE /api/Todo/{id} — only owner can delete */
  delete(id: number) {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
