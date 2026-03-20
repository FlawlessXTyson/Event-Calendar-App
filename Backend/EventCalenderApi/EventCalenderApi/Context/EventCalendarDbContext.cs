using Microsoft.EntityFrameworkCore;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

namespace EventCalenderApi.EventCalenderAppDataLibrary
{
    public class EventCalendarDbContext : DbContext
    {
        public EventCalendarDbContext(DbContextOptions<EventCalendarDbContext> options)
            : base(options)
        {
        }

        //================ TABLES =================

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Todo> Todos { get; set; }
        public DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //================ USER =================

            //unique email constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            //================ PRIMARY KEYS (ADDED FIX) =================

            modelBuilder.Entity<EventRegistration>()
                .HasKey(r => r.RegistrationId);

            modelBuilder.Entity<RoleChangeRequest>()
                .HasKey(r => r.RequestId);

            //================ ADMIN SEED =================

            //modelBuilder.Entity<User>().HasData(
            //    new User
            //    {
            //        UserId = 1,
            //        Name = "Admin",
            //        Email = "admin@gmail.com",

            //        // IMPORTANT: Replace with YOUR GENERATED HASH
            //        PasswordHash = "$2a$11$REPLACE_WITH_CORRECT_HASH",

            //        Role = UserRole.ADMIN,
            //        Status = AccountStatus.ACTIVE,
            //        CreatedAt = new DateTime(2024, 1, 1)
            //    }
            //);

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

            //================ NOTE =================

            modelBuilder.Entity<Note>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
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

            // PREVENT DUPLICATE PENDING REQUESTS (DB LEVEL)
            modelBuilder.Entity<RoleChangeRequest>()
                .HasIndex(r => r.UserId)
                .HasFilter("[Status] = 1") // 1 = Pending
                .IsUnique();
        }
    }
}