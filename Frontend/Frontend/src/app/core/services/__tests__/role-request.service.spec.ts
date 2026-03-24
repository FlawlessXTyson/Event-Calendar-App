import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RoleRequestService } from '../role-request.service';
import { environment } from '../../../../environments/environment';
import { UserRole, RequestStatus } from '../../models/models';

describe('RoleRequestService', () => {
  let service: RoleRequestService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/RoleRequest`;

  const mockRequest = {
    requestId: 1, userId: 42, requestedRole: UserRole.ORGANIZER,
    status: RequestStatus.PENDING, requestedAt: '2026-03-21T10:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(RoleRequestService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requestOrganizer() sends POST to /api/RoleRequest/request-organizer', () => {
    service.requestOrganizer().subscribe();
    const req = http.expectOne(`${base}/request-organizer`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush('Request submitted');
  });

  it('getPending() sends GET to /api/RoleRequest/pending', () => {
    service.getPending().subscribe();
    const req = http.expectOne(`${base}/pending`);
    expect(req.request.method).toBe('GET');
    req.flush([mockRequest]);
  });

  it('getPending() returns array of role change requests', () => {
    let result: any[];
    service.getPending().subscribe(rs => result = rs);
    http.expectOne(`${base}/pending`).flush([mockRequest]);
    expect(result!.length).toBe(1);
    expect(result![0].status).toBe(RequestStatus.PENDING);
  });

  it('approve() sends PUT to /api/RoleRequest/{id}/approve', () => {
    service.approve(1).subscribe();
    const req = http.expectOne(`${base}/1/approve`);
    expect(req.request.method).toBe('PUT');
    req.flush('Approved');
  });

  it('reject() sends PUT to /api/RoleRequest/{id}/reject', () => {
    service.reject(1).subscribe();
    const req = http.expectOne(`${base}/1/reject`);
    expect(req.request.method).toBe('PUT');
    req.flush('Rejected');
  });

  it('approve() uses correct id in URL', () => {
    service.approve(77).subscribe();
    http.expectOne(`${base}/77/approve`).flush('OK');
  });

  it('reject() uses correct id in URL', () => {
    service.reject(88).subscribe();
    http.expectOne(`${base}/88/reject`).flush('OK');
  });
});
