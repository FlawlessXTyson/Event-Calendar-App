import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ReminderService } from '../../../core/services/reminder.service';
import { RegistrationService } from '../../../core/services/registration.service';
import { EventService } from '../../../core/services/event.service';
import { PaymentService } from '../../../core/services/payment.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReminderResponse, EventResponse, RegistrationStatus, PaymentStatus } from '../../../core/models/models';

@Component({
  selector: 'app-user-reminders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './user-reminders.component.html',
  styleUrl: './user-reminders.component.css'
})
export class UserRemindersComponent implements OnInit {
  private svc            = inject(ReminderService);
  private regSvc         = inject(RegistrationService);
  private eventSvc       = inject(EventService);
  private paymentSvc     = inject(PaymentService);
  private toast          = inject(ToastService);
  private fb             = inject(FormBuilder);

  reminders        = signal<ReminderResponse[]>([]);
  registeredEvents = signal<EventResponse[]>([]);
  loading          = signal(true);
  saving           = signal(false);
  deleting         = signal<number|null>(null);
  showForm         = signal(false);
  mode             = signal<'datetime' | 'event'>('datetime');

  // The selected event's start datetime (IST-local) for display hint
  selectedEventInfo = signal<{ title: string; dateTime: string } | null>(null);
  // Max allowed datetime for the picker in event mode (event start time)
  eventMaxDateTime  = signal<string>('');
  // Min allowed datetime (now, updated on form open)
  nowDateTime       = signal<string>('');

  toggleForm() {
    this.showForm.update(v => !v);
    this.mode.set('datetime');
    this.selectedEventInfo.set(null);
    this.eventMaxDateTime.set('');
    this.nowDateTime.set(this.toLocalDateTimeString(new Date()));
    this.form.reset();
  }

  /** Convert a Date to "YYYY-MM-DDTHH:mm" for datetime-local input */
  private toLocalDateTimeString(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  form = this.fb.group({
    reminderTitle:    ['', Validators.required],
    reminderDateTime: [''],   // used in both modes
    eventId:          [null as number|null]
  });

  fi(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }

  ngOnInit() {
    this.nowDateTime.set(this.toLocalDateTimeString(new Date()));
    this.load();
    this.loadRegisteredEvents();
  }

  load() {
    this.svc.getMyReminders().subscribe({
      next: rs => { this.reminders.set(rs); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadRegisteredEvents() {
    // Fetch registrations and payments in parallel, then filter
    this.regSvc.getMyRegistrations().subscribe({
      next: regs => {
        const activeRegs = regs.filter(r => r.status === RegistrationStatus.REGISTERED);
        const registeredEventIds = [...new Set(activeRegs.map(r => r.eventId))];
        if (registeredEventIds.length === 0) return;

        // Also fetch user's successful payments to know which paid events are paid-for
        this.paymentSvc.getMyPayments().subscribe({
          next: payments => {
            const paidEventIds = new Set(
              payments
                .filter(p => p.status === PaymentStatus.SUCCESS)
                .map(p => p.eventId)
            );

            const fetched: EventResponse[] = [];
            let pending = registeredEventIds.length;

            registeredEventIds.forEach(id => {
              this.eventSvc.getById(id).subscribe({
                next: ev => {
                  // Filter out ended events
                  const now = new Date();
                  const eventStart = this.getEventStartDate(ev);
                  const isEnded = eventStart ? eventStart <= now : false;

                  if (!isEnded) {
                    // For paid events: only show if payment is successful
                    // For free events: show if registered
                    if (!ev.isPaidEvent || paidEventIds.has(ev.eventId)) {
                      fetched.push(ev);
                    }
                  }
                  if (--pending === 0) this.registeredEvents.set(fetched);
                },
                error: () => { if (--pending === 0) this.registeredEvents.set(fetched); }
              });
            });
          },
          error: () => {
            // If payments fetch fails, fall back to showing only free registered events
            const fetched: EventResponse[] = [];
            let pending = registeredEventIds.length;
            registeredEventIds.forEach(id => {
              this.eventSvc.getById(id).subscribe({
                next: ev => {
                  const now = new Date();
                  const eventStart = this.getEventStartDate(ev);
                  const isEnded = eventStart ? eventStart <= now : false;
                  if (!isEnded && !ev.isPaidEvent) fetched.push(ev);
                  if (--pending === 0) this.registeredEvents.set(fetched);
                },
                error: () => { if (--pending === 0) this.registeredEvents.set(fetched); }
              });
            });
          }
        });
      }
    });
  }

  /** Returns the event's start as a Date, or null if not available */
  private getEventStartDate(ev: EventResponse): Date | null {
    if (!ev.eventDate) return null;
    const datePart = ev.eventDate.substring(0, 10);
    if (ev.startTime) {
      return new Date(`${datePart}T${ev.startTime.substring(0, 5)}`);
    }
    return new Date(`${datePart}T00:00`);
  }

  onEventSelected(eventIdStr: string) {
    const eventId = Number(eventIdStr);
    const ev = this.registeredEvents().find(e => e.eventId === eventId);
    if (!ev) { this.selectedEventInfo.set(null); this.eventMaxDateTime.set(''); return; }

    const datePart = ev.eventDate.substring(0, 10); // "YYYY-MM-DD"
    let hint = ev.title;
    let maxDt = '';

    if (ev.startTime) {
      const timePart = ev.startTime.substring(0, 5); // "HH:mm"
      hint += ` — ${datePart} at ${timePart}`;
      maxDt = `${datePart}T${timePart}`;
      // Pre-fill with now (or event date if event is future) so user picks a time before event
      const nowStr = this.toLocalDateTimeString(new Date());
      this.form.patchValue({ reminderDateTime: nowStr < maxDt ? nowStr : '' });
    } else {
      hint += ` — ${datePart}`;
      maxDt = `${datePart}T23:59`;
      this.form.patchValue({ reminderDateTime: this.toLocalDateTimeString(new Date()) });
    }

    this.eventMaxDateTime.set(maxDt);
    this.selectedEventInfo.set({ title: ev.title, dateTime: hint });
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.value;

    if (!v.reminderDateTime) {
      this.toast.error('Please select a reminder date and time.', 'Validation Error');
      return;
    }
    if (this.mode() === 'event' && !v.eventId) {
      this.toast.error('Please select an event.', 'Validation Error');
      return;
    }
    // Guard: reminder must be before event start
    if (this.mode() === 'event' && this.eventMaxDateTime() && v.reminderDateTime! > this.eventMaxDateTime()) {
      this.toast.error('Reminder time must be before the event starts.', 'Validation Error');
      return;
    }

    this.saving.set(true);
    const payload: any = {
      reminderTitle: v.reminderTitle,
      reminderDateTime: new Date(v.reminderDateTime!).toISOString()
    };
    if (this.mode() === 'event' && v.eventId) {
      payload.eventId = v.eventId;
    }

    this.svc.create(payload).subscribe({
      next: r => {
        this.reminders.update(rs => [r, ...rs]);
        this.toast.success('Reminder set! You will be notified at the scheduled time.', 'Reminder Created');
        this.form.reset();
        this.selectedEventInfo.set(null);
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
      next: () => {
        this.reminders.update(rs => rs.filter(r => r.reminderId !== id));
        this.toast.success('Reminder deleted.', 'Deleted');
        this.deleting.set(null);
      },
      error: () => this.deleting.set(null)
    });
  }

  getEventTitle(eventId: number): string {
    return this.registeredEvents().find(e => e.eventId === eventId)?.title ?? `Event #${eventId}`;
  }
}
