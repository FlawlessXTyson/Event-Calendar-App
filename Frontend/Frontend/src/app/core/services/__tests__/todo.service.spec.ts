import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TodoService } from '../todo.service';
import { environment } from '../../../../environments/environment';
import { TodoStatus } from '../../models/models';

describe('TodoService', () => {
  let service: TodoService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/Todo`;

  const mockTodo = {
    todoId: 1, userId: 42, taskTitle: 'Review PR',
    dueDate: '2026-03-25', status: TodoStatus.PENDING,
    createdAt: '2026-03-20T10:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(TodoService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create() sends POST to /api/Todo', () => {
    service.create({ taskTitle: 'Review PR', dueDate: '2026-03-25' }).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.taskTitle).toBe('Review PR');
    req.flush(mockTodo);
  });

  it('create() without dueDate is valid', () => {
    service.create({ taskTitle: 'No deadline task' }).subscribe();
    const req = http.expectOne(base);
    expect(req.request.body.taskTitle).toBe('No deadline task');
    req.flush({ ...mockTodo, dueDate: undefined });
  });

  it('getMyTodos() sends GET to /api/Todo/me', () => {
    service.getMyTodos().subscribe();
    const req = http.expectOne(`${base}/me`);
    expect(req.request.method).toBe('GET');
    req.flush([mockTodo]);
  });

  it('getMyTodos() returns array of todos', () => {
    let result: any[];
    service.getMyTodos().subscribe(ts => result = ts);
    http.expectOne(`${base}/me`).flush([mockTodo]);
    expect(result![0].taskTitle).toBe('Review PR');
    expect(result![0].status).toBe(TodoStatus.PENDING);
  });

  it('complete() sends PUT to /api/Todo/{id}/complete', () => {
    service.complete(1).subscribe();
    const req = http.expectOne(`${base}/1/complete`);
    expect(req.request.method).toBe('PUT');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('update() sends PUT to /api/Todo/{id} with new title', () => {
    service.update(1, { taskTitle: 'Updated PR Review' }).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.taskTitle).toBe('Updated PR Review');
    req.flush({ ...mockTodo, taskTitle: 'Updated PR Review' });
  });

  it('update() can include dueDate', () => {
    service.update(1, { taskTitle: 'Task', dueDate: '2026-04-01' }).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.body.dueDate).toBe('2026-04-01');
    req.flush(mockTodo);
  });

  it('delete() sends DELETE to /api/Todo/{id}', () => {
    service.delete(1).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('delete() uses correct id in URL', () => {
    service.delete(55).subscribe();
    const req = http.expectOne(`${base}/55`);
    expect(req.request.url).toContain('/55');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
