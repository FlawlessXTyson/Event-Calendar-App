import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';
import { ApiError } from '../models/models';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast  = inject(ToastService);
  const router = inject(Router);
  const auth   = inject(AuthService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = err.error as ApiError | null;
      const message = body?.message ?? err.message ?? 'Something went wrong.';

      switch (err.status) {
        case 400:
          toast.error(message, 'Validation Error');
          break;
        case 401:
          // Login endpoint — wrong credentials, not a session expiry
          if (req.url.includes('/Authentication/login') || req.url.includes('/Authentication/register')) {
            break; // let the component handle it
          }
          toast.error('Your session has expired. Please sign in again.', 'Session Expired');
          auth.logout();
          break;
        case 403:
          toast.error('You don\'t have permission to perform this action.', 'Access Denied');
          break;
        case 404:
          // 404s that come from "not found" queries (empty lists) are swallowed silently
          // Only show toast if it's a direct resource miss
          if (!req.url.includes('/my') && !req.url.includes('/pending') && !req.url.includes('/registered')) {
            toast.warning(message, 'Not Found');
          }
          break;
        case 500:
          toast.error('A server error occurred. Please try again later.', 'Server Error');
          break;
        default:
          toast.error(message, 'Error');
      }

      return throwError(() => err);
    })
  );
};
