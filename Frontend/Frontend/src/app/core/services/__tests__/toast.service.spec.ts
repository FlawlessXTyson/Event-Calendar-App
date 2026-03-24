import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ToastService, Toast } from '../toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  // ── Initial state ─────────────────────────────────────────────────────────
  it('starts with empty toast list', () => {
    expect(service.toasts()).toEqual([]);
  });

  // ── success() ─────────────────────────────────────────────────────────────
  it('success() adds a toast with type=success and default title', () => {
    service.success('Operation done');
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].type).toBe('success');
    expect(toasts[0].message).toBe('Operation done');
    expect(toasts[0].title).toBe('Success');
  });

  it('success() accepts a custom title', () => {
    service.success('Saved!', 'Profile Updated');
    expect(service.toasts()[0].title).toBe('Profile Updated');
  });

  it('success() sets duration to 5000ms', () => {
    service.success('done');
    expect(service.toasts()[0].duration).toBe(5000);
  });

  // ── error() ───────────────────────────────────────────────────────────────
  it('error() adds a toast with type=error and 7000ms duration', () => {
    service.error('Something went wrong');
    const t = service.toasts()[0];
    expect(t.type).toBe('error');
    expect(t.duration).toBe(7000);
    expect(t.title).toBe('Error');
  });

  // ── warning() ─────────────────────────────────────────────────────────────
  it('warning() adds type=warning with 6000ms duration', () => {
    service.warning('Check this');
    const t = service.toasts()[0];
    expect(t.type).toBe('warning');
    expect(t.duration).toBe(6000);
  });

  // ── info() ────────────────────────────────────────────────────────────────
  it('info() adds type=info with 5000ms duration', () => {
    service.info('FYI something');
    const t = service.toasts()[0];
    expect(t.type).toBe('info');
    expect(t.duration).toBe(5000);
  });

  // ── reminder() ────────────────────────────────────────────────────────────
  it('reminder() adds type=reminder with 10000ms duration', () => {
    service.reminder('⏰ Prepare for conference', 'Reminder');
    const t = service.toasts()[0];
    expect(t.type).toBe('reminder');
    expect(t.duration).toBe(10000);
    expect(t.message).toContain('Prepare for conference');
  });

  // ── Multiple toasts ───────────────────────────────────────────────────────
  it('can hold multiple toasts simultaneously', () => {
    service.success('First');
    service.error('Second');
    service.info('Third');
    expect(service.toasts().length).toBe(3);
  });

  it('each toast gets a unique incrementing id', () => {
    service.success('A');
    service.success('B');
    const ids = service.toasts().map(t => t.id);
    expect(ids[0]).not.toBe(ids[1]);
    expect(ids[1]).toBeGreaterThan(ids[0]);
  });

  // ── remove() ──────────────────────────────────────────────────────────────
  it('remove() removes the correct toast by id', () => {
    service.success('Keep me');
    service.error('Remove me');
    const removeId = service.toasts()[1].id;
    service.remove(removeId);
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].message).toBe('Keep me');
  });

  it('remove() with unknown id does not throw', () => {
    service.success('Hello');
    expect(() => service.remove(999)).not.toThrow();
    expect(service.toasts().length).toBe(1);
  });

  // ── Auto-dismiss ──────────────────────────────────────────────────────────
  it('auto-dismisses after duration elapses', fakeAsync(() => {
    service.success('Temporary');
    expect(service.toasts().length).toBe(1);
    tick(5000);
    expect(service.toasts().length).toBe(0);
  }));

  it('reminder stays for 10 seconds before auto-dismiss', fakeAsync(() => {
    service.reminder('Test reminder');
    tick(9999);
    expect(service.toasts().length).toBe(1);
    tick(1);
    expect(service.toasts().length).toBe(0);
  }));

  it('error toast stays for 7 seconds', fakeAsync(() => {
    service.error('An error');
    tick(6999);
    expect(service.toasts().length).toBe(1);
    tick(1);
    expect(service.toasts().length).toBe(0);
  }));
});
