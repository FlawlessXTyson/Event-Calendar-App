using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Helpers;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class WalletService : IWalletService
    {
        private readonly IRepository<int, Wallet> _walletRepo;
        private readonly IRepository<int, WalletTransaction> _txRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, EventRegistration> _regRepo;
        private readonly IAuditLogRepository _auditRepo;

        public WalletService(
            IRepository<int, Wallet> walletRepo,
            IRepository<int, WalletTransaction> txRepo,
            IRepository<int, Payment> paymentRepo,
            IRepository<int, Event> eventRepo,
            IRepository<int, EventRegistration> regRepo,
            IAuditLogRepository auditRepo)
        {
            _walletRepo  = walletRepo;
            _txRepo      = txRepo;
            _paymentRepo = paymentRepo;
            _eventRepo   = eventRepo;
            _regRepo     = regRepo;
            _auditRepo   = auditRepo;
        }

        // ── Get or create wallet ──────────────────────────────────────────
        public async Task<WalletDTO> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await _walletRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                wallet = await _walletRepo.AddAsync(new Wallet { UserId = userId, Balance = 0 });

            return MapWallet(wallet);
        }

        // ── Add money to wallet ───────────────────────────────────────────
        public async Task<WalletDTO> AddMoneyAsync(int userId, AddMoneyRequestDTO request)
        {
            if (request.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero");

            var wallet = await GetOrCreateWalletEntityAsync(userId);
            wallet.Balance   += request.Amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                UserId      = userId,
                Amount      = request.Amount,
                Type        = WalletTransactionType.CREDIT,
                Source      = WalletTransactionSource.ADD_MONEY,
                Description = $"Added ₹{request.Amount:F2} via {request.PaymentMethod}"
            });

            return MapWallet(wallet);
        }

        // ── Pay for event using wallet ────────────────────────────────────
        public async Task<PaymentResponseDTO> PayWithWalletAsync(int userId, WalletPaymentRequestDTO request)
        {
            var ev = await _eventRepo.GetByIdAsync(request.EventId)
                ?? throw new NotFoundException("Event not found");

            if (!ev.IsPaidEvent)
                throw new BadRequestException("This event does not require payment");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved");

            if (ev.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active");

            var eventStart = ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero);
            if (IstClock.Now >= eventStart)
                throw new BadRequestException("Event has already started");

            // Check registration
            var isRegistered = await _regRepo.GetQueryable()
                .AnyAsync(r => r.UserId == userId && r.EventId == request.EventId && r.Status == RegistrationStatus.REGISTERED);
            if (!isRegistered)
                throw new BadRequestException("You must register before making payment");

            // Check duplicate payment
            var alreadyPaid = await _paymentRepo.GetQueryable()
                .AnyAsync(p => p.UserId == userId && p.EventId == request.EventId && p.Status == PaymentStatus.SUCCESS);
            if (alreadyPaid)
                throw new BadRequestException("You have already paid for this event");

            var wallet = await GetOrCreateWalletEntityAsync(userId);
            if (wallet.Balance < ev.TicketPrice)
                throw new BadRequestException($"Insufficient wallet balance. Required: ₹{ev.TicketPrice}, Available: ₹{wallet.Balance:F2}");

            // Deduct from wallet
            wallet.Balance   -= ev.TicketPrice;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                UserId      = userId,
                Amount      = ev.TicketPrice,
                Type        = WalletTransactionType.DEBIT,
                Source      = WalletTransactionSource.PAYMENT,
                Description = $"Payment for event: {ev.Title}"
            });
            // Create payment record
            float commission      = ev.TicketPrice * ev.CommissionPercentage / 100f;
            float organizerAmount = ev.TicketPrice - commission;

            var payment = await _paymentRepo.AddAsync(new Payment
            {
                UserId           = userId,
                EventId          = request.EventId,
                AmountPaid       = ev.TicketPrice,
                CommissionAmount = commission,
                OrganizerAmount  = organizerAmount,
                Status           = PaymentStatus.SUCCESS,
                PaymentDate      = DateTime.UtcNow
            });

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId   = userId,
                Role     = "USER",
                Action   = "WALLET_PAYMENT",
                Entity   = "Payment",
                EntityId = payment.PaymentId
            });

            return MapPayment(payment, ev);
        }

        // ── Get transaction history ───────────────────────────────────────
        public async Task<IEnumerable<WalletTransactionDTO>> GetTransactionsAsync(int userId)
        {
            var txs = await _txRepo.GetQueryable()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return txs.Select(MapTx);
        }

        // ── Credit wallet (internal use) ──────────────────────────────────
        public async Task CreditAsync(int userId, float amount, string source, string description)
        {
            if (amount <= 0) return;
            var wallet = await GetOrCreateWalletEntityAsync(userId);
            wallet.Balance   += amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            var txSource = source switch
            {
                "COMMISSION"    => WalletTransactionSource.COMMISSION,
                "COMPENSATION"  => WalletTransactionSource.COMPENSATION,
                "ORGANIZER_EARNING" => WalletTransactionSource.ORGANIZER_EARNING,
                "ADD_MONEY"     => WalletTransactionSource.ADD_MONEY,
                _               => WalletTransactionSource.REFUND
            };

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                UserId      = userId,
                Amount      = amount,
                Type        = WalletTransactionType.CREDIT,
                Source      = txSource,
                Description = description
            });
        }

        // ── Debit wallet (internal use — system refunds, allows negative balance) ──
        public async Task DebitAsync(int userId, float amount, string source, string description)
        {
            if (amount <= 0) return;
            var wallet = await GetOrCreateWalletEntityAsync(userId);

            // Allow balance to go negative for system-initiated refunds
            // (organizer/admin may not have enough balance but refund must proceed)
            wallet.Balance   -= amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                UserId      = userId,
                Amount      = amount,
                Type        = WalletTransactionType.DEBIT,
                Source      = WalletTransactionSource.PAYMENT,
                Description = description
            });
        }

        // ── Debit wallet (user-initiated — blocks if insufficient) ────────
        public async Task DebitStrictAsync(int userId, float amount, string source, string description)
        {
            if (amount <= 0) return;
            var wallet = await GetOrCreateWalletEntityAsync(userId);
            if (wallet.Balance < amount)
                throw new BadRequestException($"Insufficient wallet balance. Required: ₹{amount:F2}, Available: ₹{wallet.Balance:F2}");

            wallet.Balance   -= amount;
            wallet.UpdatedAt  = DateTime.UtcNow;
            await _walletRepo.UpdateAsync(wallet.WalletId, wallet);

            await _txRepo.AddAsync(new WalletTransaction
            {
                WalletId    = wallet.WalletId,
                UserId      = userId,
                Amount      = amount,
                Type        = WalletTransactionType.DEBIT,
                Source      = WalletTransactionSource.PAYMENT,
                Description = description
            });
        }

        // ── Private helpers ───────────────────────────────────────────────
        private async Task<Wallet> GetOrCreateWalletEntityAsync(int userId)
        {
            var wallet = await _walletRepo.GetQueryable()
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                wallet = await _walletRepo.AddAsync(new Wallet { UserId = userId, Balance = 0 });

            return wallet;
        }

        private static WalletDTO MapWallet(Wallet w) => new()
        {
            WalletId  = w.WalletId,
            UserId    = w.UserId,
            Balance   = w.Balance,
            UpdatedAt = w.UpdatedAt
        };

        private static WalletTransactionDTO MapTx(WalletTransaction t) => new()
        {
            TransactionId = t.TransactionId,
            UserId        = t.UserId,
            Amount        = t.Amount,
            Type          = t.Type.ToString(),
            Source        = t.Source.ToString(),
            Description   = t.Description,
            CreatedAt     = t.CreatedAt
        };

        private static PaymentResponseDTO MapPayment(Payment p, Event ev) => new()
        {
            PaymentId        = p.PaymentId,
            EventId          = p.EventId,
            EventTitle       = ev.Title,
            EventDate        = ev.EventDate,
            UserId           = p.UserId,
            AmountPaid       = p.AmountPaid,
            OrganizerAmount  = p.OrganizerAmount,
            Status           = p.Status,
            PaymentDate      = p.PaymentDate
        };
    }
}
