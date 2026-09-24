namespace Tickflo.Core.Test.Services.Utils;

using Tickflo.Core.Utils;
using Xunit;

public class EmailTemplateRendererTests
{
    [Fact]
    public void ReplaceVariables_WhenHtmlEncode_EncodesMarkupValues()
    {
        // Arrange
        var vars = new Dictionary<string, string>
        {
            { "ticket_subject", "<img src=x onerror=alert(1)>" }
        };

        // Act
        var result = EmailTemplateRenderer.ReplaceVariables("Hi {{ticket_subject}}", vars, htmlEncode: true);

        // Assert — markup must not survive verbatim into an HTML email body
        Assert.DoesNotContain("<img", result);
        Assert.Contains("Hi &lt;img src=x onerror=alert(1)&gt;", result);
    }

    [Fact]
    public void ReplaceVariables_WhenNotHtmlEncode_LeavesValueUnchanged()
    {
        // Arrange
        var vars = new Dictionary<string, string>
        {
            { "ticket_subject", "A & B <C>" }
        };

        // Act — subject headers are plain text, not HTML, so must not be encoded
        var result = EmailTemplateRenderer.ReplaceVariables("Subj: {{ticket_subject}}", vars, htmlEncode: false);

        // Assert
        Assert.Equal("Subj: A & B <C>", result);
    }

    [Fact]
    public void ReplaceVariables_WhenVarsNull_ReturnsTemplateUnchanged() =>
        Assert.Equal("plain {{token}}", EmailTemplateRenderer.ReplaceVariables("plain {{token}}", null, htmlEncode: true));
}
