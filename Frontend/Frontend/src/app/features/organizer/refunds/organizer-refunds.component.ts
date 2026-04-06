import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../../core/services/payment.service';
import { PaymentResponse, PaymentStatus, PagedResult } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-refunds',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organizer-refunds.component.html',
  styleUrl: './organizer-refunds.component.css'
})
export class OrganizerRefundsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  PaymentStatus  = PaymentStatus;

  refunds      = signal<PaymentResponse[]>([]);
  loading      = signal(true);
  page         = signal(1);
  pageSize     = 10;
  totalRecords = signal(0);

  totalRefunded = computed(() => this.refunds().reduce((s, r) => s + (r.refundedAmount ?? 0), 0));
  totalPages    = computed(() => Math.max(1, Math.ceil(this.totalRecords() / this.pageSize)));

  pageNums = computed(() => {
    const total = this.totalPages(), cur = this.page();
    const pages: number[] = [];
    for (let i = Math.max(1, cur - 2); i <= Math.min(total, cur + 2); i++) pages.push(i);
    return pages;
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.paySvc.getOrganizerRefunds(this.page(), this.pageSize).subscribe({
      next: (res: PagedResult<PaymentResponse>) => {
        this.refunds.set(res.data);
        this.totalRecords.set(res.totalRecords);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  goPage(p: number) { this.page.set(p); this.load(); }
}
