import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentService } from '../../../core/services/payment.service';
import { OrganizerEarnings, EventWiseEarnings } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-earnings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './organizer-earnings.component.html',
  styleUrl: './organizer-earnings.component.css'
})
export class OrganizerEarningsComponent implements OnInit {
  private paySvc = inject(PaymentService);
  summary       = signal<OrganizerEarnings|null>(null);
  eventEarnings = signal<EventWiseEarnings[]>([]);
  loading       = signal(true);

  ngOnInit() {
    let done = 0;
    const check = () => { if (++done === 2) this.loading.set(false); };
    this.paySvc.getOrganizerEarnings().subscribe({ next: e => { this.summary.set(e); check(); }, error: () => check() });
    this.paySvc.getEventWiseEarnings().subscribe({ next: e => { this.eventEarnings.set(e); check(); }, error: () => check() });
  }
}
