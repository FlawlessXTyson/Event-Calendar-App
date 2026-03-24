import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { TodoService } from '../../../core/services/todo.service';
import { ToastService } from '../../../core/services/toast.service';
import { TodoResponse, TodoStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-todos',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div>
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.5rem;">To-Do List</h1><p>{{ pending() }} task{{ pending() !== 1 ? 's' : '' }} pending</p></div>
        <button type="button" class="btn btn-primary btn-sm" (click)="showAdd.set(!showAdd())">
          <span class="material-icons-round">{{ showAdd() ? 'close' : 'add_task' }}</span>
          {{ showAdd() ? 'Cancel' : 'Add Task' }}
        </button>
      </div>

      @if (showAdd()) {
        <div class="card card-body" style="margin-bottom:20px;">
          <form [formGroup]="addForm" (ngSubmit)="addTodo()">
            <div class="form-row">
              <div class="form-group" style="margin-bottom:0;">
                <label class="form-label">Task Title <span style="color:var(--danger)">*</span></label>
                <input formControlName="taskTitle" type="text" class="form-control" [class.is-invalid]="fi('taskTitle')" placeholder="What needs to be done?" />
                @if (fi('taskTitle')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Title is required</div> }
              </div>
              <div class="form-group" style="margin-bottom:0;">
                <label class="form-label">Due Date (optional)</label>
                <input formControlName="dueDate" type="date" class="form-control" [min]="today" />
                <div class="form-hint">Cannot be in the past</div>
              </div>
            </div>
            <div style="margin-top:14px;">
              <button type="submit" class="btn btn-primary btn-sm" [disabled]="saving()">
                @if (saving()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">add</span> }
                Add Task
              </button>
            </div>
          </form>
        </div>
      }

      <!-- Filter tabs -->
      <div class="tabs">
        <button type="button" class="tab-btn" [class.active]="filter() === 'all'" (click)="filter.set('all')">All ({{ todos().length }})</button>
        <button type="button" class="tab-btn" [class.active]="filter() === 'pending'" (click)="filter.set('pending')">Pending ({{ pending() }})</button>
        <button type="button" class="tab-btn" [class.active]="filter() === 'done'" (click)="filter.set('done')">Completed ({{ completed() }})</button>
      </div>

      @if (loading()) { <div class="loading-center"><div class="spinner"></div></div> }
      @else if (filtered().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">task_alt</span><h3>{{ emptyMsg() }}</h3></div>
      } @else {
        <div style="display:flex;flex-direction:column;gap:10px;">
          @for (t of filtered(); track t.todoId) {
            <div class="card card-body" style="display:flex;align-items:center;gap:14px;" [style.opacity]="t.status === TodoStatus.COMPLETED ? '0.7' : '1'">
              <button type="button" (click)="complete(t)" [disabled]="t.status === TodoStatus.COMPLETED || completing() === t.todoId"
                style="background:none;border:none;cursor:pointer;flex-shrink:0;padding:0;">
                @if (completing() === t.todoId) { <div class="spinner spinner-sm"></div> }
                @else {
                  <span class="material-icons-round" [style.color]="t.status === TodoStatus.COMPLETED ? 'var(--success)' : 'var(--border)'">
                    {{ t.status === TodoStatus.COMPLETED ? 'check_circle' : 'radio_button_unchecked' }}
                  </span>
                }
              </button>
              <div style="flex:1;">
                @if (editing() === t.todoId) {
                  <form [formGroup]="editForm" (ngSubmit)="saveEdit(t)" style="display:flex;gap:8px;align-items:center;">
                    <input formControlName="taskTitle" type="text" class="form-control" style="flex:1;" />
                    <input formControlName="dueDate" type="date" class="form-control" style="width:160px;" [min]="today" />
                    <button type="submit" class="btn btn-primary btn-sm">Save</button>
                    <button type="button" class="btn btn-ghost btn-sm" (click)="editing.set(null)">Cancel</button>
                  </form>
                } @else {
                  <div style="font-weight:600;" [style.text-decoration]="t.status === TodoStatus.COMPLETED ? 'line-through' : 'none'">{{ t.taskTitle }}</div>
                  @if (t.dueDate) { <div style="font-size:.8rem;color:var(--text-muted);">Due {{ t.dueDate | date:'MMM d, y' }}</div> }
                }
              </div>
              @if (t.status === TodoStatus.PENDING && editing() !== t.todoId) {
                <button type="button" class="btn btn-ghost btn-icon btn-sm" (click)="startEdit(t)" title="Edit"><span class="material-icons-round" style="font-size:18px;">edit</span></button>
              }
              <button type="button" class="btn btn-ghost btn-icon btn-sm" [disabled]="deleting() === t.todoId" (click)="del(t.todoId)" title="Delete">
                @if (deleting() === t.todoId) { <div class="spinner spinner-sm"></div> }
                @else { <span class="material-icons-round" style="color:var(--danger);font-size:18px;">delete</span> }
              </button>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class UserTodosComponent implements OnInit {
  private svc   = inject(TodoService);
  private toast = inject(ToastService);
  private fb    = inject(FormBuilder);
  TodoStatus    = TodoStatus;

  todos     = signal<TodoResponse[]>([]);
  loading   = signal(true);
  saving    = signal(false);
  completing= signal<number|null>(null);
  deleting  = signal<number|null>(null);
  editing   = signal<number|null>(null);
  showAdd   = signal(false);
  filter    = signal<'all'|'pending'|'done'>('all');
  today     = new Date().toISOString().split('T')[0];

  pending   = () => this.todos().filter(t => t.status === TodoStatus.PENDING).length;
  completed = () => this.todos().filter(t => t.status === TodoStatus.COMPLETED).length;
  filtered  = computed(() => {
    const f = this.filter();
    if (f === 'pending') return this.todos().filter(t => t.status === TodoStatus.PENDING);
    if (f === 'done')    return this.todos().filter(t => t.status === TodoStatus.COMPLETED);
    return this.todos();
  });
  emptyMsg  = () => ({ all:'No tasks yet. Add your first task!', pending:'No pending tasks. Well done! 🎉', done:'No completed tasks yet.' }[this.filter()]);

  addForm  = this.fb.group({ taskTitle: ['', Validators.required], dueDate: [''] });
  editForm = this.fb.group({ taskTitle: ['', Validators.required], dueDate: [''] });

  fi(f: string) { const c = this.addForm.get(f); return c?.invalid && c?.touched; }

  ngOnInit() { this.load(); }
  load() { this.svc.getMyTodos().subscribe({ next: ts => { this.todos.set(ts); this.loading.set(false); }, error: () => this.loading.set(false) }); }

  addTodo() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.addForm.value;
    this.svc.create({ taskTitle: v.taskTitle!, dueDate: v.dueDate || undefined }).subscribe({
      next: t => {
        this.todos.update(ts => [t, ...ts]);
        this.toast.success(`Task "${t.taskTitle}" added!`, 'Task Added');
        this.addForm.reset();
        this.showAdd.set(false);
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  complete(t: TodoResponse) {
    if (t.status === TodoStatus.COMPLETED) return;
    this.completing.set(t.todoId);
    this.svc.complete(t.todoId).subscribe({
      next: () => {
        this.todos.update(ts => ts.map(x => x.todoId === t.todoId ? {...x, status: TodoStatus.COMPLETED} : x));
        this.toast.success(`"${t.taskTitle}" marked as complete! ✓`, 'Task Completed');
        this.completing.set(null);
      },
      error: () => this.completing.set(null)
    });
  }

  startEdit(t: TodoResponse) {
    this.editing.set(t.todoId);
    this.editForm.setValue({ taskTitle: t.taskTitle, dueDate: t.dueDate?.split('T')[0] ?? '' });
  }

  saveEdit(t: TodoResponse) {
    if (this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }
    const v = this.editForm.value;
    this.svc.update(t.todoId, { taskTitle: v.taskTitle!, dueDate: v.dueDate || undefined }).subscribe({
      next: updated => {
        this.todos.update(ts => ts.map(x => x.todoId === t.todoId ? updated : x));
        this.toast.success('Task updated.', 'Updated');
        this.editing.set(null);
      },
      error: () => {}
    });
  }

  del(id: number) {
    if (!confirm('Delete this task?')) return;
    this.deleting.set(id);
    this.svc.delete(id).subscribe({
      next: () => { this.todos.update(ts => ts.filter(t => t.todoId !== id)); this.toast.success('Task deleted.', 'Deleted'); this.deleting.set(null); },
      error: () => this.deleting.set(null)
    });
  }
}
