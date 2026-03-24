import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleRequestService } from '../../../core/services/role-request.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-request-role',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="max-width:560px;">
      <div style="margin-bottom:24px;"><h1 style="font-size:1.5rem;">Upgrade to Organizer</h1><p>Request organizer access to create and manage events.</p></div>
      <div class="card card-body">
        <div style="text-align:center;padding:32px 0;">
          <div style="width:80px;height:80px;border-radius:50%;background:var(--primary-light);display:flex;align-items:center;justify-content:center;margin:0 auto 20px;">
            <span class="material-icons-round" style="font-size:40px;color:var(--primary);">upgrade</span>
          </div>
          <h2 style="margin-bottom:12px;">Become an Organizer</h2>
          <p style="max-width:380px;margin:0 auto 24px;">As an organizer, you can create public events, manage registrations, view attendee lists, track earnings, and more.</p>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;max-width:400px;margin:0 auto 28px;text-align:left;">
            @for (feat of features; track feat) {
              <div style="display:flex;align-items:center;gap:8px;font-size:.875rem;">
                <span class="material-icons-round" style="color:var(--success);font-size:18px;">check_circle</span>{{ feat }}
              </div>
            }
          </div>
          @if (submitted()) {
            <div class="alert alert-success" style="display:inline-flex;gap:8px;padding:12px 20px;">
              <span class="material-icons-round">check_circle</span>
              <span>Request submitted! An admin will review it shortly.</span>
            </div>
          } @else {
            <button type="button" class="btn btn-primary btn-lg" [disabled]="loading()" (click)="request()">
              @if (loading()) { <div class="spinner spinner-sm"></div> }
              @else { <span class="material-icons-round">send</span> }
              Submit Request
            </button>
          }
        </div>
      </div>
    </div>
  `
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
      error: () => this.loading.set(false)
    });
  }
}
