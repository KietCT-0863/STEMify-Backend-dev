using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core configuration for ApplicationUser base class implementing TPT inheritance
    /// Configures the base table and common domain properties for all user types
    /// </summary>
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Configure TPT inheritance strategy
            builder.UseTptMappingStrategy();

            // Base table configuration (AspNetUsers from Identity)
            builder.ToTable("AspNetUsers");

            // Configure domain properties
            ConfigureDomainProperties(builder);

            // Configure indexes for performance
            ConfigureIndexes(builder);

            // Configure computed properties to ignore
            ConfigureIgnoredProperties(builder);
        }

        /// <summary>
        /// Configure domain-specific properties with validation and constraints
        /// </summary>
        private static void ConfigureDomainProperties(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasComment("User role (Teacher, Student, etc.)");

            builder
                .Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasComment("User status (Active, Pending, Disabled, etc.)");

            // Configure timestamps
            builder
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired()
                .HasComment("User creation timestamp");

            builder
                .Property(u => u.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired()
                .HasComment("User last update timestamp");

            builder.Property(u => u.LastLoginAt).HasComment("User last login timestamp");

            builder.Property(u => u.EmailConfirmedAt).HasComment("Email confirmation timestamp");
        }

        /// <summary>
        /// Configure indexes for common query patterns
        /// </summary>
        private static void ConfigureIndexes(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Index on role for filtering users by type
            builder.HasIndex(u => u.Role).HasDatabaseName("IX_AspNetUsers_Role");

            // Index on status for filtering active users
            builder.HasIndex(u => u.Status).HasDatabaseName("IX_AspNetUsers_Status");

            // Composite index for role and status queries
            builder
                .HasIndex(u => new { u.Role, u.Status })
                .HasDatabaseName("IX_AspNetUsers_Role_Status");

            // Index on creation date for sorting
            builder.HasIndex(u => u.CreatedAt).HasDatabaseName("IX_AspNetUsers_CreatedAt");
        }

        /// <summary>
        /// Configure properties to ignore in database mapping
        /// </summary>
        private static void ConfigureIgnoredProperties(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Ignore computed properties
            builder.Ignore(u => u.FullName);

            // Ignore domain events collection
            builder.Ignore(u => u.DomainEvents);
        }
    }
}
