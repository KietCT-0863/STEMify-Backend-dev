using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for User entity (TPT inheritance)
/// Configures the concrete User table for Admin, Staff, and Guest roles
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // TPT inheritance - User table inherits from AspNetUsers
        builder.ToTable("Users");

        // Properties
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();

        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();

        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();

        // Indexes
        builder
            .HasIndex(u => new { u.FirstName, u.LastName })
            .HasDatabaseName("IX_Users_FullName");
    }
}
