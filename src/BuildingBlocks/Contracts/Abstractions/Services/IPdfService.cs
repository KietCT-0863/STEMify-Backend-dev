namespace Contracts.Abstractions.Services
{
    public interface IPdfService
    {
        Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent);
    }
}
