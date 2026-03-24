import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EventService } from '../event.service';
import { environment } from '../../../../environments/environment';
import { EventCategory, ApprovalStatus, EventVisibility } from '../../models/models';

describe('EventService', () => {
  let service: EventService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/Event`;

  const mockEvent = {
    eventId: 1, title: 'Backend Workshop', description: 'Learn .NET',
    eventDate: '2026-03-29', location: 'Chennai', category: EventCategory.PUBLIC,
    visibility: EventVisibility.PUBLIC, approvalStatus: ApprovalStatus.APPROVED,
    isPaidEvent: true, ticketPrice: 400
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(EventService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getAll() sends GET to /api/Event', () => {
    service.getAll().subscribe();
    http.expectOne(base).flush([mockEvent]);
  });

  it('getAll() returns array of events', () => {
    let result: any[];
    service.getAll().subscribe(es => result = es);
    http.expectOne(base).flush([mockEvent]);
    expect(result!.length).toBe(1);
    expect(result![0].title).toBe('Backend Workshop');
  });

  it('getById() sends GET to /api/Event/{id}', () => {
    service.getById(1).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockEvent);
  });

  it('search() sends GET with keyword param', () => {
    service.search('backend').subscribe();
    const req = http.expectOne(r => r.url.includes('/search'));
    expect(req.request.params.get('keyword')).toBe('backend');
    req.flush([mockEvent]);
  });

  it('getByRange() sends GET with start and end params', () => {
    service.getByRange('2026-03-01', '2026-03-31').subscribe();
    const req = http.expectOne(r => r.url.includes('/range'));
    expect(req.request.params.get('start')).toBe('2026-03-01');
    expect(req.request.params.get('end')).toBe('2026-03-31');
    req.flush([mockEvent]);
  });

  it('getPaged() sends GET with pageNumber and pageSize', () => {
    service.getPaged(1, 10).subscribe();
    const req = http.expectOne(r => r.url.includes('/paged'));
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ pageNumber: 1, pageSize: 10, totalRecords: 1, data: [mockEvent] });
  });

  it('create() sends POST to /api/Event', () => {
    const dto = { title: 'New Event', description: 'Desc', eventDate: '2026-04-01',
                  startTime: '09:00:00', endTime: '11:00:00', location: 'Chennai',
                  isPaidEvent: false, ticketPrice: 0 };
    service.create(dto).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    req.flush({ ...mockEvent, ...dto });
  });

  it('getMyEvents() sends GET to /api/Event/my', () => {
    service.getMyEvents().subscribe();
    http.expectOne(`${base}/my`).flush([mockEvent]);
  });

  it('cancel() sends PUT to /api/Event/{id}/cancel', () => {
    service.cancel(1).subscribe();
    const req = http.expectOne(`${base}/1/cancel`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockEvent);
  });

  it('approve() sends POST to /api/Event/{id}/approve', () => {
    service.approve(1).subscribe();
    const req = http.expectOne(`${base}/1/approve`);
    expect(req.request.method).toBe('POST');
    req.flush(mockEvent);
  });

  it('reject() sends POST to /api/Event/{id}/reject', () => {
    service.reject(1).subscribe();
    const req = http.expectOne(`${base}/1/reject`);
    expect(req.request.method).toBe('POST');
    req.flush(mockEvent);
  });

  it('delete() sends DELETE to /api/Event/{id}', () => {
    service.delete(1).subscribe();
    const req = http.expectOne(`${base}/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(mockEvent);
  });

  it('getPending() sends GET to /api/Event/pending', () => {
    service.getPending().subscribe();
    http.expectOne(`${base}/pending`).flush([]);
  });

  it('getApproved() sends GET to /api/Event/approved', () => {
    service.getApproved().subscribe();
    http.expectOne(`${base}/approved`).flush([mockEvent]);
  });

  it('getRejected() sends GET to /api/Event/rejected', () => {
    service.getRejected().subscribe();
    http.expectOne(`${base}/rejected`).flush([]);
  });

  it('getRegistered() sends GET to /api/Event/registered', () => {
    service.getRegistered().subscribe();
    http.expectOne(`${base}/registered`).flush([mockEvent]);
  });

  it('getRefundSummary() sends GET to /api/Event/{id}/refund-summary', () => {
    service.getRefundSummary(5).subscribe();
    const req = http.expectOne(`${base}/5/refund-summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ eventId: 5, totalUsersRefunded: 2, totalRefundAmount: 800 });
  });
});
