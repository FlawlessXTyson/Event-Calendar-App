using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.RefundRequest;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class RefundRequestService : IRefundRequestService
    {
        private readonly IRepository<int, RefundRequest> _refundRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IAuditLogRepository _auditRepo;

        public RefundRequestService(
            IRepository<int, RefundRequest> refundRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo)
        {
            _refundRepo = refundRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
        }

        // User creates a refund request after cancelling
        public async Task<RefundRequestResponseDTO> CreateAsync(int userId, int paymentId)
        {
            var payment = await _paymentRepo.GetQueryable()
                .Include(p => p.Event)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
                ?? throw new NotFoundException("Payment not found");

            if (payment.UserId != userId)
                throw new UnauthorizedException("You can only request refund for your own payment");

            if (payment.Status == PaymentStatus.REFUNDED)
                throw new BadRequestException("Payment already refunded");

            // Prevent duplicate pending request
            var existing = await _refundRepo.GetQueryable()
                .AnyAsync(r => r.PaymentId == paymentId && r.Status == RefundRequestStatus.PENDING);
            if (existing)
                throw new BadRequestException("A refund request is already pending for this payment");

            var req = new RefundRequest
            {
                UserId = userId,
                EventId = payment.EventId,
                PaymentId = paymentId,
                RequestedAt = DateTime.UtcNow,
                Status = RefundRequestStatus.PENDING
            };

            var created = await _refundRepo.AddAsync(req);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "REFUND_REQUESTED",
                Entity = "RefundRequest",
                EntityId = created.RefundRequestId
            });

            return await BuildDTO(created.RefundRequestId);
        }

        public async Task<IEnumerable<RefundRequestResponseDTO>> GetPendingAsync()
        {
            var list = await _refundRepo.GetQueryable()
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.Payment)
                .Where(r => r.Status == RefundRequestStatus.PENDING)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return list.Select(MapToDTO);
        }

        public async Task<RefundRequestResponseDTO> ApproveAsync(int refundRequestId, int adminId, float percentage)
        {
            if (percentage < 0 || percentage > 100)
                throw new BadRequestException("Refund percentage must be between 0 and 100");

            var req = await _refundRepo.GetQueryable()
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId)
                ?? throw new NotFoundException("Refund request not found");

            if (req.Status != RefundRequestStatus.PENDING)
                throw new BadRequestException("Request already processed");

            var payment = req.Payment!;

            // Calculate refund split proportionally
            float refundAmount = payment.AmountPaid * percentage / 100f;
            float adminRefund = payment.AmountPaid > 0
                ? refundAmount * (payment.CommissionAmount / payment.AmountPaid)
                : 0;
            float organizerRefund = refundAmount - adminRefund;

            payment.Status = PaymentStatus.REFUNDED;
            payment.RefundedAmount = refundAmount;
            payment.RefundedAt = DateTime.UtcNow;
            payment.CommissionAmount = Math.Max(0, payment.CommissionAmount - adminRefund);
            payment.OrganizerAmount  = Math.Max(0, payment.OrganizerAmount  - organizerRefund);

            await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

            req.Status = RefundRequestStatus.APPROVED;
            req.ApprovedPercentage = percentage;
            req.ReviewedByAdminId = adminId;
            req.ReviewedAt = DateTime.UtcNow;
            await _refundRepo.UpdateAsync(refundRequestId, req);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = adminId,
                Role = "ADMIN",
                Action = "REFUND_APPROVED",
                Entity = "Payment",
                EntityId = payment.PaymentId
            });

            return await BuildDTO(refundRequestId);
        }

        public async Task<RefundRequestResponseDTO> RejectAsync(int refundRequestId, int adminId)
        {
            var req = await _refundRepo.GetQueryable()
                .FirstOrDefaultAsync(r => r.RefundRequestId == refundRequestId)
                ?? throw new NotFoundException("Refund request not found");

            if (req.Status != RefundRequestStatus.PENDING)
                throw new BadRequestException("Request already processed");

            req.Status = RefundRequestStatus.REJECTED;
            req.ReviewedByAdminId = adminId;
            req.ReviewedAt = DateTime.UtcNow;
            await _refundRepo.UpdateAsync(refundRequestId, req);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = adminId,
                Role = "ADMIN",
                Action = "REFUND_REJECTED",
                Entity = "RefundRequest",
                EntityId = refundRequestId
            });

            return await BuildDTO(refundRequestId);
        }

        private async Task<RefundRequestResponseDTO> BuildDTO(int id)
        {
            var r = await _refundRepo.GetQueryable()
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.Payment)
                .FirstAsync(x => x.RefundRequestId == id);
            return MapToDTO(r);
        }

        private static RefundRequestResponseDTO MapToDTO(RefundRequest r) => new()
        {
            RefundRequestId    = r.RefundRequestId,
            UserId             = r.UserId,
            UserName           = r.User?.Name  ?? string.Empty,
            UserEmail          = r.User?.Email ?? string.Empty,
            EventId            = r.EventId,
            EventTitle         = r.Event?.Title ?? string.Empty,
            PaymentId          = r.PaymentId,
            AmountPaid         = r.Payment?.AmountPaid ?? 0,
            RequestedAt        = r.RequestedAt,
            Status             = r.Status,
            ApprovedPercentage = r.ApprovedPercentage,
            ReviewedAt         = r.ReviewedAt
        };
    }
}
