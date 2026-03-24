import { Injectable, signal, computed } from '@angular/core';

/**
 * Tracks whether the user has a pending registration that requires
 * payment or cancellation before navigating away.
 */
@Injectable({ providedIn: 'root' })
export class RegistrationStateService {
  /** eventId of the pending registration, null if none */
  private _pendingEventId = signal<number | null>(null);
  private _pendingIsPaid  = signal<boolean>(false);

  readonly pendingEventId = this._pendingEventId.asReadonly();
  readonly isPendingPayment = computed(() => this._pendingEventId() !== null && this._pendingIsPaid());
  readonly isNavigationBlocked = computed(() => this._pendingEventId() !== null && this._pendingIsPaid());

  setPending(eventId: number, isPaidEvent: boolean): void {
    this._pendingEventId.set(eventId);
    this._pendingIsPaid.set(isPaidEvent);
  }

  clearPending(): void {
    this._pendingEventId.set(null);
    this._pendingIsPaid.set(false);
  }
}
