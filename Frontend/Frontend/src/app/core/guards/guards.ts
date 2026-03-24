import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { RegistrationStateService } from '../services/registration-state.service';

export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  router.navigate(['/auth/login']);
  return false;
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (!auth.isLoggedIn()) return true;
  inject(AuthService).redirectByRole();
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isAdmin()) return true;
  router.navigate([auth.isLoggedIn() ? '/not-found' : '/auth/login']);
  return false;
};

export const organizerGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isOrganizer()) return true;
  router.navigate([auth.isLoggedIn() ? '/not-found' : '/auth/login']);
  return false;
};

export const userGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isUser()) return true;
  router.navigate([auth.isLoggedIn() ? '/not-found' : '/auth/login']);
  return false;
};

export const loggedInGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  router.navigate(['/auth/login']);
  return false;
};

/**
 * Blocks navigation away from any route when the user has a pending
 * registration that requires payment or cancellation.
 */
export const registrationPendingGuard: CanDeactivateFn<unknown> = () => {
  const regState = inject(RegistrationStateService);
  const toast    = inject(ToastService);
  if (regState.isNavigationBlocked()) {
    toast.warning(
      'Please complete payment or cancel registration before leaving.',
      'Navigation Blocked'
    );
    return false;
  }
  return true;
};
