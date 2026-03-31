import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ReminderService } from '../../../core/services/reminder.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderResponse, EventResponse, RegistrationStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-reminders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './user-reminders.component.html',
  styleUrl: './user-reminders.component.css'
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
  mode      = signal<'datetime' | 'minutes'>('datetime');

  toggleForm() { this.showForm.update(v => !v); this.mode.set('datetime'); this.form.reset(); }

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
