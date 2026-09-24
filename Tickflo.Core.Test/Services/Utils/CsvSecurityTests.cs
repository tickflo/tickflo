namespace Tickflo.Core.Test.Services.Utils;

using Tickflo.Core.Utils;
using Xunit;

public class CsvSecurityTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"http://evil\",\"click\")", "'=HYPERLINK(\"http://evil\",\"click\")")]
    [InlineData("+SUM(A1:A9)", "'+SUM(A1:A9)")]
    [InlineData("-2+3", "'-2+3")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("=cmd|'/C calc'!A0", "'=cmd|'/C calc'!A0")]
    [InlineData("\t=HYPERLINK(\"http://evil\",\"click\")", "'\t=HYPERLINK(\"http://evil\",\"click\")")]
    [InlineData("\r@SUM(A1)", "'\r@SUM(A1)")]
    public void SanitizeCell_WhenValueStartsWithFormulaTrigger_PrefixesSingleQuote(string input, string expected) =>
        Assert.Equal(expected, CsvSecurity.SanitizeCell(input));

    [Theory]
    [InlineData("Plain text")]
    [InlineData("123456")]
    [InlineData("")]
    [InlineData("Jane Doe")]
    [InlineData("john@example.com")]
    [InlineData("123 Main St")]
    public void SanitizeCell_WhenValueNotFormulaTrigger_ReturnsUnchanged(string input) =>
        Assert.Equal(input, CsvSecurity.SanitizeCell(input));
}
