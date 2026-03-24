# EventCalenderApp — Angular 19 Frontend

## Quick Start

```bash
cd eca2
npm install --legacy-peer-deps
ng serve
```

Open: http://localhost:4200

## API URL
Set in `src/environments/environment.ts` → `apiUrl: 'http://localhost:5263/api'`

## Architecture

### Role-based Routing
| Role | Dashboard | Guard |
|------|-----------|-------|
| USER | /user/dashboard | userGuard |
| ORGANIZER | /organizer/dashboard | organizerGuard |
| ADMIN | /admin/dashboard | adminGuard |

After login, users are automatically redirected to their role-specific dashboard.

### Key Backend Rules (enforced in frontend too)
- **EventDate** cannot be in the past
- **StartTime** must be in future for same-day events
- **EndTime** must be after StartTime
- **EventEndDate** (optional) cannot be before EventDate
- **RegistrationDeadline** must be future, before event start, not after event end
- **Paid events** require ticketPrice > 0
- **Register before Pay** — backend enforces this order
- **Reminder** — provide EITHER reminderDateTime OR (eventId + minutesBefore), never both
- **Email format** — strictly validated: user@domain.com format required

### API Endpoints Covered
- POST /api/Authentication/register + login
- GET/POST/PUT/DELETE /api/Event (+ approve, reject, cancel, refund-summary, search, range, paged, my, registered, pending, rejected, approved)
- POST/PUT/GET /api/EventRegistration (register, cancel, my, event/{id})
- POST/GET/PUT /api/Payment (create, my-payments, event/{id}, all, refund, commission-summary, organizer-earnings, organizer-event-earnings)
- POST/GET/DELETE /api/Reminder (create, me, delete)
- POST/GET/PUT/DELETE /api/Todo (create, me, complete, update, delete)
- GET/POST/PUT /api/User (me, all, create, updateMe, delete)
- POST/GET/PUT /api/RoleRequest (request-organizer, pending, approve, reject)

### Reminder Notification System
Polls every 30 seconds. Fires toast + browser notification when a reminder is due (within 1 minute). Starts automatically after login.
