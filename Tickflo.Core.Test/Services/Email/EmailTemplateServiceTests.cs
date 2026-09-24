namespace Tickflo.Core.Test.Services.Email;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Xunit;

public class EmailTemplateServiceTests
{
    private static TickfloDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TickfloDbContext(options);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMarkupInBodyVariable_HtmlEncodesBody_ButNotSubject()
    {
        // Arrange
        var db = CreateDbContext();
        db.EmailTemplates.Add(new EmailTemplate
        {
            TemplateTypeId = (int)EmailTemplateType.TicketComment,
            Subject = "New comment: {{ticket_subject}}",
            Body = "<p>Comment: {{ticket_subject}}</p>"
        });
        await db.SaveChangesAsync();

        var service = new EmailTemplateService(db);
        var markupSubject = "<img src=x onerror=alert(1)>";

        // Act
        var (subject, body) = await service.RenderTemplateAsync(
            EmailTemplateType.TicketComment,
            new Dictionary<string, string> { { "ticket_subject", markupSubject } });

        // Assert — subject is a plain-text header (kept verbatim); body is HTML (encoded)
        Assert.Equal($"New comment: {markupSubject}", subject);
        Assert.DoesNotContain(markupSubject, body);
        Assert.Contains("<p>Comment: &lt;img src=x onerror=alert(1)&gt;</p>", body);
    }
}
