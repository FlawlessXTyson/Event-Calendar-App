import { Component, inject, signal, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDto } from '../../../core/models/models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.css']
})
export class NotificationBellComponent implements OnInit {
  readonly notifSvc = inject(NotificationService);
  open = signal(false);

  ngOnInit() {
    this.notifSvc.loadNotifications();
  }

  toggle() { this.open.update(v => !v); }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    const target = e.target as HTMLElement;
    if (!target.closest('.notif-bell-wrapper')) {
      this.open.set(false);
    }
  }

  onMarkRead(n: NotificationDto, e: MouseEvent) {
    e.stopPropagation();
    if (!n.isRead) this.notifSvc.markAsRead(n.notificationId);
  }

  onMarkAll(e: MouseEvent) {
    e.stopPropagation();
    this.notifSvc.markAllAsRead();
  }

  typeIcon(type: string): string {
    switch (type) {
      case 'REFUND':       return 'currency_rupee';
      case 'WARNING':      return 'warning';
      case 'EVENT_UPDATE': return 'event';
      default:             return 'info';
    }
  }

  typeColor(type: string): string {
    switch (type) {
      case 'REFUND':       return 'var(--success)';
      case 'WARNING':      return 'var(--warning)';
      case 'EVENT_UPDATE': return 'var(--primary)';
      default:             return 'var(--info)';
    }
  }

  timeAgo(dateStr: string): string {
    // Ensure the string is parsed as UTC — backend returns DateTime.UtcNow
    // but .NET may omit the 'Z' suffix, causing JS to treat it as local time
    const utcStr = dateStr.endsWith('Z') || dateStr.includes('+') ? dateStr : dateStr + 'Z';
    const diff = Date.now() - new Date(utcStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1)  return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)  return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }
}
