import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PaymentService } from '../payment.service';
import { environment } from '../../../../environments/environment';
import { PaymentStatus } from '../../models/models';

describe('PaymentService', () => {
  let service: PaymentService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/Payment`;

  const mockPayment = {
    paymentId: 1, eventId: 5, amountPaid: 400,
    status: PaymentStatus.SUCCESS, paymentDate: '2026-03-21T10:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PaymentService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create() sends POST to /api/Payment with eventId', () => {
    service.create({ eventId: 5 }).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.eventId).toBe(5);
    req.flush(mockPayment);
  });

  it('getMyPayments() sends GET to /api/Payment/my-payments', () => {
    service.getMyPayments().subscribe();
    const req = http.expectOne(`${base}/my-payments`);
    expect(req.request.method).toBe('GET');
    req.flush([mockPayment]);
  });

  it('getByEvent() sends GET to /api/Payment/event/{eventId}', () => {
    service.getByEvent(5).subscribe();
    const req = http.expectOne(`${base}/event/5`);
    expect(req.request.method).toBe('GET');
    req.flush([mockPayment]);
  });

  it('getAll() sends GET to /api/Payment/all (admin)', () => {
    service.getAll().subscribe();
    http.expectOne(`${base}/all`).flush([mockPayment]);
  });

  it('refund() sends PUT to /api/Payment/{id}/refund', () => {
    service.refund(1).subscribe();
    const req = http.expectOne(`${base}/1/refund`);
    expect(req.request.method).toBe('PUT');
    req.flush({ ...mockPayment, status: PaymentStatus.REFUNDED });
  });

  it('getCommissionSummary() sends GET to /api/Payment/commission-summary', () => {
    service.getCommissionSummary().subscribe();
    http.expectOne(`${base}/commission-summary`).flush({
      totalCommission: 120, totalOrganizerPayout: 1080, totalPayments: 3
    });
  });

  it('getOrganizerEarnings() sends GET to /api/Payment/organizer-earnings', () => {
    service.getOrganizerEarnings().subscribe();
    http.expectOne(`${base}/organizer-earnings`).flush({
      totalRevenue: 1200, totalCommission: 120, netEarnings: 1080, totalTransactions: 3
    });
  });

  it('getEventWiseEarnings() sends GET to /api/Payment/organizer-event-earnings', () => {
    service.getEventWiseEarnings().subscribe();
    http.expectOne(`${base}/organizer-event-earnings`).flush([]);
  });
});
