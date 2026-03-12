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

        // Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Todo> Todos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // primary keys

            // User
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            // Event
            modelBuilder.Entity<Event>()
                .HasKey(e => e.EventId);

            // EventRegistration
            modelBuilder.Entity<EventRegistration>()
                .HasKey(er => er.RegistrationId);

            // Payment
            modelBuilder.Entity<Payment>()
                .HasKey(p => p.PaymentId);

            // Note
            modelBuilder.Entity<Note>()
                .HasKey(n => n.NoteId);

            // Reminder
            modelBuilder.Entity<Reminder>()
                .HasKey(r => r.ReminderId);

            // Todo
            modelBuilder.Entity<Todo>()
                .HasKey(t => t.TodoId);

            //relaionships btw themm

            // Event → CreatedBy (User)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedBy)
                .WithMany(u => u.EventsCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event → ApprovedBy (User)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.ApprovedBy)
                .WithMany(u => u.EventsApproved)
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);


            //event registration relationships

            // Registration → User
            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Registration → Event
            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            //payment relationships

            // Payment → User
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment → Event
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Event)
                .WithMany(e => e.Payments)
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);


            //notes

            modelBuilder.Entity<Note>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            //reminders

            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            //todos

            modelBuilder.Entity<Todo>()
                .HasOne(t => t.User)
                .WithMany(u => u.Todos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}