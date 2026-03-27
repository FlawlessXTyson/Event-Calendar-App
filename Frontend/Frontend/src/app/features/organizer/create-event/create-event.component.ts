import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { EventService } from '../../../core/services/event.service';
import { ToastService } from '../../../core/services/toast.service';
import { futureDateOnly } from '../../../core/validators/custom.validators';

// ── Location Data ──────────────────────────────────────────────────────────────
const LOCATION_DATA: Record<string, Record<string, string[]>> = {
  'India': {
    'Tamil Nadu':    ['Chennai', 'Coimbatore', 'Madurai', 'Salem', 'Tiruchirappalli', 'Tirunelveli', 'Vellore'],
    'Karnataka':     ['Bangalore', 'Mysore', 'Hubli', 'Mangalore', 'Belgaum', 'Davangere'],
    'Maharashtra':   ['Mumbai', 'Pune', 'Nagpur', 'Nashik', 'Aurangabad', 'Solapur'],
    'Delhi':         ['New Delhi', 'Dwarka', 'Rohini', 'Saket', 'Noida', 'Gurugram'],
    'West Bengal':   ['Kolkata', 'Howrah', 'Durgapur', 'Asansol', 'Siliguri'],
    'Telangana':     ['Hyderabad', 'Warangal', 'Nizamabad', 'Karimnagar'],
    'Gujarat':       ['Ahmedabad', 'Surat', 'Vadodara', 'Rajkot', 'Bhavnagar'],
    'Rajasthan':     ['Jaipur', 'Jodhpur', 'Udaipur', 'Kota', 'Ajmer'],
    'Uttar Pradesh': ['Lucknow', 'Kanpur', 'Agra', 'Varanasi', 'Allahabad', 'Meerut'],
    'Kerala':        ['Thiruvananthapuram', 'Kochi', 'Kozhikode', 'Thrissur', 'Kollam'],
  },
  'USA': {
    'California':  ['Los Angeles', 'San Francisco', 'San Diego', 'San Jose', 'Sacramento'],
    'New York':    ['New York City', 'Buffalo', 'Rochester', 'Albany', 'Syracuse'],
    'Texas':       ['Houston', 'Dallas', 'Austin', 'San Antonio', 'Fort Worth'],
    'Florida':     ['Miami', 'Orlando', 'Tampa', 'Jacksonville', 'Tallahassee'],
    'Illinois':    ['Chicago', 'Aurora', 'Naperville', 'Joliet', 'Rockford'],
    'Washington':  ['Seattle', 'Spokane', 'Tacoma', 'Bellevue', 'Olympia'],
  },
  'UK': {
    'England':  ['London', 'Manchester', 'Birmingham', 'Leeds', 'Liverpool', 'Bristol', 'Sheffield'],
    'Scotland': ['Edinburgh', 'Glasgow', 'Aberdeen', 'Dundee', 'Inverness'],
    'Wales':    ['Cardiff', 'Swansea', 'Newport', 'Bangor'],
  },
  'Canada': {
    'Ontario':          ['Toronto', 'Ottawa', 'Mississauga', 'Hamilton', 'London'],
    'British Columbia': ['Vancouver', 'Victoria', 'Surrey', 'Burnaby', 'Kelowna'],
    'Quebec':           ['Montreal', 'Quebec City', 'Laval', 'Gatineau'],
    'Alberta':          ['Calgary', 'Edmonton', 'Red Deer', 'Lethbridge'],
  },
  'Australia': {
    'New South Wales': ['Sydney', 'Newcastle', 'Wollongong', 'Canberra'],
    'Victoria':        ['Melbourne', 'Geelong', 'Ballarat', 'Bendigo'],
    'Queensland':      ['Brisbane', 'Gold Coast', 'Sunshine Coast', 'Townsville'],
    'Western Australia': ['Perth', 'Fremantle', 'Bunbury', 'Geraldton'],
  },
};

