namespace Tickflo.Core.Utils;

using System.Text.Encodings.Web;

/// <summary>
/// Replaces {{placeholder}} tokens in email templates. Values that are substituted
/// into the HTML email body are HTML-encoded first so user-controlled content
/// (ticket subjects, contact names, comments) cannot inject markup or phishing HTML
/// into recipients' HTML email clients. Subject lines are plain text headers and
/// intentionally left unencoded.
/// </summary>
public static class EmailTemplateRenderer
{
    public static string ReplaceVariables(string template, Dictionary<string, string>? variables, bool htmlEncode)
    {
        if (variables == null)
        {
            return template;
        }

        var result = template;
        foreach (var kvp in variables)
        {
            var placeholder = $"{{{{{kvp.Key}}}}}";
            var value = htmlEncode ? HtmlEncoder.Default.Encode(kvp.Value) : kvp.Value;
            result = result.Replace(placeholder, value);
        }

        return result;
    }
}
