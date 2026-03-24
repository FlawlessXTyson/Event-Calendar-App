import { TestBed, ComponentFixture, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UserTodosComponent } from '../todos/user-todos.component';
import { TodoService } from '../../../core/services/todo.service';
import { ToastService } from '../../../core/services/toast.service';
import { TodoStatus } from '../../../core/models/models';
import { of, throwError } from 'rxjs';

const pending = (id: number, title: string) => ({
  todoId: id, userId: 1, taskTitle: title,
  dueDate: '2026-04-01', status: TodoStatus.PENDING, createdAt: ''
});
const done = (id: number, title: string) => ({ ...pending(id, title), status: TodoStatus.COMPLETED });

describe('UserTodosComponent', () => {
  let fixture: ComponentFixture<UserTodosComponent>;
  let component: UserTodosComponent;
  let todoSvc: TodoService;
  let toastSvc: ToastService;

  const mockTodos = [pending(1, 'Task A'), pending(2, 'Task B'), done(3, 'Task C')];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserTodosComponent, HttpClientTestingModule], providers: [provideRouter([])]
    }).compileComponents();
    fixture   = TestBed.createComponent(UserTodosComponent);
    component = fixture.componentInstance;
    todoSvc   = TestBed.inject(TodoService);
    toastSvc  = TestBed.inject(ToastService);
    spyOn(todoSvc, 'getMyTodos').and.returnValue(of(mockTodos));
    fixture.detectChanges();
  });

  // ── Initialization ────────────────────────────────────────────────────────
  it('creates the component', () => {
    expect(component).toBeTruthy();
  });

  it('loads todos on init', () => {
    expect(todoSvc.getMyTodos).toHaveBeenCalled();
    expect(component.todos().length).toBe(3);
  });

  it('sets loading to false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  // ── Computed counts ───────────────────────────────────────────────────────
  it('pending() returns count of PENDING todos', () => {
    expect(component.pending()).toBe(2);
  });

  it('completed() returns count of COMPLETED todos', () => {
    expect(component.completed()).toBe(1);
  });

  // ── filter signal ─────────────────────────────────────────────────────────
  it('filter defaults to all', () => {
    expect(component.filter()).toBe('all');
  });

  it('filtered() returns all todos when filter=all', () => {
    component.filter.set('all');
    expect(component.filtered().length).toBe(3);
  });

  it('filtered() returns only pending when filter=pending', () => {
    component.filter.set('pending');
    expect(component.filtered().length).toBe(2);
    expect(component.filtered().every(t => t.status === TodoStatus.PENDING)).toBeTrue();
  });

  it('filtered() returns only completed when filter=done', () => {
    component.filter.set('done');
    expect(component.filtered().length).toBe(1);
    expect(component.filtered()[0].taskTitle).toBe('Task C');
  });

  // ── addTodo() ─────────────────────────────────────────────────────────────
  it('addTodo() marks form as touched when title empty', () => {
    component.addForm.get('taskTitle')!.setValue('');
    component.addTodo();
    expect(component.addForm.get('taskTitle')!.touched).toBeTrue();
  });

  it('addTodo() does not call service when title is empty', () => {
    const spy = spyOn(todoSvc, 'create');
    component.addTodo();
    expect(spy).not.toHaveBeenCalled();
  });

  it('addTodo() calls todoSvc.create with title and dueDate', () => {
    const newTodo = pending(4, 'New Task');
    const spy = spyOn(todoSvc, 'create').and.returnValue(of(newTodo));
    spyOn(toastSvc, 'success');
    component.addForm.setValue({ taskTitle: 'New Task', dueDate: '2026-04-10' });
    component.addTodo();
    expect(spy).toHaveBeenCalledWith({ taskTitle: 'New Task', dueDate: '2026-04-10' });
  });

  it('addTodo() prepends new todo to list', () => {
    const newTodo = pending(4, 'New Task');
    spyOn(todoSvc, 'create').and.returnValue(of(newTodo));
    spyOn(toastSvc, 'success');
    component.addForm.setValue({ taskTitle: 'New Task', dueDate: '' });
    component.addTodo();
    expect(component.todos()[0].taskTitle).toBe('New Task');
    expect(component.todos().length).toBe(4);
  });

  it('addTodo() resets form and hides panel on success', () => {
    const newTodo = pending(4, 'Task');
    spyOn(todoSvc, 'create').and.returnValue(of(newTodo));
    spyOn(toastSvc, 'success');
    component.showAdd.set(true);
    component.addForm.setValue({ taskTitle: 'Task', dueDate: '' });
    component.addTodo();
    expect(component.showAdd()).toBeFalse();
    expect(component.addForm.get('taskTitle')!.value).toBeFalsy();
  });

  // ── complete() ────────────────────────────────────────────────────────────
  it('complete() calls todoSvc.complete with correct id', () => {
    spyOn(todoSvc, 'complete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.complete(component.todos()[0]);
    expect(todoSvc.complete).toHaveBeenCalledWith(1);
  });

  it('complete() updates todo status to COMPLETED', () => {
    spyOn(todoSvc, 'complete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.complete(component.todos()[0]);
    expect(component.todos().find(t => t.todoId === 1)?.status).toBe(TodoStatus.COMPLETED);
  });

  it('complete() does nothing if todo is already COMPLETED', () => {
    const spy = spyOn(todoSvc, 'complete').and.returnValue(of(undefined));
    component.complete(done(3, 'Task C'));
    expect(spy).not.toHaveBeenCalled();
  });

  // ── startEdit() ───────────────────────────────────────────────────────────
  it('startEdit() sets editing signal to todo id', () => {
    component.startEdit(mockTodos[0]);
    expect(component.editing()).toBe(1);
  });

  it('startEdit() patches editForm with existing values', () => {
    component.startEdit(mockTodos[0]);
    expect(component.editForm.get('taskTitle')!.value).toBe('Task A');
  });

  // ── saveEdit() ────────────────────────────────────────────────────────────
  it('saveEdit() calls todoSvc.update with new title', () => {
    const updatedTodo = { ...mockTodos[0], taskTitle: 'Updated Task A' };
    const spy = spyOn(todoSvc, 'update').and.returnValue(of(updatedTodo));
    spyOn(toastSvc, 'success');
    component.startEdit(mockTodos[0]);
    component.editForm.setValue({ taskTitle: 'Updated Task A', dueDate: '' });
    component.saveEdit(mockTodos[0]);
    expect(spy).toHaveBeenCalledWith(1, { taskTitle: 'Updated Task A', dueDate: undefined });
  });

  it('saveEdit() updates todo in list', () => {
    const updatedTodo = { ...mockTodos[0], taskTitle: 'Updated' };
    spyOn(todoSvc, 'update').and.returnValue(of(updatedTodo));
    spyOn(toastSvc, 'success');
    component.startEdit(mockTodos[0]);
    component.editForm.setValue({ taskTitle: 'Updated', dueDate: '' });
    component.saveEdit(mockTodos[0]);
    expect(component.todos().find(t => t.todoId === 1)?.taskTitle).toBe('Updated');
    expect(component.editing()).toBeNull();
  });

  // ── del() ─────────────────────────────────────────────────────────────────
  it('del() calls todoSvc.delete and removes todo from list', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    spyOn(todoSvc, 'delete').and.returnValue(of(undefined));
    spyOn(toastSvc, 'success');
    component.del(1);
    expect(component.todos().find(t => t.todoId === 1)).toBeUndefined();
    expect(component.todos().length).toBe(2);
  });

  it('del() does NOT call service when confirm is cancelled', () => {
    spyOn(window, 'confirm').and.returnValue(false);
    const spy = spyOn(todoSvc, 'delete');
    component.del(1);
    expect(spy).not.toHaveBeenCalled();
  });

  // ── emptyMsg() ────────────────────────────────────────────────────────────
  it('emptyMsg() returns correct message for each filter', () => {
    component.filter.set('all');
    expect(component.emptyMsg()).toContain('first task');
    component.filter.set('pending');
    expect(component.emptyMsg()).toContain('No pending');
    component.filter.set('done');
    expect(component.emptyMsg()).toContain('No completed');
  });

  // ── today ─────────────────────────────────────────────────────────────────
  it('today is today\'s date in YYYY-MM-DD format', () => {
    const expected = new Date().toISOString().split('T')[0];
    expect(component.today).toBe(expected);
  });
});
