using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Resource.Application.Common.Interfaces;
using Resource.Application.Models.Agent;
using System.Collections.Concurrent;
using System.Text;

namespace Resource.Infrastructure.Services
{
    /// <summary>
    /// Implementation of Gemini API Context Caching
    /// Reduces cost by 90% and improves latency by caching system instructions
    /// </summary>
    public class GeminiCacheService : IGeminiCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<GeminiCacheService> _logger;

        // In-memory tracking of cache names to avoid duplicate API calls
        private static readonly ConcurrentDictionary<string, CacheMetadata> _cacheRegistry = new();

        private const string SYSTEM_CONTEXT_CACHE_KEY = "stemify_system_context_cache";
        private const string CACHE_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/cachedContents";

        private const string SYSTEM_CONTEXT = @"Bạn là STEMify Assistant — trợ lý ảo chính thức của nền tảng học tập STEMify (https://stemify.vn),
                một website học STEM thế hệ mới kết hợp mô phỏng 3D, lập trình kéo-thả, và lộ trình học cá nhân hóa cho học sinh tiểu học.

                Nhiệm vụ của bạn:
                - Giải thích, hướng dẫn, hoặc hỗ trợ người dùng về các chủ đề thuộc STEM, giáo dục, khoa học, robot, công nghệ, và lập trình.
                - Có thể giới thiệu hoặc mô tả các tính năng của STEMify (ví dụ: mô phỏng 3D, bài học lập trình, khung chương trình STEM, khóa học hoặc nội dung học tập).
                - Nếu người dùng hỏi ngoài phạm vi STEM hoặc không liên quan đến STEMify, hãy lịch sự từ chối bằng câu:
                  ""Xin lỗi, tôi chỉ hỗ trợ các chủ đề liên quan đến STEM và nền tảng STEMify.""
                - Luôn trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu, và không quá 100 từ.
                - Giọng điệu thân thiện, mang phong cách giáo viên hướng dẫn học sinh.";

        public GeminiCacheService(
            HttpClient httpClient,
            IConfiguration config,
            IMemoryCache memoryCache,
            ILogger<GeminiCacheService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<string> GetOrCreateSystemContextCacheAsync()
        {
            try
            {
                // Check in-memory cache first
                if (_memoryCache.TryGetValue(SYSTEM_CONTEXT_CACHE_KEY, out string? cachedName)
                    && !string.IsNullOrEmpty(cachedName))
                {
                    // Verify cache is still valid
                    if (await IsCacheValidAsync(cachedName))
                    {
                        _logger.LogInformation("Using existing system context cache: {CacheName}", cachedName);
                        return cachedName;
                    }

                    _logger.LogWarning("System context cache expired, creating new one");
                }

                // Create new cache
                var cacheName = await CreateCachedContentAsync(
                    model: $"models/{_config["Gemini:Model"] ?? "gemini-1.5-flash-001"}",
                    systemInstruction: SYSTEM_CONTEXT,
                    contents: null,
                    ttlSeconds: 3600, // 1 hour
                    displayName: "STEMify System Context"
                );

                // Store in memory cache with expiration slightly less than Gemini cache TTL
                _memoryCache.Set(SYSTEM_CONTEXT_CACHE_KEY, cacheName, TimeSpan.FromMinutes(55));

                _logger.LogInformation("Created new system context cache: {CacheName}", cacheName);
                return cacheName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or create system context cache");
                throw;
            }
        }

        public async Task<string> CreateCachedContentAsync(
            string model,
            string systemInstruction,
            List<string>? contents = null,
            int ttlSeconds = 3600,
            string? displayName = null)
        {
            try
            {
                var request = new CreateCachedContentRequest
                {
                    model = model,
                    contents = contents?.Select(c => new Content
                    {
                        parts = new[] { new Part { text = c } }
                    }).ToArray() ?? Array.Empty<Content>(),
                    systemInstruction = new SystemInstruction
                    {
                        parts = new[] { new Part { text = systemInstruction } }
                    },
                    ttl = $"{ttlSeconds}s",
                    displayName = displayName
                };

                var jsonRequest = JsonConvert.SerializeObject(request, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var url = $"{CACHE_BASE_URL}?key={_config["Gemini:ApiKey"]}";

                _logger.LogDebug("Creating cached content: {DisplayName}", displayName);
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create cache. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to create cached content: {response.StatusCode}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CachedContentResponse>(jsonResponse);

                if (cacheResponse == null || string.IsNullOrEmpty(cacheResponse.name))
                {
                    throw new InvalidOperationException("Invalid cache response from Gemini API");
                }

                // Track cache metadata
                var metadata = new CacheMetadata
                {
                    CacheName = cacheResponse.name,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.Parse(cacheResponse.expireTime).ToUniversalTime(),
                    Purpose = displayName ?? "custom"
                };
                _cacheRegistry.TryAdd(cacheResponse.name, metadata);

                _logger.LogInformation("Successfully created cache: {CacheName}, expires: {ExpiresAt}",
                    cacheResponse.name, metadata.ExpiresAt);

                return cacheResponse.name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cached content");
                throw;
            }
        }

        public async Task DeleteCachedContentAsync(string cacheName)
        {
            try
            {
                var url = $"{CACHE_BASE_URL}/{cacheName}?key={_config["Gemini:ApiKey"]}";
                var response = await _httpClient.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _cacheRegistry.TryRemove(cacheName, out _);
                    _logger.LogInformation("Deleted cache: {CacheName}", cacheName);
                }
                else
                {
                    _logger.LogWarning("Failed to delete cache: {CacheName}, Status: {Status}",
                        cacheName, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cached content: {CacheName}", cacheName);
            }
        }

        public async Task<bool> IsCacheValidAsync(string cacheName)
        {
            try
            {
                // Check local registry first
                if (_cacheRegistry.TryGetValue(cacheName, out var metadata))
                {
                    if (metadata.ExpiresAt > DateTime.UtcNow)
                    {
                        return true;
                    }

                    // Expired locally, remove from registry
                    _cacheRegistry.TryRemove(cacheName, out _);
                    return false;
                }

                // Verify with Gemini API
                var retrievedMetadata = await GetCacheMetadataAsync(cacheName);
                return retrievedMetadata != null && retrievedMetadata.ExpiresAt > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CacheMetadata?> GetCacheMetadataAsync(string cacheName)
        {
            try
            {
                // Check local registry first
                if (_cacheRegistry.TryGetValue(cacheName, out var metadata))
                {
                    return metadata;
                }

                // Query Gemini API
                var url = $"{CACHE_BASE_URL}/{cacheName}?key={_config["Gemini:ApiKey"]}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var cacheResponse = JsonConvert.DeserializeObject<CachedContentResponse>(jsonResponse);

                if (cacheResponse == null)
                {
                    return null;
                }

                var retrievedMetadata = new CacheMetadata
                {
                    CacheName = cacheResponse.name,
                    CreatedAt = DateTime.Parse(cacheResponse.createTime).ToUniversalTime(),
                    ExpiresAt = DateTime.Parse(cacheResponse.expireTime).ToUniversalTime(),
                    Purpose = cacheResponse.displayName ?? "unknown"
                };

                _cacheRegistry.TryAdd(cacheName, retrievedMetadata);
                return retrievedMetadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache metadata: {CacheName}", cacheName);
                return null;
            }
        }
    }
}
