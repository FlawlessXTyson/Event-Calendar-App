import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }), // tells angular when to render the ui
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()), // smooth trnasition whn routes
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])), //
    provideAnimationsAsync(),
  ]
};
