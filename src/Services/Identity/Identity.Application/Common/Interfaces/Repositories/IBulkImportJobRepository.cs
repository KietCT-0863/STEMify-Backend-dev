using Contracts.Abstractions.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Common.Interfaces.Repositories;

public interface IBulkImportJobRepository : IRepositoryBaseAsync<BulkImportJob, Guid>
{
    /// <summary>
    /// Get all jobs for an organization with pagination
    /// </summary>
    Task<List<BulkImportJob>> GetByOrganizationAsync(
        int organizationId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs that are pending processing
    /// </summary>
    Task<List<BulkImportJob>> GetPendingJobsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count jobs by organization
    /// </summary>
    Task<int> CountByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs by status for monitoring
    /// </summary>
    Task<List<BulkImportJob>> GetByStatusAsync(
      BulkImportStatus status,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// Get recently completed jobs with failures for error analysis
    /// </summary>
    Task<List<BulkImportJob>> GetRecentJobsWithFailuresAsync(
         int organizationId,
         int count = 10,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if there's any job currently processing for organization
    /// </summary>
    Task<bool> HasActiveJobAsync(
        int organizationId,
        CancellationToken cancellationToken = default);
    
}
