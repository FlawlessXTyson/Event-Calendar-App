import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Validates email format strictly */
export function strictEmail(): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    const v = ctrl.value as string;
    if (!v) return null;
    const re = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
    return re.test(v) ? null : { invalidEmail: { message: 'Enter a valid email address (e.g. user@gmail.com)' } };
  };
}

/** Date must not be in the past */
export function futureDateOnly(): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    if (!ctrl.value) return null;
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const val   = new Date(ctrl.value);
    return val < today ? { pastDate: { message: 'Date cannot be in the past' } } : null;
  };
}

/** End date must be >= start date */
export function endDateAfterStart(startKey: string): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    const parent = ctrl.parent;
    if (!parent) return null;
    const start = parent.get(startKey)?.value;
    if (!start || !ctrl.value) return null;
    return new Date(ctrl.value) < new Date(start)
      ? { endBeforeStart: { message: 'End date must be on or after start date' } }
      : null;
  };
}

/** End time must be after start time (same day) */
export function endTimeAfterStart(startKey: string): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    const parent = ctrl.parent;
    if (!parent) return null;
    const start = parent.get(startKey)?.value as string;
    if (!start || !ctrl.value) return null;
    return ctrl.value <= start
      ? { endTimeBeforeStart: { message: 'End time must be after start time' } }
      : null;
  };
}

/** Ticket price must be > 0 for paid events */
export function paidEventPrice(isPaidKey: string): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    const parent = ctrl.parent;
    if (!parent) return null;
    const isPaid = parent.get(isPaidKey)?.value as boolean;
    if (!isPaid) return null;
    const price = parseFloat(ctrl.value);
    return (!price || price <= 0)
      ? { invalidPrice: { message: 'Ticket price must be greater than 0 for paid events' } }
      : null;
  };
}

/** Password minimum 6 chars */
export function minPassword(): ValidatorFn {
  return (ctrl: AbstractControl): ValidationErrors | null => {
    const v = ctrl.value as string;
    if (!v) return null;
    return v.length < 6
      ? { weakPassword: { message: 'Password must be at least 6 characters' } }
      : null;
  };
}

/** Confirm password matches */
export function passwordsMatch(ctrl: AbstractControl): ValidationErrors | null {
  const pw  = ctrl.get('password')?.value;
  const cpw = ctrl.get('confirmPassword')?.value;
  if (!pw || !cpw) return null;
  return pw !== cpw ? { mismatch: { message: 'Passwords do not match' } } : null;
}
