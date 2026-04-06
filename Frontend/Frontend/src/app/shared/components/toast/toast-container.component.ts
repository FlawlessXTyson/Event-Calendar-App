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
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.css']
})
export class ToastContainerComponent {
  readonly toast = inject(ToastService);
  iconOf(type: string): string { return ICONS[type] ?? 'info'; }
}
