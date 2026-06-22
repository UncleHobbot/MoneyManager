using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services.Import;

/// <summary>
/// CIBC (Canadian Imperial Bank of Commerce) CSV importer. CIBC files have
/// no header row; columns are positional (Date=0, Description=1,
/// AmountDebit=2, AmountCredit=3, AccountNumber=4). Debit is determined by
/// <c>AmountDebit.HasValue</c>. Fuzzy date matching (±5 days) because CIBC
/// posting dates can drift from transaction dates.
/// </summary>
public sealed class CibcImporter : IBankImporter
{
    public string BankType => "CIBC";
    public bool ApplyRules => true;
    public bool UseFuzzyDateMatch => true;

    public void Validate(Stream stream)
    {
        // Read up to 5 lines from the stream, then attempt to parse them
        // as CIBC records. CIBC has no header row, so we look for at least
        // one record with a non-default Date, a non-empty Description, and
        // a non-empty AccountNumber. If nothing parses, the file is not
        // a CIBC export.
        using var lineReader = new StreamReader(stream, leaveOpen: true);
        var lines = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var line = lineReader.ReadLine();
            if (line == null) break;
            lines.Add(line);
        }
        stream.Position = 0;

        if (lines.Count == 0)
            throw new InvalidOperationException("CIBC CSV file is empty.");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        try
        {
            using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", lines))));
            using var csv = new CsvReader(reader, config);

            var record = new CIBCCSV();
            var records = csv.EnumerateRecords(record);
            var hasValidRecord = false;

            foreach (var r in records)
            {
                if (r.Date != default
                    && !string.IsNullOrWhiteSpace(r.Description)
                    && !string.IsNullOrWhiteSpace(r.AccountNumber))
                {
                    hasValidRecord = true;
                    break;
                }
            }

            if (!hasValidRecord)
                throw new InvalidOperationException(
                    "CIBC CSV file does not have the expected structure. " +
                    "Unable to find valid records with Date, Description, and Account Number.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "CIBC CSV file does not have the expected structure. " +
                "Expected format: Date (index 0), Description (index 1), " +
                "AmountDebit (index 2), AmountCredit (index 3), AccountNumber (index 4).", ex);
        }
    }

    public IEnumerable<NormalizedRow> ReadRows(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        var record = new CIBCCSV();
        foreach (var r in csv.EnumerateRecords(record))
        {
            var isDebit = r.AmountDebit.HasValue;
            var amount = r.AmountDebit ?? r.AmountCredit ?? 0;

            yield return new NormalizedRow(
                Date: r.Date,
                Amount: amount,
                IsDebit: isDebit,
                Description: r.Description,
                OriginalDescription: r.Description,
                AccountName: r.AccountNumber,
                CategoryHint: null);
        }
    }
}
