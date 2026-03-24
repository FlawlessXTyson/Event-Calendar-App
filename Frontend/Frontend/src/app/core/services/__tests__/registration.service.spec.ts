import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RegistrationService } from '../registration.service';
import { environment } from '../../../../environments/environment';
import { RegistrationStatus } from '../../models/models';

describe('RegistrationService', () => {
  let service: RegistrationService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/EventRegistration`;

  const mockReg = {
    registrationId: 1, eventId: 5, userId: 42,
    status: RegistrationStatus.REGISTERED, registeredAt: '2026-03-21T10:00:00Z'
  };
  const mockWrapper = { message: 'Registered successfully', data: mockReg };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(RegistrationService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('register() sends POST to /api/EventRegistration with eventId', () => {
    service.register({ eventId: 5 }).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.eventId).toBe(5);
    req.flush(mockWrapper);
  });

  it('register() returns RegistrationWrapper', () => {
    let result: any;
    service.register({ eventId: 5 }).subscribe(r => result = r);
    http.expectOne(base).flush(mockWrapper);
    expect(result.message).toBe('Registered successfully');
    expect(result.data.registrationId).toBe(1);
  });

  it('cancel() sends PUT to /api/EventRegistration/{id}/cancel', () => {
    service.cancel(1).subscribe();
    const req = http.expectOne(`${base}/1/cancel`);
    expect(req.request.method).toBe('PUT');
    req.flush({ ...mockWrapper, message: 'Cancelled' });
  });

  it('getMyRegistrations() sends GET to /api/EventRegistration/my', () => {
    service.getMyRegistrations().subscribe();
    const req = http.expectOne(`${base}/my`);
    expect(req.request.method).toBe('GET');
    req.flush([mockReg]);
  });

  it('getByEvent() sends GET to /api/EventRegistration/event/{eventId}', () => {
    service.getByEvent(5).subscribe();
    const req = http.expectOne(`${base}/event/5`);
    expect(req.request.method).toBe('GET');
    req.flush([mockReg]);
  });

  it('getByEvent() uses correct eventId in URL', () => {
    service.getByEvent(99).subscribe();
    const req = http.expectOne(`${base}/event/99`);
    expect(req.request.url).toContain('/event/99');
    req.flush([]);
  });
});