@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div style="max-width:760px;">
      <div style="margin-bottom:24px;">
        <h1 style="font-size:1.5rem;">Create New Event</h1>
        <p>Fill in the details. Events are reviewed by an admin before going live.</p>
      </div>
      <div class="alert alert-info" style="margin-bottom:20px;background:linear-gradient(135deg,var(--primary-light),#E0F2FE);border-left:4px solid var(--primary);border-radius:var(--r);">
        <span class="material-icons-round" style="color:var(--primary);">auto_awesome</span>
        <div>
          <div style="font-weight:700;color:var(--primary);font-size:.95rem;">✨ Bring your vision to life!</div>
          <div style="font-size:.85rem;color:var(--text-secondary);margin-top:2px;">Create an event that inspires, connects, and leaves a lasting impression. Your next great event starts here.</div>
        </div>
      </div>
      <div class="card card-body">
        <form [formGroup]="form" (ngSubmit)="submit()">

          <!-- Basic Info -->
          <h3 style="margin-bottom:16px;font-size:1rem;">Basic Information</h3>
          <div class="form-group">
            <label class="form-label">Event Title <span style="color:var(--danger)">*</span></label>
            <input formControlName="title" type="text" class="form-control" [class.is-invalid]="fi('title')"
              placeholder="e.g. Annual Tech Conference 2025" maxlength="200" />
            @if (fi('title')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Title is required</div> }
          </div>
          <div class="form-group">
            <label class="form-label">Description</label>
            <textarea formControlName="description" class="form-control"
              placeholder="Describe your event, agenda, speakers..." rows="4"></textarea>
          </div>

          <!-- Location Dropdowns -->
          <h3 style="margin-bottom:12px;font-size:1rem;">Location</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Country <span style="color:var(--danger)">*</span></label>
              <select formControlName="country" class="form-control" [class.is-invalid]="fi('country')"
                (change)="onCountryChange()">
                <option value="">-- Select Country --</option>
                @for (c of countries; track c) {
                  <option [value]="c">{{ c }}</option>
                }
              </select>
              @if (fi('country')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Country is required</div> }
            </div>
            <div class="form-group">
              <label class="form-label">State <span style="color:var(--danger)">*</span></label>
              <select formControlName="state" class="form-control" [class.is-invalid]="fi('state')"
                [disabled]="!form.get('country')?.value"
                (change)="onStateChange()">
                <option value="">-- Select State --</option>
                @for (s of availableStates(); track s) {
                  <option [value]="s">{{ s }}</option>
                }
              </select>
              @if (fi('state')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>State is required</div> }
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">City <span style="color:var(--danger)">*</span></label>
              <select formControlName="city" class="form-control" [class.is-invalid]="fi('city')"
                [disabled]="!form.get('state')?.value">
                <option value="">-- Select City --</option>
                @for (c of availableCities(); track c) {
                  <option [value]="c">{{ c }}</option>
                }
              </select>
              @if (fi('city')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>City is required</div> }
            </div>
            <div class="form-group">
              <label class="form-label">Address / Venue</label>
              <input formControlName="address" type="text" class="form-control"
                placeholder="e.g. Trade Centre, Hall A, 2nd Floor" />
              <div class="form-hint">Detailed venue address (optional)</div>
            </div>
          </div>
          @if (locationPreview()) {
            <div class="form-group">
              <div style="background:var(--surface-2);border-radius:var(--r-sm);padding:10px 14px;font-size:.875rem;color:var(--text-secondary);">
                <span class="material-icons-round" style="font-size:16px;vertical-align:middle;margin-right:4px;">location_on</span>
                <strong>Location:</strong> {{ locationPreview() }}
              </div>
            </div>
          }

          <div class="divider"></div>
          <h3 style="margin-bottom:16px;font-size:1rem;">Date &amp; Time <span style="color:var(--text-muted);font-size:.8rem;">(All times in local timezone)</span></h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Start Date <span style="color:var(--danger)">*</span></label>
              <input formControlName="eventDate" type="date" class="form-control" [class.is-invalid]="fi('eventDate')" [min]="today" />
              @if (fi('eventDate')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ dateError() }}</div> }
            </div>
            <div class="form-group">
              <label class="form-label">End Date <span style="color:var(--text-muted);">(multi-day, optional)</span></label>
              <input formControlName="eventEndDate" type="date" class="form-control"
                [class.is-invalid]="fi('eventEndDate')"
                [min]="form.get('eventDate')?.value || today" />
              @if (fi('eventEndDate')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>End date must be on or after start date</div> }
              <div class="form-hint">Leave blank for single-day events</div>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Start Time <span style="color:var(--danger)">*</span></label>
              <input formControlName="startTime" type="time" class="form-control" [class.is-invalid]="fi('startTime')" />
              @if (fi('startTime')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Start time is required</div> }
            </div>
            <div class="form-group">
              <label class="form-label">End Time <span style="color:var(--danger)">*</span></label>
              <input formControlName="endTime" type="time" class="form-control" [class.is-invalid]="fi('endTime')" />
              @if (fi('endTime')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ endTimeError() }}</div> }
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Registration Deadline <span style="color:var(--text-muted);">(optional)</span></label>
            <input formControlName="registrationDeadline" type="datetime-local" class="form-control"
              [class.is-invalid]="fi('registrationDeadline')" />
            @if (fi('registrationDeadline')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>{{ deadlineError() }}</div> }
            <div class="form-hint">Must be in the future and before the event start date/time.</div>
          </div>

          <div class="divider"></div>
          <h3 style="margin-bottom:16px;font-size:1rem;">Capacity &amp; Pricing</h3>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Seats Limit <span style="color:var(--text-muted);">(optional)</span></label>
              <input formControlName="seatsLimit" type="number" class="form-control"
                [class.is-invalid]="fi('seatsLimit')"
                placeholder="Leave blank for unlimited" min="1" />
              @if (fi('seatsLimit')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Seats must be at least 1</div> }
              <div class="form-hint">For free events, seats are checked at registration. For paid events, at payment.</div>
            </div>
            <div></div>
          </div>
          <div class="form-group">
            <label style="display:flex;align-items:center;gap:10px;cursor:pointer;">
              <input formControlName="isPaidEvent" type="checkbox" style="width:18px;height:18px;accent-color:var(--primary);" />
              <span class="form-label" style="margin:0;">This is a paid event</span>
            </label>
          </div>
          @if (form.get('isPaidEvent')?.value) {
            <div class="form-group">
              <label class="form-label">Ticket Price (&#8377;) <span style="color:var(--danger)">*</span></label>
              <input formControlName="ticketPrice" type="number" class="form-control"
                [class.is-invalid]="fi('ticketPrice')"
                placeholder="e.g. 499" min="1" step="0.01" />
              @if (fi('ticketPrice')) { <div class="form-error"><span class="material-icons-round" style="font-size:14px;">error</span>Ticket price must be greater than 0 for paid events</div> }
              <div class="form-hint">Platform deducts 10% commission. You receive 90%.</div>
            </div>
          }

          <div style="display:flex;gap:12px;margin-top:8px;">
            <button type="submit" class="btn btn-primary" [disabled]="saving()">
              @if (saving()) { <div class="spinner spinner-sm"></div> }
              @else { <span class="material-icons-round">send</span> }
              Submit for Review
            </button>
            <button type="button" class="btn btn-ghost" (click)="router.navigate(['/organizer/my-events'])">Cancel</button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class CreateEventComponent implements OnInit {
  private eventSvc = inject(EventService);
  private toast    = inject(ToastService);
  readonly router  = inject(Router);
  private fb       = inject(FormBuilder);

  saving = signal(false);
  today  = new Date().toISOString().split('T')[0];

  countries        = Object.keys(LOCATION_DATA);
  availableStates  = signal<string[]>([]);
  availableCities  = signal<string[]>([]);

  form = this.fb.group({
    title:                ['', [Validators.required, Validators.maxLength(200)]],
    description:          [''],
    country:              ['', Validators.required],
    state:                ['', Validators.required],
    city:                 ['', Validators.required],
    address:              [''],
    eventDate:            ['', [Validators.required, futureDateOnly()]],
    eventEndDate:         [''],
    startTime:            ['', Validators.required],
    endTime:              ['', Validators.required],
    registrationDeadline: [''],
    seatsLimit:           [null as number|null, Validators.min(1)],
    isPaidEvent:          [false],
    ticketPrice:          [0]
  }, { validators: this.crossValidate.bind(this) });

  ngOnInit() {}

  onCountryChange() {
    const country = this.form.get('country')?.value ?? '';
    this.availableStates.set(country ? Object.keys(LOCATION_DATA[country] ?? {}) : []);
    this.availableCities.set([]);
    this.form.patchValue({ state: '', city: '' });
  }

  onStateChange() {
    const country = this.form.get('country')?.value ?? '';
    const state   = this.form.get('state')?.value ?? '';
    this.availableCities.set(country && state ? (LOCATION_DATA[country]?.[state] ?? []) : []);
    this.form.patchValue({ city: '' });
  }

  locationPreview(): string {
    const v = this.form.value;
    const parts = [v.city, v.state, v.country].filter(Boolean);
    if (!parts.length) return '';
    const loc = parts.join(', ');
    return v.address ? `${loc} — ${v.address}` : loc;
  }

  crossValidate(g: AbstractControl) {
    // End time must be after start time
    const start = g.get('startTime')?.value as string;
    const end   = g.get('endTime')?.value as string;
    if (start && end && end <= start) {
      g.get('endTime')?.setErrors({ endBeforeStart: true });
    } else if (g.get('endTime')?.hasError('endBeforeStart')) {
      g.get('endTime')?.setErrors(null);
    }

    // End date must be >= start date
    const eDate    = g.get('eventDate')?.value as string;
    const eEndDate = g.get('eventEndDate')?.value as string;
    if (eDate && eEndDate && eEndDate < eDate) {
      g.get('eventEndDate')?.setErrors({ endBeforeStart: true });
    } else if (g.get('eventEndDate')?.hasError('endBeforeStart')) {
      g.get('eventEndDate')?.setErrors(null);
    }

    // Paid event price validation
    const isPaid = g.get('isPaidEvent')?.value as boolean;
    const price  = g.get('ticketPrice')?.value;
    if (isPaid && (!price || price <= 0)) {
      g.get('ticketPrice')?.setErrors({ invalidPrice: true });
    } else if (g.get('ticketPrice')?.hasError('invalidPrice')) {
      g.get('ticketPrice')?.setErrors(null);
    }

    // Registration deadline must be in future and before event start
    const deadline = g.get('registrationDeadline')?.value as string;
    if (deadline) {
      const now = new Date();
      const dl  = new Date(deadline);
      if (dl <= now) {
        g.get('registrationDeadline')?.setErrors({ pastDeadline: true });
      } else if (eDate && start) {
        const eventStart = new Date(`${eDate}T${start}`);
        if (dl >= eventStart) {
          g.get('registrationDeadline')?.setErrors({ deadlineAfterEvent: true });
        } else if (g.get('registrationDeadline')?.hasError('deadlineAfterEvent') || g.get('registrationDeadline')?.hasError('pastDeadline')) {
          g.get('registrationDeadline')?.setErrors(null);
        }
      } else if (g.get('registrationDeadline')?.hasError('pastDeadline')) {
        g.get('registrationDeadline')?.setErrors(null);
      }
    }

    return null;
  }

  fi(f: string) { const c = this.form.get(f); return c?.invalid && c?.touched; }

  dateError() {
    const e = this.form.get('eventDate')?.errors;
    return e?.['required'] ? 'Start date is required' : e?.['pastDate'] ? 'Date cannot be in the past' : '';
  }

  endTimeError() {
    const e = this.form.get('endTime')?.errors;
    return e?.['required'] ? 'End time is required' : e?.['endBeforeStart'] ? 'End time must be after start time' : '';
  }

  deadlineError() {
    const e = this.form.get('registrationDeadline')?.errors;
    if (e?.['pastDeadline']) return 'Registration deadline must be in the future';
    if (e?.['deadlineAfterEvent']) return 'Registration deadline must be before the event start';
    return '';
  }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.toast.warning('Please fix the validation errors before submitting.', 'Form Invalid');
      return;
    }

    const v = this.form.value;

    // Frontend replication of backend validations
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const eventDate = new Date(v.eventDate!);
    if (eventDate < today) {
      this.toast.error('Event date cannot be in the past.', 'Validation Error');
      return;
    }

    if (v.startTime && v.endTime && v.endTime <= v.startTime) {
      this.toast.error('End time must be after start time.', 'Validation Error');
      return;
    }

    if (v.eventEndDate && v.eventDate && v.eventEndDate < v.eventDate) {
      this.toast.error('Event end date cannot be before start date.', 'Validation Error');
      return;
    }

    if (v.isPaidEvent && (!v.ticketPrice || v.ticketPrice <= 0)) {
      this.toast.error('Paid event must have a ticket price greater than 0.', 'Validation Error');
      return;
    }

    if (v.seatsLimit !== null && v.seatsLimit !== undefined && v.seatsLimit < 1) {
      this.toast.error('Seats limit must be at least 1.', 'Validation Error');
      return;
    }

    if (v.registrationDeadline) {
      const dl = new Date(v.registrationDeadline);
      if (dl <= new Date()) {
        this.toast.error('Registration deadline must be in the future.', 'Validation Error');
        return;
      }
      if (v.eventDate && v.startTime) {
        const eventStart = new Date(`${v.eventDate}T${v.startTime}`);
        if (dl >= eventStart) {
          this.toast.error('Registration deadline must be before the event start.', 'Validation Error');
          return;
        }
      }
    }

    this.saving.set(true);

    // Build location string: "City, State, Country — Address"
    const locationParts = [v.city, v.state, v.country].filter(Boolean);
    const location = v.address
      ? `${locationParts.join(', ')} — ${v.address}`
      : locationParts.join(', ');

    const payload: any = {
      title:       v.title,
      description: v.description || '',
      location,
      eventDate:   v.eventDate,
      startTime:   v.startTime + ':00',
      endTime:     v.endTime + ':00',
      isPaidEvent: v.isPaidEvent,
      ticketPrice: v.isPaidEvent ? v.ticketPrice : 0,
    };
    if (v.eventEndDate)         payload.eventEndDate         = v.eventEndDate;
    if (v.registrationDeadline) payload.registrationDeadline = new Date(v.registrationDeadline!).toISOString();
    if (v.seatsLimit)           payload.seatsLimit           = v.seatsLimit;

    this.eventSvc.create(payload).subscribe({
      next: ev => {
        this.toast.success(`Event "${ev.title}" submitted for admin review!`, 'Event Created');
        this.router.navigate(['/organizer/my-events']);
      },
      error: () => this.saving.set(false)
    });
  }
}
