import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';

const ICONS: Record<string, string> = {
  success:  'check_circle',
  error:    'error',
  warning:  'warning_amber',
  info:     'info',
  reminder: 'notifications_active'
};

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      @for (t of toast.toasts(); track t.id) {
        <div class="toast toast-{{ t.type }}">
          <span class="material-icons-round toast-icon">{{ iconOf(t.type) }}</span>
          <div class="toast-content">
            <div class="toast-title">{{ t.title }}</div>
            <div class="toast-message">{{ t.message }}</div>
          </div>
          <button class="toast-close" type="button" (click)="toast.remove(t.id)">
            <span class="material-icons-round" style="font-size:18px;">close</span>
          </button>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  readonly toast = inject(ToastService);
  iconOf(type: string): string { return ICONS[type] ?? 'info'; }
}
