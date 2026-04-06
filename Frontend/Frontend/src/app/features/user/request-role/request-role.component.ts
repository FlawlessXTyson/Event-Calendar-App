import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleRequestService } from '../../../core/services/role-request.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-request-role',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './request-role.component.html',
  styleUrl: './request-role.component.css'
})
export class RequestRoleComponent {
  private svc   = inject(RoleRequestService);
  private toast = inject(ToastService);
  loading   = signal(false);
  submitted = signal(false);
  features  = ['Create events','Manage registrations','View attendees','Track earnings','Refund summary'];

  request() {
    this.loading.set(true);
    this.svc.requestOrganizer().subscribe({
      next: msg => {
        this.toast.success(typeof msg === 'string' ? msg : 'Request submitted successfully!', 'Request Sent');
        this.submitted.set(true);
        this.loading.set(false);
      },
      error: (err : Error) => {console.error(err); this.loading.set(false)}
    });
  }
}
