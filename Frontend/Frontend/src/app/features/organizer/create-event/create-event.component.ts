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
  templateUrl: './create-event.component.html',
  styleUrl: './create-event.component.css'
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
    registrationDeadline: ['', Validators.required],
    seatsLimit:           [null as number|null, [Validators.required, Validators.min(1)]],
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
    const start      = g.get('startTime')?.value as string;
    const end        = g.get('endTime')?.value as string;
    const eDate      = g.get('eventDate')?.value as string;
    const eEndDate   = g.get('eventEndDate')?.value as string;

    // End time vs start time — only validate when it's a SINGLE-DAY event
    // (no eventEndDate, or eventEndDate === eventDate)
    const isSameDay = !eEndDate || eEndDate === eDate;
    if (isSameDay && start && end && end <= start) {
      g.get('endTime')?.setErrors({ endBeforeStart: true });
    } else if (g.get('endTime')?.hasError('endBeforeStart')) {
      g.get('endTime')?.setErrors(null);
    }

    // End date must be >= start date
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

    // Registration deadline — required + must be in future and before event start
    const deadline = g.get('registrationDeadline')?.value as string;
    if (!deadline) {
      g.get('registrationDeadline')?.setErrors({ required: true });
    } else {
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
    if (e?.['required'])           return 'Registration deadline is required';
    if (e?.['pastDeadline'])       return 'Registration deadline must be in the future';
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

    const isSameDay = !v.eventEndDate || v.eventEndDate === v.eventDate;
    if (isSameDay && v.startTime && v.endTime && v.endTime <= v.startTime) {
      this.toast.error('End time must be after start time for single-day events.', 'Validation Error');
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

    if (!v.seatsLimit || v.seatsLimit < 1) {
      this.toast.error('Seats limit is required and must be at least 1.', 'Validation Error');
      return;
    }

    if (!v.registrationDeadline) {
      this.toast.error('Registration deadline is required.', 'Validation Error');
      return;
    }
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
    payload.seatsLimit = v.seatsLimit;

    this.eventSvc.create(payload).subscribe({
      next: ev => {
        this.toast.success(`Event "${ev.title}" submitted for admin review!`, 'Event Created');
        this.router.navigate(['/organizer/my-events']);
      },
      error: () => this.saving.set(false)
    });
  }
}
