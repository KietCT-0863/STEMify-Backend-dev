using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ServiceStack;
using System.Collections.Concurrent;

namespace Caching.Cache
{
    /// <summary>
    /// Redis cache implementation with built-in metrics tracking
    /// Metrics are exposed via GetCacheStatistics() for Aspire/Prometheus integration
    /// </summary>
    public class CacheRedis : ICacheRedis
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<CacheRedis> _logger;

        private static readonly ConcurrentDictionary<string, long> CacheHits = new();
        private static readonly ConcurrentDictionary<string, long> CacheMisses = new();
        private static readonly ConcurrentDictionary<string, long> CacheSets = new();
        private static readonly ConcurrentDictionary<string, long> CacheClears = new();
        private static readonly ConcurrentDictionary<string, long> CacheErrors = new();

        public CacheRedis(IDistributedCache distributedCache, ILogger<CacheRedis> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public static CacheStatistics GetCacheStatistics()
        {
            return new CacheStatistics
            {
                Hits = CacheHits.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Misses = CacheMisses.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Sets = CacheSets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Clears = CacheClears.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Errors = CacheErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        private static string ExtractKeyPrefix(string cacheKey)
        {
            // Extract prefix before first colon for metrics labeling
            var colonIndex = cacheKey.IndexOf(':');
            return colonIndex > 0 ? cacheKey[..colonIndex] : cacheKey;
        }

        public async Task<string> GetAsync(string cacheKey)
        {
            var keyPrefix = ExtractKeyPrefix(cacheKey);

            try
            {
                var json = await _distributedCache.GetStringAsync(cacheKey);

                if (string.IsNullOrEmpty(json))
                {
                    CacheMisses.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
                    return null!;
                }

                CacheHits.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
                return json;
            }
            catch (Exception ex)
            {
                CacheErrors.AddOrUpdate($"get:{keyPrefix}", 1, (_, count) => count + 1);
                _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}. Returning null.", cacheKey);
                return null!;
            }
        }

        public async Task<T> GetAsync<T>(string cacheKey)
        {
            var keyPrefix = ExtractKeyPrefix(cacheKey);

            try
            {
                var json = await _distributedCache.GetStringAsync(cacheKey);
                if (json != null && !string.IsNullOrEmpty(json))
                {
                    try
                    {
                        CacheHits.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
                        return json.FromJson<T>();
                    }
                    catch (Exception ex)
                    {
                        CacheErrors.AddOrUpdate($"deserialize:{keyPrefix}", 1, (_, count) => count + 1);
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key {CacheKey}", cacheKey);
                        return default!;
                    }
                }

                CacheMisses.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
                return default!;
            }
            catch (Exception ex)
            {
                CacheErrors.AddOrUpdate($"get:{keyPrefix}", 1, (_, count) => count + 1);
                _logger.LogWarning(ex, "Redis GET failed for key {CacheKey}. Returning default.", cacheKey);
                return default!;
            }
        }

        public async Task SetAsync(string cacheKey, object response, TimeSpan timeLive)
        {
            if (response == null)
            {
                return;
            }

            var keyPrefix = ExtractKeyPrefix(cacheKey);

            try
            {
                var json = response.ToJson();
                await _distributedCache.SetStringAsync(
                    cacheKey,
                    json,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeLive }
                );

                CacheSets.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
            }
            catch (Exception ex)
            {
                CacheErrors.AddOrUpdate($"set:{keyPrefix}", 1, (_, count) => count + 1);
                _logger.LogWarning(ex, "Redis SET failed for key {CacheKey}. Continuing without cache.", cacheKey);
            }
        }

        public async Task IncreaseIntAsync(string cacheKey, TimeSpan timeLive)
        {
            try
            {
                var json = await _distributedCache.GetStringAsync(cacheKey);
                if (json == null || string.IsNullOrEmpty(json))
                {
                    await SetAsync(cacheKey, 1, timeLive);
                    return;
                }
                var totalCount = int.Parse(json);
                await SetAsync(cacheKey, totalCount + 1, timeLive);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis INCREASE failed for key {CacheKey}. Skipping operation.", cacheKey);
            }
        }

        public async Task ClearAsync(string cacheKey)
        {
            var keyPrefix = ExtractKeyPrefix(cacheKey);

            try
            {
                await _distributedCache.RemoveAsync(cacheKey);

                CacheClears.AddOrUpdate(keyPrefix, 1, (_, count) => count + 1);
            }
            catch (Exception ex)
            {
                CacheErrors.AddOrUpdate($"clear:{keyPrefix}", 1, (_, count) => count + 1);
                _logger.LogWarning(ex, "Redis CLEAR failed for key {CacheKey}. Skipping operation.", cacheKey);
            }
        }

        public string Get(string cacheKey)
        {
            try
            {
                var json = _distributedCache.GetString(cacheKey);
                return string.IsNullOrEmpty(json) ? null : json;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET (sync) failed for key {CacheKey}. Returning null.", cacheKey);
                return null;
            }
        }

        public T Get<T>(string cacheKey)
        {
            try
            {
                var json = _distributedCache.GetString(cacheKey);
                if (json != null && !string.IsNullOrEmpty(json))
                {
                    try
                    {
                        return json.FromJson<T>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key {CacheKey}", cacheKey);
                        return default(T);
                    }
                }
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis GET (sync) failed for key {CacheKey}. Returning default.", cacheKey);
                return default(T);
            }
        }

        public void Set(string cacheKey, object response, TimeSpan timeLive)
        {
            if (response == null)
            {
                return;
            }

            try
            {
                var json = response.ToJson();
                _distributedCache.SetString(
                    cacheKey,
                    json,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeLive }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis SET (sync) failed for key {CacheKey}. Continuing without cache.", cacheKey);
            }
        }

        public void Clear(string cacheKey)
        {
            try
            {
                _distributedCache.Remove(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis CLEAR (sync) failed for key {CacheKey}. Skipping operation.", cacheKey);
            }
        }
    }
}