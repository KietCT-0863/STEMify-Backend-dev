using Contracts.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Resource.Infrastructure.Configurations;

/// <summary>
/// Base entity configuration for all domain entities
/// Provides common configuration rules following Clean Architecture principles
/// </summary>
/// <typeparam name="T">Entity type that extends EntityBase<K></typeparam>
/// <typeparam name="K">Primary key type</typeparam>
public abstract class BaseEntityConfiguration<T, K> : IEntityTypeConfiguration<T>
    where T : EntityBase<K>
{
    /// <summary>
    /// Configure common entity properties
    /// Override in derived classes for specific entity configurations
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // Configure primary key
        builder.HasKey(e => e.Id);

        // Configure ID property based on type
        ConfigureIdProperty(builder);

        // Configure common properties if they exist in EntityBase
        ConfigureCommonProperties(builder);
    }

    /// <summary>
    /// Configure ID property based on its type
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    protected virtual void ConfigureIdProperty(EntityTypeBuilder<T> builder)
    {
        var idProperty = builder.Property(e => e.Id);

        // Configure based on ID type
        if (typeof(K) == typeof(int))
        {
            idProperty.ValueGeneratedOnAdd(); // Auto-increment for int IDs
        }
        else if (typeof(K) == typeof(Guid))
        {
            idProperty.ValueGeneratedOnAdd(); // Generated GUIDs
        }
        else if (typeof(K) == typeof(string))
        {
            idProperty.ValueGeneratedNever(); // Manual string IDs
        }
    }

    /// <summary>
    /// Configure common properties that may exist in EntityBase
    /// Override in specific configurations if needed
    /// </summary>
    /// <param name="builder">Entity type builder</param>
    protected virtual void ConfigureCommonProperties(EntityTypeBuilder<T> builder)
    {
        // Add common property configurations here if EntityBase has them
        // Example: CreatedDate, UpdatedDate, etc.

        // Check if entity has CreatedAt property via reflection
        var createdAtProperty = typeof(T).GetProperty("CreatedAt");
        if (createdAtProperty != null)
        {
            builder
                .Property("CreatedAt")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();
        }

        // Check if entity has UpdatedAt property via reflection
        var updatedAtProperty = typeof(T).GetProperty("UpdatedAt");
        if (updatedAtProperty != null)
        {
            builder
                .Property("UpdatedAt")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}

/// <summary>
/// Specialized base configuration for entities with integer IDs
/// Most common case in the system
/// </summary>
/// <typeparam name="T">Entity type with integer ID</typeparam>
public abstract class BaseIntEntityConfiguration<T> : BaseEntityConfiguration<T, int>
    where T : EntityBase<int>
{
    // Inherits all base configuration with int specialization
}

/// <summary>
/// Specialized base configuration for entities with GUID IDs
/// For entities requiring unique global identifiers
/// </summary>
/// <typeparam name="T">Entity type with GUID ID</typeparam>
public abstract class BaseGuidEntityConfiguration<T> : BaseEntityConfiguration<T, Guid>
    where T : EntityBase<Guid>
{
    // Inherits all base configuration with GUID specialization
}
