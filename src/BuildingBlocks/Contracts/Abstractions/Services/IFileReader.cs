namespace Contracts.Abstractions.Services
{
    public interface IFileReader
    {
        Task<string> ReadFileAsync(string filePath);
    }
}
