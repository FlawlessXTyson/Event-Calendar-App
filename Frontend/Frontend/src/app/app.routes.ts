import { Routes } from '@angular/router';
import { authGuard, guestGuard, adminGuard, organizerGuard, userGuard, loggedInGuard, registrationPendingGuard } from './core/guards/guards';

export const routes: Routes = [
  // ── Guest / Public ─────────────────────────────────────────────────────
  {
    path: '',
    loadComponent: () => import('./features/public/layout/public-layout.component').then(m => m.PublicLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent) },
      { path: 'events', loadComponent: () => import('./features/public/events/events.component').then(m => m.EventsComponent) },
      { path: 'events/:id', canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/public/event-detail/event-detail.component').then(m => m.EventDetailComponent) },
      { path: 'calendar', loadComponent: () => import('./features/public/calendar/calendar.component').then(m => m.CalendarComponent) },
    ]
  },

  // ── Auth ────────────────────────────────────────────────────────────────
  {
    path: 'auth',
    children: [
      { path: 'login',    canActivate: [guestGuard], loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  // ── User Dashboard ──────────────────────────────────────────────────────
  {
    path: 'user',
    canActivate: [userGuard],
    loadComponent: () => import('./features/user/layout/user-layout.component').then(m => m.UserLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',    canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/dashboard/user-dashboard.component').then(m => m.UserDashboardComponent) },
      { path: 'my-events',    canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/my-events/user-my-events.component').then(m => m.UserMyEventsComponent) },
      { path: 'payments',     canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/payments/user-payments.component').then(m => m.UserPaymentsComponent) },
      { path: 'reminders',    canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/reminders/user-reminders.component').then(m => m.UserRemindersComponent) },
      { path: 'todos',        canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/todos/user-todos.component').then(m => m.UserTodosComponent) },
      { path: 'profile',      canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/profile/user-profile.component').then(m => m.UserProfileComponent) },
      { path: 'request-role', canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/request-role/request-role.component').then(m => m.RequestRoleComponent) },
      { path: 'calendar',     canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/calendar/user-calendar.component').then(m => m.UserCalendarComponent) },
      { path: 'events-attended', canDeactivate: [registrationPendingGuard], loadComponent: () => import('./features/user/events-attended/events-attended.component').then(m => m.EventsAttendedComponent) },
    ]
  },

  // ── Organizer Dashboard ─────────────────────────────────────────────────
  {
    path: 'organizer',
    canActivate: [organizerGuard],
    loadComponent: () => import('./features/organizer/layout/organizer-layout.component').then(m => m.OrganizerLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',     loadComponent: () => import('./features/organizer/dashboard/organizer-dashboard.component').then(m => m.OrganizerDashboardComponent) },
      { path: 'create-event',  loadComponent: () => import('./features/organizer/create-event/create-event.component').then(m => m.CreateEventComponent) },
      { path: 'my-events',     loadComponent: () => import('./features/organizer/my-events/organizer-my-events.component').then(m => m.OrganizerMyEventsComponent) },
      { path: 'earnings',      loadComponent: () => import('./features/organizer/earnings/organizer-earnings.component').then(m => m.OrganizerEarningsComponent) },
      { path: 'registrations', loadComponent: () => import('./features/organizer/event-registrations/event-registrations.component').then(m => m.EventRegistrationsComponent) },
      { path: 'profile',       loadComponent: () => import('./features/organizer/profile/organizer-profile.component').then(m => m.OrganizerProfileComponent) },
      { path: 'calendar',      loadComponent: () => import('./features/organizer/calendar/organizer-calendar.component').then(m => m.OrganizerCalendarComponent) },
    ]
  },

  // ── Admin Dashboard ─────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard',     loadComponent: () => import('./features/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'events',        loadComponent: () => import('./features/admin/events/admin-events.component').then(m => m.AdminEventsComponent) },
      { path: 'users',         loadComponent: () => import('./features/admin/users/admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'payments',      loadComponent: () => import('./features/admin/payments/admin-payments.component').then(m => m.AdminPaymentsComponent) },
      { path: 'role-requests', loadComponent: () => import('./features/admin/role-requests/admin-role-requests.component').then(m => m.AdminRoleRequestsComponent) },
      { path: 'profile',       loadComponent: () => import('./features/admin/profile/admin-profile.component').then(m => m.AdminProfileComponent) },
      { path: 'calendar',      loadComponent: () => import('./features/admin/calendar/admin-calendar.component').then(m => m.AdminCalendarComponent) },
      { path: 'audit-logs',       loadComponent: () => import('./features/admin/audit-logs/admin-audit-logs.component').then(m => m.AdminAuditLogsComponent) },
      { path: 'refund-requests',  loadComponent: () => import('./features/admin/refund-requests/admin-refund-requests.component').then(m => m.AdminRefundRequestsComponent) },
    ]
  },

  // ── Not Found ────────────────────────────────────────────────────────────
  { path: 'not-found', loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) },
  { path: '**',        loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) }
];
