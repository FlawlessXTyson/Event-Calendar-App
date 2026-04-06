import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditLogService } from '../../../core/services/audit-log.service';
import { AuditLog } from '../../../core/models/models';

const ACTION_STYLES: Record<string, { bg: string; color: string }> = {
  ADDED:    { bg: '#D1FAE5', color: '#065F46' },
  MODIFIED: { bg: '#DBEAFE', color: '#1E40AF' },
  DELETED:  { bg: '#FEE2E2', color: '#991B1B' },
  LOGIN:    { bg: '#EDE9FE', color: '#5B21B6' },
  REGISTER: { bg: '#FEF3C7', color: '#92400E' },
};

@Component({
  selector: 'app-admin-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-audit-logs.component.html'
})
export class AdminAuditLogsComponent implements OnInit {
  private svc = inject(AuditLogService);

  logs    = signal<AuditLog[]>([]);
  loading = signal(true);
  page    = signal(1);
  readonly pageSize = 50;

  search       = '';
  filterAction = '';
  filterEntity = '';
  filterRole   = '';

  actions = ['ADDED', 'MODIFIED', 'DELETED', 'LOGIN', 'REGISTER'];

  entities = computed(() => [...new Set(this.logs().map(l => l.entity))].sort());

  filtered = computed(() => {
    const s = this.search.toLowerCase();
    return this.logs().filter(l => {
      const matchSearch = !s || l.userId.toString().includes(s) || l.entity.toLowerCase().includes(s) || (l.userName ?? '').toLowerCase().includes(s);
      const matchAction = !this.filterAction || l.action === this.filterAction;
      const matchEntity = !this.filterEntity || l.entity === this.filterEntity;
      const matchRole   = !this.filterRole   || l.role === this.filterRole;
      return matchSearch && matchAction && matchEntity && matchRole;
    });
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));

  paginated = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });

  ngOnInit() {
    this.svc.getAll().subscribe({
      next: logs => { this.logs.set(logs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  clearFilters() {
    this.search = '';
    this.filterAction = '';
    this.filterEntity = '';
    this.filterRole = '';
    this.page.set(1);
  }

  countAction(action: string) {
    return this.logs().filter(l => l.action === action).length;
  }

  /** Convert UTC datetime string to IST (UTC+5:30) */
  toIst(utcStr: string): Date {
    // Always treat as UTC by appending Z if no timezone info present
    const s = utcStr.endsWith('Z') || utcStr.includes('+') ? utcStr : utcStr + 'Z';
    return new Date(s);
  }

  actionStyle(action: string) {
    return ACTION_STYLES[action] ?? { bg: '#F1F5F9', color: '#475569' };
  }

  roleStyle(role: string) {
    const map: Record<string, { bg: string; color: string }> = {
      ADMIN:     { bg: '#FEE2E2', color: '#991B1B' },
      ORGANIZER: { bg: '#FEF3C7', color: '#92400E' },
      USER:      { bg: '#DBEAFE', color: '#1E40AF' },
      SYSTEM:    { bg: '#F1F5F9', color: '#475569' },
    };
    return map[role] ?? { bg: '#F1F5F9', color: '#475569' };
  }
}
