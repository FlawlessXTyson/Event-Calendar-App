import { FormControl, FormGroup } from '@angular/forms';
import {
  strictEmail,
  futureDateOnly,
  endDateAfterStart,
  endTimeAfterStart,
  paidEventPrice,
  minPassword,
  passwordsMatch
} from '../custom.validators';

// ─── strictEmail ──────────────────────────────────────────────────────────────
describe('strictEmail validator', () => {
  const validator = strictEmail();

  it('returns null for empty string (optional field)', () => {
    const ctrl = new FormControl('');
    expect(validator(ctrl)).toBeNull();
  });

  it('returns null for valid email: user@gmail.com', () => {
    const ctrl = new FormControl('user@gmail.com');
    expect(validator(ctrl)).toBeNull();
  });

  it('returns null for valid email with dots: first.last@domain.co.in', () => {
    const ctrl = new FormControl('first.last@domain.co.in');
    expect(validator(ctrl)).toBeNull();
  });

  it('returns null for valid email with plus: user+tag@example.org', () => {
    const ctrl = new FormControl('user+tag@example.org');
    expect(validator(ctrl)).toBeNull();
  });

  it('returns error for missing @ symbol', () => {
    const ctrl = new FormControl('userexample.com');
    const result = validator(ctrl);
    expect(result).not.toBeNull();
    expect(result!['invalidEmail']).toBeDefined();
  });

  it('returns error for missing domain extension', () => {
    const ctrl = new FormControl('user@domain');
    expect(validator(ctrl)).not.toBeNull();
  });

  it('returns error for double @', () => {
    const ctrl = new FormControl('user@@domain.com');
    expect(validator(ctrl)).not.toBeNull();
  });

  it('returns error message matching user@gmail.com example', () => {
    const ctrl = new FormControl('invalid-email');
    const result = validator(ctrl);
    expect(result!['invalidEmail'].message).toContain('user@gmail.com');
  });

  it('returns error for plain space in email', () => {
    const ctrl = new FormControl('user @gmail.com');
    expect(validator(ctrl)).not.toBeNull();
  });
});

// ─── futureDateOnly ───────────────────────────────────────────────────────────
describe('futureDateOnly validator', () => {
  const validator = futureDateOnly();

  it('returns null for empty value', () => {
    const ctrl = new FormControl('');
    expect(validator(ctrl)).toBeNull();
  });

  it('returns null for today', () => {
    const today = new Date().toISOString().split('T')[0];
    const ctrl = new FormControl(today);
    expect(validator(ctrl)).toBeNull();
  });

  it('returns null for tomorrow', () => {
    const tomorrow = new Date(Date.now() + 86400000).toISOString().split('T')[0];
    const ctrl = new FormControl(tomorrow);
    expect(validator(ctrl)).toBeNull();
  });

  it('returns pastDate error for yesterday', () => {
    const yesterday = new Date(Date.now() - 86400000).toISOString().split('T')[0];
    const ctrl = new FormControl(yesterday);
    const result = validator(ctrl);
    expect(result).not.toBeNull();
    expect(result!['pastDate']).toBeDefined();
  });

  it('returns pastDate error for a date in 2020', () => {
    const ctrl = new FormControl('2020-01-01');
    expect(validator(ctrl)).not.toBeNull();
  });
});

// ─── endDateAfterStart ────────────────────────────────────────────────────────
describe('endDateAfterStart validator', () => {
  function makeGroup(startDate: string, endDate: string) {
    const group = new FormGroup({
      startDate: new FormControl(startDate),
      endDate:   new FormControl(endDate, [endDateAfterStart('startDate')])
    });
    return group;
  }

  it('returns null when endDate equals startDate', () => {
    const group = makeGroup('2026-05-10', '2026-05-10');
    expect(group.get('endDate')!.errors).toBeNull();
  });

  it('returns null when endDate is after startDate', () => {
    const group = makeGroup('2026-05-10', '2026-05-15');
    expect(group.get('endDate')!.errors).toBeNull();
  });

  it('returns endBeforeStart error when endDate is before startDate', () => {
    const group = makeGroup('2026-05-10', '2026-05-05');
    expect(group.get('endDate')!.errors).not.toBeNull();
    expect(group.get('endDate')!.errors!['endBeforeStart']).toBeDefined();
  });

  it('returns null when startDate is empty', () => {
    const group = makeGroup('', '2026-05-10');
    expect(group.get('endDate')!.errors).toBeNull();
  });

  it('returns null when endDate is empty', () => {
    const group = makeGroup('2026-05-10', '');
    expect(group.get('endDate')!.errors).toBeNull();
  });
});

