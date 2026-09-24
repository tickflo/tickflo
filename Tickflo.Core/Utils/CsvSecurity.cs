namespace Tickflo.Core.Utils;

/// <summary>
/// Neutralizes spreadsheet formula injection ("CSV injection") in CSV exports and
/// report downloads. Any cell that begins with a formula trigger character (=, +,
/// -, @, tab, carriage return) is prefixed with a single quote so spreadsheet apps
/// (Excel, LibreOffice, Google Sheets) treat it as text instead of evaluating it as
/// a formula (e.g. =HYPERLINK(...), =cmd|'...'!A0 DDE payloads) or fetching a URL.
/// </summary>
public static class CsvSecurity
{
    private const string FormulaTriggerPrefixes = "=+-@\t\r";

    public static string SanitizeCell(string value)
    {
        if (value.Length > 0 && FormulaTriggerPrefixes.Contains(value[0]))
        {
            return "'" + value;
        }

        return value;
    }
}
