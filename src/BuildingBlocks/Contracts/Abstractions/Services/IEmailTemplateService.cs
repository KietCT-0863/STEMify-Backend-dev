namespace Contracts.Abstractions.Services;

/// <summary>
/// Interface for email template processing
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Process template with provided data
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="templateData">Data to populate template</param>
    /// <returns>Processed template content</returns>
    Task<string> ProcessTemplateAsync(string templateName, Dictionary<string, object> templateData);

    /// <summary>
    /// Check if template exists
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <returns>True if template exists</returns>
    bool TemplateExists(string templateName);

    /// <summary>
    /// Register a template
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="templateContent">Template content</param>
    void RegisterTemplate(string templateName, string templateContent);

    /// <summary>
    /// Load templates from directory
    /// </summary>
    /// <param name="templateDirectory">Directory containing templates</param>
    Task LoadTemplatesAsync(string templateDirectory);

    /// <summary>
    /// Get available template names
    /// </summary>
    /// <returns>List of available template names</returns>
    IEnumerable<string> GetAvailableTemplates();
}
