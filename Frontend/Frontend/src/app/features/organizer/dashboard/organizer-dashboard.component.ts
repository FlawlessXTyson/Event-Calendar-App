import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { EventService } from '../../../core/services/event.service';
import { PaymentService } from '../../../core/services/payment.service';
import { EventResponse, OrganizerEarnings, ApprovalStatus } from '../../../core/models/models';

@Component({
  selector: 'app-organizer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './organizer-dashboard.component.html',
  styleUrl: './organizer-dashboard.component.css'
})
export class OrganizerDashboardComponent implements OnInit {
  auth     = inject(AuthService);
  private eventSvc = inject(EventService);
  private paySvc   = inject(PaymentService);

  myEvents = signal<EventResponse[]>([]);
  earnings = signal<OrganizerEarnings|null>(null);

  approved = () => this.myEvents().filter(e => e.approvalStatus === ApprovalStatus.APPROVED).length;
  pending  = () => this.myEvents().filter(e => e.approvalStatus === ApprovalStatus.PENDING).length;

  ngOnInit() {
    this.eventSvc.getMyEvents().subscribe({ next: evs => this.myEvents.set(evs), error: () => {} });
    this.paySvc.getOrganizerEarnings().subscribe({ next: e => this.earnings.set(e), error: () => {} });
  }

  approvalLabel(s: number) { return {1:'Pending',2:'Approved',3:'Rejected'}[s] ?? s; }
  approvalBadge(s: number) { return {1:'badge-warning',2:'badge-success',3:'badge-danger'}[s] ?? 'badge-gray'; }
}
