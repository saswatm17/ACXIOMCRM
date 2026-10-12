using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer indexes and constraints
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(c => c.CustomerCode).IsUnique();
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();

                entity.HasOne(c => c.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(c => c.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Lead indexes, relationships and decimal precision
            builder.Entity<Lead>(entity =>
            {
                entity.HasIndex(l => l.LeadCode).IsUnique();
                entity.HasIndex(l => l.Email);
                entity.HasIndex(l => l.Phone);

                entity.Property(l => l.ExpectedValue)
                      .HasPrecision(18, 2);

                entity.HasOne(l => l.AssignedToUser)
                      .WithMany()
                      .HasForeignKey(l => l.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Customer)
                      .WithMany(c => c.Leads)
                      .HasForeignKey(l => l.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Opportunity relationships, decimal precision and constraints
            builder.Entity<Opportunity>(entity =>
            {
                entity.Property(o => o.Amount)
                      .HasPrecision(18, 2);

                entity.HasOne(o => o.Customer)
                      .WithMany(c => c.Opportunities)
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                      .WithMany(l => l.Opportunities)
                      .HasForeignKey(o => o.LeadId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.AssignedToUser)
                      .WithMany()
                      .HasForeignKey(o => o.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // FollowUp relationships
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasOne(f => f.Customer)
                      .WithMany(c => c.FollowUps)
                      .HasForeignKey(f => f.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Lead)
                      .WithMany(l => l.FollowUps)
                      .HasForeignKey(f => f.LeadId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.AssignedToUser)
                      .WithMany()
                      .HasForeignKey(f => f.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Activity relationships
            builder.Entity<Activity>(entity =>
            {
                entity.HasOne(a => a.Customer)
                      .WithMany(c => c.Activities)
                      .HasForeignKey(a => a.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Lead)
                      .WithMany(l => l.Activities)
                      .HasForeignKey(a => a.LeadId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.AssignedToUser)
                      .WithMany()
                      .HasForeignKey(a => a.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog relationships and indexes
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.Action);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
