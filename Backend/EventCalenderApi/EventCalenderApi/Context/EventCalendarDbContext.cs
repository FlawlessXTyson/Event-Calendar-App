using Microsoft.EntityFrameworkCore;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using System.Security.Claims;

namespace EventCalenderApi.EventCalenderAppDataLibrary
{
    public class EventCalendarDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventCalendarDbContext(
            DbContextOptions<EventCalendarDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        //================ TABLES =================

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Todo> Todos { get; set; }
        public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<RefundRequest> RefundRequests { get; set; }

        //  AUTO AUDIT LOG (ADDED ONLY THIS)
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditLogs = new List<AuditLog>();

            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;

            int userId = userIdClaim != null ? int.Parse(userIdClaim) : 0;
            string role = roleClaim ?? "SYSTEM";

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached)
                    continue;

                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                {
                    auditLogs.Add(new AuditLog
                    {
                        UserId = userId,
                        Role = role,
                        Action = entry.State.ToString().ToUpper(), // ADDED / MODIFIED / DELETED
                        Entity = entry.Entity.GetType().Name,
                        EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue as int? ?? 0,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            if (auditLogs.Any())
            {
                AuditLogs.AddRange(auditLogs);
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //================ USER =================
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            //================ PRIMARY KEYS =================
            modelBuilder.Entity<EventRegistration>()
                .HasKey(r => r.RegistrationId);

            modelBuilder.Entity<RoleChangeRequest>()
                .HasKey(r => r.RequestId);

            //================ EVENT =================
            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedBy)
                .WithMany(u => u.EventsCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.ApprovedBy)
                .WithMany(u => u.EventsApproved)
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            //================ EVENT REGISTRATION =================
            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            //================ PAYMENT =================
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Event)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            //================ REMINDER =================
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //================ TODO =================
            modelBuilder.Entity<Todo>()
                .HasOne(t => t.User)
                .WithMany(u => u.Todos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //================ ROLE CHANGE REQUEST =================
            modelBuilder.Entity<RoleChangeRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleChangeRequest>()
                .HasIndex(r => r.UserId)
                .HasFilter("[Status] = 1")
                .IsUnique();

            //================ TICKET =================
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Event)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            //================ REFUND REQUEST =================
            modelBuilder.Entity<RefundRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefundRequest>()
                .HasOne(r => r.Event)
                .WithMany()
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RefundRequest>()
                .HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}