// ─── endTimeAfterStart ────────────────────────────────────────────────────────
describe('endTimeAfterStart validator', () => {
  function makeGroup(startTime: string, endTime: string) {
    return new FormGroup({
      startTime: new FormControl(startTime),
      endTime:   new FormControl(endTime, [endTimeAfterStart('startTime')])
    });
  }

  it('returns null when endTime is after startTime', () => {
    const g = makeGroup('09:00', '10:00');
    expect(g.get('endTime')!.errors).toBeNull();
  });

  it('returns error when endTime equals startTime', () => {
    const g = makeGroup('09:00', '09:00');
    expect(g.get('endTime')!.errors!['endTimeBeforeStart']).toBeDefined();
  });

  it('returns error when endTime is before startTime', () => {
    const g = makeGroup('14:00', '10:00');
    expect(g.get('endTime')!.errors!['endTimeBeforeStart']).toBeDefined();
  });

  it('returns null when startTime is empty', () => {
    const g = makeGroup('', '10:00');
    expect(g.get('endTime')!.errors).toBeNull();
  });
});

// ─── paidEventPrice ───────────────────────────────────────────────────────────
describe('paidEventPrice validator', () => {
  function makeGroup(isPaid: boolean, price: number | string) {
    return new FormGroup({
      isPaid:      new FormControl(isPaid),
      ticketPrice: new FormControl(price, [paidEventPrice('isPaid')])
    });
  }

  it('returns null when event is free (isPaid=false), price=0', () => {
    const g = makeGroup(false, 0);
    expect(g.get('ticketPrice')!.errors).toBeNull();
  });

  it('returns null when event is paid and price > 0', () => {
    const g = makeGroup(true, 100);
    expect(g.get('ticketPrice')!.errors).toBeNull();
  });

  it('returns invalidPrice error when isPaid=true and price=0', () => {
    const g = makeGroup(true, 0);
    expect(g.get('ticketPrice')!.errors!['invalidPrice']).toBeDefined();
  });

  it('returns invalidPrice error when isPaid=true and price is negative', () => {
    const g = makeGroup(true, -50);
    expect(g.get('ticketPrice')!.errors!['invalidPrice']).toBeDefined();
  });

  it('returns null when isPaid=false regardless of price', () => {
    const g = makeGroup(false, -100);
    expect(g.get('ticketPrice')!.errors).toBeNull();
  });
});

// ─── minPassword ──────────────────────────────────────────────────────────────
describe('minPassword validator', () => {
  const validator = minPassword();

  it('returns null for empty string', () => {
    expect(validator(new FormControl(''))).toBeNull();
  });

  it('returns null for password of exactly 6 characters', () => {
    expect(validator(new FormControl('abcdef'))).toBeNull();
  });

  it('returns null for password longer than 6 characters', () => {
    expect(validator(new FormControl('securepassword123'))).toBeNull();
  });

  it('returns weakPassword error for 5-character password', () => {
    const result = validator(new FormControl('abc12'));
    expect(result!['weakPassword']).toBeDefined();
    expect(result!['weakPassword'].message).toContain('6 characters');
  });

  it('returns weakPassword error for single character', () => {
    expect(validator(new FormControl('a'))).not.toBeNull();
  });
});

// ─── passwordsMatch ───────────────────────────────────────────────────────────
describe('passwordsMatch validator', () => {
  function makeGroup(pw: string, cpw: string) {
    return new FormGroup({
      password:        new FormControl(pw),
      confirmPassword: new FormControl(cpw)
    }, { validators: passwordsMatch });
  }

  it('returns null when passwords match', () => {
    const g = makeGroup('mypassword', 'mypassword');
    expect(passwordsMatch(g)).toBeNull();
  });

  it('returns mismatch error when passwords differ', () => {
    const g = makeGroup('password1', 'password2');
    expect(passwordsMatch(g)!['mismatch']).toBeDefined();
  });

  it('returns null when either field is empty', () => {
    const g = makeGroup('', 'something');
    expect(passwordsMatch(g)).toBeNull();
  });

  it('is case-sensitive: Password vs password are different', () => {
    const g = makeGroup('Password', 'password');
    expect(passwordsMatch(g)!['mismatch']).toBeDefined();
  });
});
