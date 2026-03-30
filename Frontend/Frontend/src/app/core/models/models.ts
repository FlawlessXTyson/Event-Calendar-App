// ─── ENUMS ───────────────────────────────────────────────────────────────────
export enum UserRole {
  USER = 1,
  ORGANIZER = 2,
  ADMIN = 3
}

export enum AccountStatus {
  ACTIVE = 1,
  BLOCKED = 2
}

export enum EventCategory {
  HOLIDAY = 1,
  AWARENESS = 2,
  PUBLIC = 3,
  PERSONAL = 4
}

export enum EventVisibility {
  PUBLIC = 1,
  PRIVATE = 2
}

export enum EventStatus {
  CANCELLED = 0,
  ACTIVE = 1,
  COMPLETED = 2
}

export enum ApprovalStatus {
  PENDING = 1,
  APPROVED = 2,
  REJECTED = 3
}

export enum PaymentStatus {
  PENDING = 1,
  SUCCESS = 2,
  FAILED = 3,
  REFUNDED = 4
}

export enum RegistrationStatus {
  REGISTERED = 1,
  CANCELLED = 2
}

export enum TodoStatus {
  PENDING = 1,
  COMPLETED = 2
}

export enum RequestStatus {
  PENDING = 1,
  APPROVED = 2,
  REJECTED = 3
}

// ─── AUTH ────────────────────────────────────────────────────────────────────

/** POST /api/Authentication/register — JSON property name is "username" */
export interface RegisterRequest {
  username: string;   // maps to UserName in RegisterRequestDTO via [JsonPropertyName("username")]
  email: string;
  password: string;
}

/** POST /api/Authentication/login */
export interface LoginRequest {
  email: string;
  password: string;
}

/** Response from login and register */
export interface LoginResponse {
  token: string;
}

// ─── USER ─────────────────────────────────────────────────────────────────────

/** Response from GET /api/User/me and GET /api/User */
export interface UserDto {
  userId: number;
  name: string;
  email: string;
  role: UserRole;
  status: AccountStatus;
  createdAt: string;
  profileImageUrl?: string;
}

/** POST /api/User (ADMIN only) */
export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
  role: UserRole;
}

/** PUT /api/User/me — name required, email optional */
export interface UpdateUserRequest {
  name: string;
  email?: string;
  role?: UserRole;
  status?: AccountStatus;
}

// ─── EVENT ────────────────────────────────────────────────────────────────────

/**
 * Response shape from all event endpoints.
 * NOTE: EventService.MapToDTO returns eventId, title, description, eventDate,
 * location, category, visibility, approvalStatus, isPaidEvent, ticketPrice.
 * The full Event model has startTime, endTime, eventEndDate, seatsLimit,
 * registrationDeadline, status, createdByUserId, createdAt but these are
 * NOT in the EventResponseDTO — frontend uses what is available.
 */
export interface EventResponse {
  eventId: number;
  title: string;
  description: string;
  eventDate: string;        // DateTime → ISO string
  location: string;
  category: EventCategory;
  visibility: EventVisibility;
  approvalStatus: ApprovalStatus;
  isPaidEvent: boolean;
  ticketPrice: number;
  // Full model fields that MAY come through (for detail pages)
  eventEndDate?: string;
  startTime?: string;       // TimeSpan → "HH:mm:ss"
  endTime?: string;
  seatsLimit?: number;
  seatsLeft?: number;           // remaining seats — calculated by backend (SeatsLimit - booked)
  organizerName?: string;       // name of the organizer who created the event
  refundCutoffDays?: number;
  earlyRefundPercentage?: number;
  registrationDeadline?: string;
  isRegistrationOpen?: boolean;    // computed server-side — use this instead of local time math
  hasStarted?: boolean;            // true once event start time passed (UTC)
  hasEnded?: boolean;              // true once event end time passed (UTC)
  status?: EventStatus;
  createdByUserId?: number;
  createdAt?: string;
  commissionPercentage?: number;
}

/**
 * POST /api/Event (ORGANIZER/ADMIN)
 * BACKEND VALIDATIONS:
 *  - eventDate cannot be in the past
 *  - if same day: startTime must be in future
 *  - endTime must be after startTime
 *  - eventEndDate (if set) cannot be before eventDate
 *  - registrationDeadline: must be in future, before event start, not after event end
 *  - if isPaidEvent=true: ticketPrice must be > 0
 *  - visibility and createdByUserId are set server-side (ignored from client)
 *  - category is auto-set to PUBLIC server-side
 */
export interface CreateEventRequest {
  title: string;
  description: string;
  eventDate: string;          // "YYYY-MM-DD"
  eventEndDate?: string;      // optional multi-day end date
  startTime: string;          // "HH:mm:ss" — REQUIRED by backend
  endTime: string;            // "HH:mm:ss" — REQUIRED by backend
  location: string;
  seatsLimit?: number;
  registrationDeadline?: string;  // ISO datetime string
  isPaidEvent: boolean;
  ticketPrice: number;
}

/** Paged result wrapper */
export interface PagedResult<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  data: T[];
}

/** GET /api/Event/{id}/refund-summary */
export interface RefundSummary {
  eventId: number;
  totalUsersRefunded: number;
  totalRefundAmount: number;
}

// ─── EVENT REGISTRATION ───────────────────────────────────────────────────────

