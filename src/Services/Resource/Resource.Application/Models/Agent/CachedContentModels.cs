namespace Resource.Application.Models.Agent
{
    /// <summary>
    /// Request model for creating cached content in Gemini API
    /// </summary>
    public class CreateCachedContentRequest
    {
        public string model { get; set; }
        public Content[] contents { get; set; }
        public SystemInstruction? systemInstruction { get; set; }
        public string? ttl { get; set; } // Format: "3600s" for 1 hour
        public string? displayName { get; set; }
    }

    /// <summary>
    /// Response model from Gemini cachedContents.create endpoint
    /// </summary>
    public class CachedContentResponse
    {
        public string name { get; set; } // Format: "cachedContents/{id}"
        public string model { get; set; }
        public string createTime { get; set; }
        public string updateTime { get; set; }
        public string expireTime { get; set; }
        public string? displayName { get; set; }
        public int usageMetadata { get; set; }
    }

    /// <summary>
    /// System instruction for cached content
    /// </summary>
    public class SystemInstruction
    {
        public Part[] parts { get; set; }
    }

    /// <summary>
    /// Request model for generateContent using cached context
    /// </summary>
    public class GenerateContentWithCacheRequest
    {
        public Content[] contents { get; set; }
        public string? cachedContent { get; set; } // Reference to cached content name
    }

    /// <summary>
    /// Cache metadata for tracking
    /// </summary>
    public class CacheMetadata
    {
        public string CacheName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Purpose { get; set; } // e.g., "system_context", "course_recommendations"
    }
}
