using Contracts.Common.Domain;
using Contracts.Domains;
using Identity.Domain.Common;
using Identity.Domain.Entities;
using Identity.Infrastructure.Identity;
using Identity.Application.ReadModels;
using MassTransit;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace Identity.Infrastructure.Data
{
    /// <summary>
    /// Application database context supporting TPT inheritance pattern
    /// Uses domain entities directly with ASP.NET Identity integration via TPT strategy
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApplicationDbContext>? _logger;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IServiceProvider serviceProvider)
            : base(options)
        {
            _serviceProvider = serviceProvider;

            _logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>();
        }

        public new DbSet<User> Users { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<JobRole> JobRoles { get; set; } = null!;
        public DbSet<BulkImportJob> BulkImportJobs { get; set; } = null!;
        public DbSet<Invitation> Invitations { get; set; } = null!;
        public DbSet<OrganizationUser> OrganizationUsers { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;

        public DbSet<OrganizationUserLicenseReadModel> OrganizationUserLicenseReadModels { get; set; } = null!;


        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.UseOpenIddict<Guid>();

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.AddInboxStateEntity();
            builder.AddOutboxMessageEntity();
            builder.AddOutboxStateEntity();

            ConfigureDomainEvents(builder);
        }

        
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Collect domain events from aggregate roots
            var entitiesWithEvents = ChangeTracker.Entries()
                .Where(e => e.Entity is IAggregateBase)
                .Select(e => e.Entity as IAggregateBase)
                .Where(e => e != null && e.HasUncommittedDomainEvents())
                .ToList();

            var allDomainEvents = new List<Contracts.Abstractions.Event.IDomainEvent>();
            foreach (var entity in entitiesWithEvents)
            {
                if (entity == null) continue;
                var events = entity.GetUncommittedDomainEvents();
                if (events != null && events.Count > 0)
                {
                    allDomainEvents.AddRange(events);
                }
            }

            if (allDomainEvents.Count == 0)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var currentTransaction = Database.CurrentTransaction;
            var shouldManageTransaction = currentTransaction == null;

            if (shouldManageTransaction)
            {
                var strategy = Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var result = await SaveChangesWithEventsAsync(allDomainEvents, entitiesWithEvents, cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                return await SaveChangesWithEventsAsync(allDomainEvents, entitiesWithEvents, cancellationToken);
            }
        }

        private async Task<int> SaveChangesWithEventsAsync(
            List<Contracts.Abstractions.Event.IDomainEvent> allDomainEvents,
            List<IAggregateBase?> entitiesWithEvents,
            CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested();

            var publishEndpoint = _serviceProvider.GetService<IPublishEndpoint>();
            if (publishEndpoint != null)
            {
                var endpointType = publishEndpoint.GetType().FullName;
                _logger?.LogInformation(
                    "Publishing {Count} domain events within transaction. IPublishEndpoint type: {EndpointType}",
                    allDomainEvents.Count,
                    endpointType);

                foreach (var domainEvent in allDomainEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    _logger?.LogDebug("Publishing event: {EventType}", domainEvent.GetType().Name);
                    await publishEndpoint.Publish(domainEvent,domainEvent.GetType(), cancellationToken);
                }

                _logger?.LogInformation("Events published to Outbox, now saving entity changes...");
            }
            else
            {
                _logger?.LogWarning("IPublishEndpoint not available - domain events will not be published");
            }

            // Clear events from aggregates
            foreach (var entity in entitiesWithEvents)
            {
                entity?.ClearDomainEvents();
            }

            // Save entity changes within same transaction as Outbox messages
            var result = await base.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Entity changes and Outbox messages saved. Transaction will commit.");

            return result;
        }

        /// <summary>
        /// Configure domain events handling for aggregate roots
        /// </summary>
        private static void ConfigureDomainEvents(ModelBuilder builder)
        {
            builder.Ignore<DomainEvent>();

            // Find all entity types that implement IAggregateRoot and ignore DomainEvents
            var aggregateRootTypes = builder
                .Model.GetEntityTypes()
                .Where(e =>
                    e.ClrType.GetInterfaces()
                        .Any(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>)
                        )
                )
                .ToList();

            foreach (var entityType in aggregateRootTypes)
            {
                builder.Entity(entityType.ClrType).Ignore("DomainEvents");
            }
        }
    }
}
