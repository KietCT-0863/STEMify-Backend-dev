using Resource.Application.Models.Agent;

namespace Resource.Application.Common.Interfaces
{
    /// <summary>
    /// Service for managing Gemini API context caching
    /// Reduces latency and cost by caching repeated content like system instructions
    /// </summary>
    public interface IGeminiCacheService
    {
        /// <summary>
        /// Creates or retrieves cached system context for STEMify Assistant
        /// Cache is valid for 1 hour by default
        /// </summary>
        /// <returns>Cache name to use in subsequent requests (e.g., "cachedContents/abc123")</returns>
        Task<string> GetOrCreateSystemContextCacheAsync();

        /// <summary>
        /// Creates a new cached content with custom parameters
        /// </summary>
        /// <param name="model">Gemini model name (e.g., "models/gemini-1.5-flash-001")</param>
        /// <param name="systemInstruction">System instruction to cache</param>
        /// <param name="contents">Additional contents to cache</param>
        /// <param name="ttlSeconds">Time-to-live in seconds (default: 3600 = 1 hour)</param>
        /// <param name="displayName">Optional display name for the cache</param>
        /// <returns>Cache name for reference</returns>
        Task<string> CreateCachedContentAsync(
            string model,
            string systemInstruction,
            List<string>? contents = null,
            int ttlSeconds = 3600,
            string? displayName = null
        );

        /// <summary>
        /// Deletes a cached content by name
        /// </summary>
        /// <param name="cacheName">Cache name (e.g., "cachedContents/abc123")</param>
        Task DeleteCachedContentAsync(string cacheName);

        /// <summary>
        /// Checks if a cache is still valid (not expired)
        /// </summary>
        /// <param name="cacheName">Cache name to check</param>
        /// <returns>True if cache exists and is not expired</returns>
        Task<bool> IsCacheValidAsync(string cacheName);

        /// <summary>
        /// Gets metadata about a cached content
        /// </summary>
        /// <param name="cacheName">Cache name to query</param>
        /// <returns>Cache metadata including expiration time</returns>
        Task<CacheMetadata?> GetCacheMetadataAsync(string cacheName);
    }
}
