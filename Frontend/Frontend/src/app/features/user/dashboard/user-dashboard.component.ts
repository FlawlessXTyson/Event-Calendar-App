import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { EventService } from '../../../core/services/event.service';
import { PaymentService } from '../../../core/services/payment.service';
import { TodoService } from '../../../core/services/todo.service';
import { EventResponse, PaymentResponse, TodoResponse, PaymentStatus, TodoStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-dashboard.component.html',
  styleUrl: './user-dashboard.component.css'
})
export class UserDashboardComponent implements OnInit {
  auth = inject(AuthService);
  private eventSvc = inject(EventService);
  private paySvc   = inject(PaymentService);
  private todoSvc  = inject(TodoService);

  registeredEvents = signal<EventResponse[]>([]);
  payments         = signal<PaymentResponse[]>([]);
  todos            = signal<TodoResponse[]>([]);

  totalSpent   = () => this.payments().filter(p => p.status === PaymentStatus.SUCCESS).reduce((s,p) => s+p.amountPaid, 0);
  pendingTodos = () => this.todos().filter(t => t.status === TodoStatus.PENDING).length;

  ngOnInit() {
    this.eventSvc.getRegistered().subscribe({
      next: evs => {
        // Only keep events that haven't ended yet, sorted by date
        const now = new Date();
        const upcoming = evs
          .filter(e => !e.hasEnded && new Date(e.eventDate) >= new Date(now.toDateString()))
          .sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime());
        this.registeredEvents.set(upcoming);
      },
      error: () => {}
    });
    this.paySvc.getMyPayments().subscribe({ next: ps => this.payments.set(ps), error: () => {} });
    this.todoSvc.getMyTodos().subscribe({ next: ts => this.todos.set(ts), error: () => {} });
  }

  statusLabel(s: PaymentStatus) { return {1:'Pending',2:'Success',3:'Failed',4:'Refunded'}[s] ?? s; }
  statusBadge(s: PaymentStatus) { return {1:'badge-warning',2:'badge-success',3:'badge-danger',4:'badge-info'}[s] ?? 'badge-gray'; }
}
