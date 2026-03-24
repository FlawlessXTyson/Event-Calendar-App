import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info' | 'reminder';

export interface Toast {
  id: number;
  type: ToastType;
  title: string;
  message: string;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _counter = 0;
  readonly toasts = signal<Toast[]>([]);

  private add(type: ToastType, title: string, message: string, duration = 5000): void {
    const id = ++this._counter;
    this.toasts.update(list => [...list, { id, type, title, message, duration }]);
    setTimeout(() => this.remove(id), duration);
  }

  success(message: string, title = 'Success')   { this.add('success', title, message); }
  error(message: string, title = 'Error')        { this.add('error', title, message, 7000); }
  warning(message: string, title = 'Warning')    { this.add('warning', title, message, 6000); }
  info(message: string, title = 'Info')          { this.add('info', title, message); }
  reminder(message: string, title = 'Reminder')  { this.add('reminder', title, message, 10000); }

  remove(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }
}
