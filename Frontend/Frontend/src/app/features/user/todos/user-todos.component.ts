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
  templateUrl: './user-todos.component.html',
  styleUrl: './user-todos.component.css'
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