/** POST /api/EventRegistration — userId is from JWT, not body */
export interface RegisterEventRequest {
  eventId: number;
}

/** Registration response + cancel response wraps in { message, data } */
export interface RegistrationWrapper {
  message: string;
  data: EventRegistrationResponse;
}

export interface EventRegistrationResponse {
  registrationId: number;
  eventId: number;
  userId: number;
  userName?: string;
  userEmail?: string;
  status: RegistrationStatus;
  registeredAt: string;
}

// ─── PAYMENT ──────────────────────────────────────────────────────────────────

/** POST /api/Payment — must register before payment */
export interface PaymentRequest {
  eventId: number;
}

export interface PaymentResponse {
  paymentId: number;
  eventId: number;
  eventTitle?: string;
  userId?: number;
  userName?: string;
  userEmail?: string;
  amountPaid: number;
  refundedAmount?: number;
  status: PaymentStatus;
  paymentDate: string;
  refundedAt?: string;
}

/** GET /api/Payment/commission-summary (ADMIN) */
export interface CommissionSummary {
  totalCommission: number;
  totalOrganizerPayout: number;
  totalPayments: number;
}

/** GET /api/Payment/organizer-earnings (ORGANIZER) */
export interface OrganizerEarnings {
  totalRevenue: number;
  totalCommission: number;
  netEarnings: number;
  totalTransactions: number;
}

/** GET /api/Payment/organizer-event-earnings (ORGANIZER) */
export interface EventWiseEarnings {
  eventId: number;
  eventTitle: string;
  totalRevenue: number;
  totalCommission: number;
  netEarnings: number;
  totalTransactions: number;
}

// ─── REMINDER ─────────────────────────────────────────────────────────────────

/**
 * POST /api/Reminder
 * BACKEND LOGIC:
 *  - reminderTitle required
 *  - CANNOT provide both reminderDateTime AND minutesBefore
 *  - Case 1 (manual): provide reminderDateTime — must be in future
 *  - Case 2 (event-based): provide eventId + minutesBefore (>0) — event must have startTime
 *    calculated = eventDate + startTime - minutesBefore; must be in future
 *  - Duplicate check: same userId + eventId + title + time (within 1 second)
 */
export interface CreateReminderRequest {
  eventId?: number;
  reminderTitle: string;
  reminderDateTime?: string;  // ISO — for manual reminders
  minutesBefore?: number;     // for event-based reminders
}

export interface ReminderResponse {
  reminderId: number;
  userId: number;
  eventId?: number;
  reminderTitle: string;
  reminderDateTime: string;
  createdAt: string;
}

// ─── TODO ─────────────────────────────────────────────────────────────────────

/** POST /api/Todo — userId is from JWT */
export interface CreateTodoRequest {
  taskTitle: string;
  dueDate?: string;    // YYYY-MM-DD — cannot be in the past
}

/** PUT /api/Todo/{id} */
export interface UpdateTodoRequest {
  taskTitle: string;
  dueDate?: string;
}

export interface TodoResponse {
  todoId: number;
  userId: number;
  taskTitle: string;
  dueDate?: string;
  status: TodoStatus;
  createdAt: string;
}

// ─── ROLE REQUEST ─────────────────────────────────────────────────────────────

export interface RoleChangeRequest {
  requestId: number;
  userId: number;
  requestedRole: UserRole;
  status: RequestStatus;
  requestedAt: string;
  reviewedByAdminId?: number;
  user?: { name?: string; email?: string };
}

// ─── REFUND REQUEST ───────────────────────────────────────────────────────────

export enum RefundRequestStatus { PENDING = 1, APPROVED = 2, REJECTED = 3 }

export interface RefundRequestResponse {
  refundRequestId: number;
  userId: number;
  userName: string;
  userEmail: string;
  eventId: number;
  eventTitle: string;
  paymentId: number;
  amountPaid: number;
  requestedAt: string;
  status: RefundRequestStatus;
  approvedPercentage?: number;
  reviewedAt?: string;
}

// ─── TICKET ───────────────────────────────────────────────────────────────────

export interface TicketResponse {
  ticketId: number;
  userId: number;
  userName: string;
  eventId: number;
  eventTitle: string;
  eventDescription: string;
  eventLocation: string;
  eventDate: string;
  startTime?: string;
  endTime?: string;
  paymentId?: number;
  amountPaid: number;
  isPaidEvent: boolean;
  generatedAt: string;
}

// ─── AUDIT LOG ────────────────────────────────────────────────────────────────

export interface AuditLog {
  id: number;
  userId: number;
  userName?: string;
  role: string;
  action: string;
  entity: string;
  entityId: number;
  createdAt: string;
}

// ─── API ERROR ────────────────────────────────────────────────────────────────

/** Shape returned by ExceptionMiddleware */
export interface ApiError {
  success: boolean;
  statusCode: number;
  message: string;
  timestamp: string;
}

// ─── JWT PAYLOAD ──────────────────────────────────────────────────────────────

export interface JwtPayload {
  // ClaimTypes.NameIdentifier → userId
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
  // ClaimTypes.Name → name
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'?: string;
  // ClaimTypes.Email → email
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'?: string;
  // ClaimTypes.Role → role
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  nameid?: string;
  unique_name?: string;
  email?: string;
  role?: string;
  exp: number;
  iat: number;
}
