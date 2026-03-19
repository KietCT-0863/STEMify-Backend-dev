using Contracts.Abstractions.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Abstractions.Services.PdfLayer
{
    public class PdfService(HttpClient httpClient, IConfiguration configuration) : IPdfService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = configuration["Pdflayer:ApiKey"] ?? throw new ArgumentNullException("Pdflayer:ApiKey");

        public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent)
        {
            try
            {
                // All parameters directly in URL for highest quality
                var apiUrl = $"https://api.pdflayer.com/api/convert?" +
                    $"access_key={_apiKey}" +
                    $"&page_size=A4" +
                    $"&orientation=landscape" +
                    $"&margin_top=0" +
                    $"&margin_bottom=0" +
                    $"&margin_left=0" +
                    $"&margin_right=0" +
                    $"&dpi=300" +
                    $"&image_quality=100" +
                    $"&use_print_media=0" +  // Changed to 0 - use screen media for better positioning
                    $"&no_images=0" +
                    $"&grayscale=0" +
                    $"&delay=2000" +  // Increased delay for image loading
                    $"&javascript=1" +
                    $"&inline=0" +
                    $"&force=1" +
                    $"&page_width=900" +  // Custom width matching certificate
                    $"&page_height=650" +  // Custom height matching certificate
                    $"&viewport=900x650";  // Viewport matching exact certificate size

                using var content = new MultipartFormDataContent
                {
                    { new StringContent(htmlContent), "document_html" }
                };

                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to convert HTML to PDF. Status: {response.StatusCode}, Message: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while converting HTML to PDF: " + ex.Message, ex);
            }
        }
    }
}