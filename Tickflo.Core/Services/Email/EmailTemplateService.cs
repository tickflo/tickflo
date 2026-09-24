namespace Tickflo.Core.Services.Email;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Utils;

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

        // The subject is a plain-text header (no HTML encoding); the body is sent to
        // Mailgun as HTML, so substituted variables are HTML-encoded to prevent stored
        // HTML injection into recipients' email clients.
        var subject = EmailTemplateRenderer.ReplaceVariables(template.Subject, variables, htmlEncode: false);
        var body = EmailTemplateRenderer.ReplaceVariables(template.Body, variables, htmlEncode: true);

        return (subject, body);
    }

    private async Task<EmailTemplate> GetTemplateOrThrowAsync(EmailTemplateType templateType)
    {
        var template = await this.dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateTypeId == (int)templateType)
            ?? throw new InvalidOperationException(string.Format(null, TemplateNotFoundErrorFormat, (int)templateType));

        return template;
    }
}
