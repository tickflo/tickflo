namespace Tickflo.Core.Services.Admin;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Utils;

public class EmailLogEntry
{
    public int Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IEmailLogService
{
    public Task<(int total, List<EmailLogEntry>? emails)> GetEmailsAsync(int pageNumber = 1, int pageSize = 50);
}

public class EmailLogService(TickfloDbContext db) : IEmailLogService
{
    private readonly TickfloDbContext db = db;
    private List<EmailTemplate>? templates;

    public async Task<(int total, List<EmailLogEntry>? emails)> GetEmailsAsync(int pageNumber = 1, int pageSize = 50)
    {
        var total = await this.db.Emails.CountAsync();
        if (total == 0)
        {
            return (0, null);
        }

        this.templates = await this.db.EmailTemplates.ToListAsync();
        if (this.templates == null || this.templates.Count == 0)
        {
            return (total, null);
        }

        var emails = await this.db.Emails.OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (total, emails.Select(e => new EmailLogEntry
        {
            Id = e.Id,
            To = e.To,
            From = e.From,
            Subject = this.RenderSubject(e.TemplateId, e.Vars),
            Body = this.RenderBody(e.TemplateId, e.Vars),
            CreatedAt = e.CreatedAt,
        }).ToList());
    }

    private string RenderSubject(int templateId, Dictionary<string, string>? vars)
    {
        if (vars == null)
        {
            return string.Empty;
        }

        var template = this.templates?.FirstOrDefault(t => t.Id == templateId);
        if (template == null)
        {
            return string.Empty;
        }

        return EmailTemplateRenderer.ReplaceVariables(template.Subject, vars, htmlEncode: false);
    }

    private string RenderBody(int templateId, Dictionary<string, string>? vars)
    {
        if (vars == null)
        {
            return string.Empty;
        }

        var template = this.templates?.FirstOrDefault(t => t.Id == templateId);
        if (template == null)
        {
            return string.Empty;
        }

        // Route through EmailTemplateRenderer with htmlEncode so user-controlled
        // variable values cannot inject markup/HTML into the rendered body (stored
        // XSS guard, same policy as outbound email rendering).
        return EmailTemplateRenderer.ReplaceVariables(template.Body, vars, htmlEncode: true);
    }
}
