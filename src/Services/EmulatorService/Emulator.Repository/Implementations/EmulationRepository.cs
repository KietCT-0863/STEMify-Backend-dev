using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Emulator.Repository.Configuration;
using Emulator.Repository.Models;

namespace Emulator.Repository.Implementations;

/// <summary>
/// MongoDB implementation of IEmulationRepository
/// </summary>
public class EmulationRepository : IEmulationRepository
{
    private readonly IMongoCollection<Emulation> _emulations;

    public EmulationRepository(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _emulations = database.GetCollection<Emulation>(settings.Value.EmulationsCollectionName);
    }

    public async Task<Emulation> CreateAsync(Emulation emulation)
    {
        await _emulations.InsertOneAsync(emulation);
        return emulation;
    }

    public async Task<Emulation?> GetByIdAsync(string emulationId)
    {
        return await _emulations
            .Find(e => e.EmulationId == emulationId && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<Emulation?> GetBySlugAsync(string slug)
    {
        return await _emulations
            .Find(e => e.Slug == slug && !e.IsDeleted)
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResult<Emulation>> GetPagedAsync(EmulationFilterParams filters)
    {
        var filterBuilder = Builders<Emulation>.Filter;
        var filter = filterBuilder.Eq(e => e.IsDeleted, false);

        // Search filter
        if (!string.IsNullOrEmpty(filters.Search))
        {
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(e => e.Name, new MongoDB.Bson.BsonRegularExpression(filters.Search, "i")),
                filterBuilder.Regex(e => e.Description, new MongoDB.Bson.BsonRegularExpression(filters.Search, "i"))
            );
            filter = filterBuilder.And(filter, searchFilter);
        }

        // Difficulty filter
        if (!string.IsNullOrEmpty(filters.Difficulty))
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(e => e.Definition.Metadata.Difficulty, filters.Difficulty));
        }

        // Status filter
        if (!string.IsNullOrEmpty(filters.Status))
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(e => e.Status, filters.Status));
        }

        // Visibility filter
        if (!string.IsNullOrEmpty(filters.Visibility))
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(e => e.Visibility, filters.Visibility));
        }

        // Tags filter
        if (filters.Tags != null && filters.Tags.Count > 0)
        {
            filter = filterBuilder.And(filter, filterBuilder.AnyIn(e => e.Definition.Metadata.Tags, filters.Tags));
        }

        // CreatedBy filter
        if (!string.IsNullOrEmpty(filters.CreatedByUserId))
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(e => e.CreatedBy, filters.CreatedByUserId));
        }

        // Get total count
        var total = await _emulations.CountDocumentsAsync(filter);

        // Sorting
        var sortBuilder = Builders<Emulation>.Sort;
        var sort = filters.SortOrder.ToLower() == "asc"
            ? sortBuilder.Ascending(filters.SortBy)
            : sortBuilder.Descending(filters.SortBy);

        // Pagination
        var items = await _emulations
            .Find(filter)
            .Sort(sort)
            .Skip((filters.Page - 1) * filters.Limit)
            .Limit(filters.Limit)
            .ToListAsync();

        return new PagedResult<Emulation>
        {
            Items = items,
            TotalCount = (int)total,
            PageNumber = filters.Page,
            PageSize = filters.Limit
        };
    }

    public async Task<bool> UpdateAsync(Emulation emulation)
    {
        emulation.UpdatedAt = DateTime.UtcNow;
        
        var result = await _emulations.ReplaceOneAsync(
            e => e.EmulationId == emulation.EmulationId,
            emulation
        );
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string emulationId, bool permanent = false)
    {
        if (permanent)
        {
            var result = await _emulations.DeleteOneAsync(e => e.EmulationId == emulationId);
            return result.DeletedCount > 0;
        }
        else
        {
            var update = Builders<Emulation>.Update
                .Set(e => e.IsDeleted, true)
                .Set(e => e.DeletedAt, DateTime.UtcNow);
            var result = await _emulations.UpdateOneAsync(
                e => e.EmulationId == emulationId,
                update
            );
            return result.ModifiedCount > 0;
        }
    }

    public async Task<bool> ExistsAsync(string emulationId)
    {
        return await _emulations
            .Find(e => e.EmulationId == emulationId && !e.IsDeleted)
            .AnyAsync();
    }

    public async Task<List<Emulation>> GetByCreatorAsync(string userId)
    {
        return await _emulations
            .Find(e => e.CreatedBy == userId && !e.IsDeleted)
            .SortByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Emulation>> GetPublishedAsync(int limit = 10)
    {
        return await _emulations
            .Find(e => e.Status == "published" && !e.IsDeleted)
            .SortByDescending(e => e.PublishedAt)
            .Limit(limit)
            .ToListAsync();
    }
}
