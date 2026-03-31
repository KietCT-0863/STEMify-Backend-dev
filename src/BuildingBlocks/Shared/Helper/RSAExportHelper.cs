using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Shared.Helper
{
    public static class RSAExportHelper
    {
        public static bool HasMediaContent(string contentBody)
        {
            return contentBody.Contains("<img") ||
                   contentBody.Contains("<video") ||
                   contentBody.Contains("<audio") ||
                   contentBody.Contains("youtube.com") ||
                   contentBody.Contains(".mp4") ||
                   contentBody.Contains(".mp3") ||
                   contentBody.Contains(".png") ||
                   contentBody.Contains(".jpg");
        }

        public static List<string> ExtractImageUrlsFromHtml(string htmlContent)
        {
            var urls = new List<string>();
            if (string.IsNullOrEmpty(htmlContent)) return urls;

            // Regex to match img src attributes with Cloudinary URLs
            var imgRegex = new Regex(@"<img[^>]+src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            var matches = imgRegex.Matches(htmlContent);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (IsValidImageUrl(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            return urls.Distinct().ToList();
        }

        public static List<string> ExtractVideoUrlsFromHtml(string htmlContent)
        {
            var urls = new List<string>();
            if (string.IsNullOrEmpty(htmlContent)) return urls;

            // Regex to match video src attributes
            var videoRegex = new Regex(@"<video[^>]+src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);
            var sourceRegex = new Regex(@"<source[^>]+src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);

            var matches = videoRegex.Matches(htmlContent);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (IsValidVideoUrl(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            // Only check source tags that are inside video elements, not audio elements
            var videoSourceRegex = new Regex(@"<video[^>]*>.*?<source[^>]+src=[""']([^""']+)[""'][^>]*>.*?</video>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var videoSourceMatches = videoSourceRegex.Matches(htmlContent);
            foreach (Match match in videoSourceMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (IsValidVideoUrl(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            return urls.Distinct().ToList();
        }

        public static List<string> ExtractAudioUrlsFromHtml(string htmlContent)
        {
            var urls = new List<string>();
            if (string.IsNullOrEmpty(htmlContent)) return urls;

            // Regex to match audio src attributes
            var audioRegex = new Regex(@"<audio[^>]+src=[""']([^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);

            var matches = audioRegex.Matches(htmlContent);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (IsValidAudioUrl(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            // Check source tags that are inside audio elements
            var audioSourceRegex = new Regex(@"<audio[^>]*>.*?<source[^>]+src=[""']([^""']+)[""'][^>]*>.*?</audio>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var audioSourceMatches = audioSourceRegex.Matches(htmlContent);
            foreach (Match match in audioSourceMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value;
                    if (IsValidAudioUrl(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            return urls.Distinct().ToList();
        }

        public static string GetFileNameFromCloudinaryUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;

                if (segments.Length > 0)
                {
                    var lastSegment = segments[segments.Length - 1];
                    // Remove the leading slash if present
                    if (lastSegment.StartsWith("/"))
                        lastSegment = lastSegment.Substring(1);

                    // URL decode the filename to handle special characters
                    lastSegment = Uri.UnescapeDataString(lastSegment);

                    // If no extension, try to determine from URL
                    if (!Path.HasExtension(lastSegment))
                    {
                        var fileExtension = DetermineExtensionFromUrl(url);
                        lastSegment += fileExtension;
                    }

                    // Clean up filename for file system
                    lastSegment = CleanFileName(lastSegment);

                    return lastSegment;
                }

                // Fallback: generate a name based on URL hash
                var extension = DetermineExtensionFromUrl(url);
                return $"asset_{Math.Abs(url.GetHashCode())}{extension}";
            }
            catch
            {
                // Fallback filename
                var extension = DetermineExtensionFromUrl(url);
                return $"asset_{Math.Abs(url.GetHashCode())}{extension}";
            }
        }

        public static string CleanFileName(string fileName)
        {
            // Remove or replace invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Also replace some common problematic characters
            fileName = fileName.Replace("%", "_")
                               .Replace(" ", "_")
                               .Replace("#", "_")
                               .Replace("&", "_")
                               .Replace("?", "_");

            return fileName;
        }

        public static bool IsValidVideoUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            // Check file extension first (most reliable)
            if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".wmv", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // If it's a Cloudinary URL, check if it's NOT an audio file
            if (url.Contains("cloudinary.com/"))
            {
                return !IsAudioFileExtension(url);
            }

            return false;
        }

        public static bool IsValidAudioUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            // Check file extension first (most reliable)
            if (url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
                url.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // For Cloudinary URLs, we need to be more specific
            if (url.Contains("cloudinary.com/"))
            {
                return IsAudioFileExtension(url);
            }

            return false;
        }

        public static bool IsAudioFileExtension(string url)
        {
            return url.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".flac", StringComparison.OrdinalIgnoreCase);
        }

        public static string DetermineExtensionFromUrl(string url)
        {
            // First check if URL already has a valid extension
            var urlExtension = Path.GetExtension(url).ToLower();
            if (!string.IsNullOrEmpty(urlExtension) &&
                (urlExtension == ".mp3" || urlExtension == ".wav" || urlExtension == ".ogg" ||
                 urlExtension == ".mp4" || urlExtension == ".webm" || urlExtension == ".avi" ||
                 urlExtension == ".png" || urlExtension == ".jpg" || urlExtension == ".jpeg" ||
                 urlExtension == ".gif" || urlExtension == ".svg" || urlExtension == ".webp"))
            {
                return urlExtension;
            }

            // Fallback based on Cloudinary resource type
            if (url.Contains("/video/upload/"))
            {
                // Could be video or audio, need to check context or assume based on usage
                if (url.Contains(".mp3") || url.Contains(".wav") || url.Contains(".ogg"))
                    return ".mp3"; // Default audio extension
                return ".mp4"; // Default video extension
            }
            if (url.Contains("/image/upload/")) return ".png";
            if (url.Contains("/raw/upload/")) return ".pdf";

            return ".png"; // Default fallback
        }

        public static bool IsValidImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            return url.Contains("cloudinary.com") ||
                   url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                   url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
        }


        public static async Task<string> ReplaceUrlsWithLocalPaths(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent)) return htmlContent;

            // Replace image URLs
            var imgRegex = new Regex(@"<img([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = imgRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidImageUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var localPath = $"../assets/images/{fileName}";
                    return $"<img{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            // Replace video URLs
            var videoRegex = new Regex(@"<video([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = videoRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidVideoUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var localPath = $"../assets/video/{fileName}";
                    return $"<video{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            // Replace source URLs in video/audio tags
            var sourceRegex = new Regex(@"<source([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = sourceRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidVideoUrl(url) || IsValidAudioUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var folder = IsValidVideoUrl(url) ? "video" : "audio";
                    var localPath = $"../assets/{folder}/{fileName}";
                    return $"<source{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            return htmlContent;
        }

        public static string GetAssetFolder(string fileUrl)
        {
            var extension = Path.GetExtension(fileUrl).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "images",
                ".mp3" or ".wav" or ".ogg" or ".m4a" => "audio",
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" => "video",
                ".pdf" or ".doc" or ".docx" or ".ppt" or ".pptx" => "documents",
                ".ttf" or ".otf" or ".woff" or ".woff2" => "fonts",
                _ => "documents"
            };
        }

        public static string GetAssetType(string fileUrl)
        {
            var extension = Path.GetExtension(fileUrl).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "image",
                ".mp3" or ".wav" or ".ogg" or ".m4a" => "audio",
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".flv" => "video",
                ".ttf" or ".otf" or ".woff" or ".woff2" => "font",
                _ => "document"
            };
        }

        public static string GetMimeType(string fileUrl)
        {
            var extension = Path.GetExtension(fileUrl).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".mp4" => "video/mp4",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        public static async Task<long> GetFileSizeAsync(string fileUrl)
        {
            try
            {
                // If it's a local file path
                if (File.Exists(fileUrl))
                {
                    return new FileInfo(fileUrl).Length;
                }

                // If it's a remote URL (e.g., Cloudinary)
                if (Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute) &&
                    (fileUrl.StartsWith("http://") || fileUrl.StartsWith("https://")))
                {
                    using var httpClient = new HttpClient();
                    using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, fileUrl));
                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Content.Headers.ContentLength.HasValue)
                        {
                            return response.Content.Headers.ContentLength.Value;
                        }
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public static async Task CopyFileToArchiveAsync(ZipArchive archive, string sourceUrl, string targetPath)
        {
            var entry = archive.CreateEntry(targetPath);
            using var entryStream = entry.Open();

            if (File.Exists(sourceUrl))
            {
                // Local file
                using var fileStream = File.OpenRead(sourceUrl);
                await fileStream.CopyToAsync(entryStream);
            }
            else
            {
                // Remote file
                await CopyRemoteFileToStreamAsync(sourceUrl, entryStream);
            }
        }

        public static async Task CopyRemoteFileToStreamAsync(string sourceUrl, Stream entryStream)
        {
            string safeUrl = NormalizeUrl(sourceUrl);

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(new Uri(safeUrl));
            response.EnsureSuccessStatusCode();

            using var remoteStream = await response.Content.ReadAsStreamAsync();
            await remoteStream.CopyToAsync(entryStream);
        }

        private static string NormalizeUrl(string sourceUrl)
        {
            // Already valid absolute?
            if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                // Check if URL needs encoding (has spaces or non-ASCII characters)
                if (sourceUrl.Contains(" ") || sourceUrl.Any(c => c > 127))
                {
                    // Encode the path and query parts
                    var uriBuilder = new UriBuilder(uri);
                    var pathAndQuery = uri.PathAndQuery;
                    
                    // Split path and query
                    var parts = pathAndQuery.Split('?');
                    var path = parts[0];
                    var query = parts.Length > 1 ? parts[1] : string.Empty;
                    
                    // Encode path segments
                    var segments = path.Split('/');
                    var encodedSegments = segments.Select(s => Uri.EscapeDataString(s)).ToArray();
                    var encodedPath = string.Join("/", encodedSegments);
                    
                    // Rebuild URL
                    uriBuilder.Path = encodedPath;
                    if (!string.IsNullOrEmpty(query))
                    {
                        uriBuilder.Query = query;
                    }
                    
                    return uriBuilder.Uri.AbsoluteUri;
                }
                
                return uri.AbsoluteUri;
            }

            // Try unescape → rebuild
            try
            {
                var unescaped = Uri.UnescapeDataString(sourceUrl);
                if (Uri.TryCreate(unescaped, UriKind.Absolute, out uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    return uri.AbsoluteUri;
                }
            }
            catch
            {
                // ignore bad escapes
            }

            // Last fallback: only replace spaces
            var replaced = sourceUrl.Replace(" ", "%20");
            if (Uri.TryCreate(replaced, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }

            throw new ArgumentException($"Invalid URL: {sourceUrl}", nameof(sourceUrl));
        }

        public static async Task<string> ReplaceUrlsWithLocalPathsForCourse(string htmlContent, bool isInCourse = false)
        {
            if (string.IsNullOrEmpty(htmlContent)) return htmlContent;

            var assetPrefix = isInCourse ? "../../../assets" : "../assets";

            // Replace image URLs
            var imgRegex = new Regex(@"<img([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = imgRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidImageUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var localPath = $"{assetPrefix}/images/{fileName}";
                    return $"<img{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            // Replace video URLs
            var videoRegex = new Regex(@"<video([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = videoRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidVideoUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var localPath = $"{assetPrefix}/video/{fileName}";
                    return $"<video{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            // Replace source URLs in video/audio tags
            var sourceRegex = new Regex(@"<source([^>]+)src=[""']([^""']+)[""']([^>]*>)", RegexOptions.IgnoreCase);
            htmlContent = sourceRegex.Replace(htmlContent, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                if (IsValidVideoUrl(url) || IsValidAudioUrl(url))
                {
                    var fileName = GetFileNameFromCloudinaryUrl(url);
                    var folder = IsValidVideoUrl(url) ? "video" : "audio";
                    var localPath = $"{assetPrefix}/{folder}/{fileName}";
                    return $"<source{beforeSrc}src=\"{localPath}\"{afterSrc}";
                }

                return match.Value;
            });

            return htmlContent;
        }
    }
}
