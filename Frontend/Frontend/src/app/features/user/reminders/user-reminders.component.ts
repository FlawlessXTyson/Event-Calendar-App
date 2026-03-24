import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ReminderService } from '../../../core/services/reminder.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderResponse } from '../../../core/models/models';

@Component({
  selector: 'app-user-reminders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div>
      <div class="section-header" style="margin-bottom:24px;">
        <div><h1 style="font-size:1.5rem;">Reminders</h1><p>Get notified before your events</p></div>
        <button type="button" class="btn btn-primary btn-sm" (click)="showForm.set(!showForm())">
          <span class="material-icons-round">{{ showForm() ? 'close' : 'add' }}</span>
          {{ showForm() ? 'Cancel' : 'New Reminder' }}
        </button>
      </div>

      @if (showForm()) {
        <div class="card card-body" style="margin-bottom:24px;">
          <h3 style="margin-bottom:16px;">Create Reminder</h3>
          <div class="alert alert-info" style="margin-bottom:16px;">
            <span class="material-icons-round">info</span>
            <div>Provide either a <strong>specific date/time</strong> OR an <strong>Event ID + minutes before</strong> — not both.</div>
          </div>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="form-group">
              <label class="form-label">Reminder Title <span style="color:var(--danger)">*</span></label>
              <input formControlName="reminderTitle" type="text" class="form-control" [class.is-invalid]="fi('reminderTitle')" placeholder="e.g. Prepare for conference" />
              @if (fi('reminderTitle')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Title is required</div> }
            </div>
            <div class="form-row">
              <div class="form-group">
                <label class="form-label">Specific Date & Time</label>
                <input formControlName="reminderDateTime" type="datetime-local" class="form-control" />
                <div class="form-hint">Leave blank if using Event ID + minutes</div>
              </div>
              <div class="form-group">
                <label class="form-label">Event ID (optional)</label>
                <input formControlName="eventId" type="number" class="form-control" placeholder="Event ID" />
              </div>
            </div>
            <div class="form-group">
              <label class="form-label">Minutes Before Event Start</label>
              <input formControlName="minutesBefore" type="number" class="form-control" placeholder="e.g. 30" min="1" />
              <div class="form-hint">Only used if Event ID is set. Cannot use with Date/Time.</div>
            </div>
            <div style="display:flex;gap:10px;">
              <button type="submit" class="btn btn-primary" [disabled]="saving()">
                @if (saving()) { <div class="spinner spinner-sm"></div> } @else { <span class="material-icons-round">save</span> }
                Save Reminder
              </button>
            </div>
          </form>
        </div>
      }

      @if (loading()) {
        <div class="loading-center"><div class="spinner"></div></div>
      } @else if (reminders().length === 0) {
        <div class="empty-state"><span class="material-icons-round empty-icon">notifications_off</span><h3>No reminders set</h3><p>Create reminders to get notified before events.</p></div>
      } @else {
        <div style="display:flex;flex-direction:column;gap:12px;">
          @for (r of reminders(); track r.reminderId) {
            <div class="card card-body" style="display:flex;align-items:center;justify-content:space-between;gap:16px;">
              <div style="display:flex;align-items:center;gap:14px;">
                <div style="width:42px;height:42px;background:var(--primary-light);border-radius:var(--r-sm);display:flex;align-items:center;justify-content:center;">
                  <span class="material-icons-round" style="color:var(--primary);">notifications_active</span>
                </div>
                <div>
                  <div style="font-weight:600;">{{ r.reminderTitle }}</div>
                  <div style="font-size:.82rem;color:var(--text-muted);">
                    <span class="material-icons-round" style="font-size:14px;vertical-align:middle;">schedule</span>
                    {{ r.reminderDateTime | date:'MMM d, y, h:mm a' }}
                    @if (r.eventId) { &nbsp;·&nbsp; Event #{{ r.eventId }} }
                  </div>
                </div>
              </div>
              <button type="button" class="btn btn-ghost btn-icon" [disabled]="deleting() === r.reminderId" (click)="del(r.reminderId)" title="Delete">
                @if (deleting() === r.reminderId) { <div class="spinner spinner-sm"></div> }
                @else { <span class="material-icons-round" style="color:var(--danger);">delete</span> }
              </button>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class UserRemindersComponent implements OnInit {
  private svc   = inject(ReminderService);
  private toast = inject(ToastService);
  private fb    = inject(FormBuilder);

  reminders = signal<ReminderResponse[]>([]);
  loading   = signal(true);
  saving    = signal(false);
  deleting  = signal<number|null>(null);
  showForm  = signal(false);

  form = this.fb.group({
    reminderTitle:   ['', Validators.required],
    reminderDateTime:[''],
    eventId:         [null as number|null],
    minutesBefore:   [null as number|null]
  });

  fi(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }

  ngOnInit() { this.load(); }
  load() { this.svc.getMyReminders().subscribe({ next: rs => { this.reminders.set(rs); this.loading.set(false); }, error: () => this.loading.set(false) }); }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.value;
    const hasDateTime    = !!v.reminderDateTime;
    const hasMinutes     = !!v.minutesBefore && v.minutesBefore! > 0;
    if (hasDateTime && hasMinutes) {
      this.toast.error('Provide either a date/time OR event ID + minutes before — not both.', 'Validation Error');
      return;
    }
    if (!hasDateTime && !hasMinutes) {
      this.toast.error('Provide either a specific date/time or an event ID with minutes before start.', 'Validation Error');
      return;
    }
    this.saving.set(true);
    const payload: any = { reminderTitle: v.reminderTitle };
    if (hasDateTime) payload.reminderDateTime = new Date(v.reminderDateTime!).toISOString();
    if (v.eventId)   payload.eventId        = v.eventId;
    if (hasMinutes)  payload.minutesBefore  = v.minutesBefore;

    this.svc.create(payload).subscribe({
      next: r => {
        this.reminders.update(rs => [r, ...rs]);
        this.toast.success('Reminder set! You will be notified at the scheduled time.', 'Reminder Created');
        this.form.reset();
        this.showForm.set(false);
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  del(id: number) {
    if (!confirm('Delete this reminder?')) return;
    this.deleting.set(id);
    this.svc.delete(id).subscribe({
      next: () => { this.reminders.update(rs => rs.filter(r => r.reminderId !== id)); this.toast.success('Reminder deleted.', 'Deleted'); this.deleting.set(null); },
      error: () => this.deleting.set(null)
    });
  }
}
