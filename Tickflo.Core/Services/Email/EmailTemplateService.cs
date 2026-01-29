namespace Tickflo.Core.Services.Email;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IEmailTemplateService
{
    public Task<(string subject, string body)> RenderTemplateAsync(EmailTemplateType templateType, Dictionary<string, string> variables, int? workspaceId = null);
}


public class EmailTemplateService(TickfloDbContext dbContext) : IEmailTemplateService
{
    #region Constants
    private static readonly CompositeFormat TemplateNotFoundErrorFormat = CompositeFormat.Parse("Email template with type ID {0} not found.");
    #endregion

    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<(string subject, string body)> RenderTemplateAsync(
        EmailTemplateType templateType,
        Dictionary<string, string> variables,
        int? workspaceId = null)
    {
        var template = await this.GetTemplateOrThrowAsync(templateType);

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return (subject, body);
    }

    private async Task<EmailTemplate> GetTemplateOrThrowAsync(EmailTemplateType templateType)
    {
        var template = await this.dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateTypeId == (int)templateType)
            ?? throw new InvalidOperationException(string.Format(null, TemplateNotFoundErrorFormat, (int)templateType));

        return template;
    }

    private static string ReplaceVariables(string text, Dictionary<string, string> variables)
    {
        var result = text;
        foreach (var kvp in variables)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            result = result.Replace(placeholder, kvp.Value);
        }
        return result;
    }
}
