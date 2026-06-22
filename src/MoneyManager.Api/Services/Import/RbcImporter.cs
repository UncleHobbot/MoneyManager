using System.Globalization;
using CsvHelper;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services.Import;

/// <summary>
/// RBC (Royal Bank of Canada) CSV importer. RBC files have a header row, a
/// "CAD$" column where negative amounts are debits, and two description
/// columns concatenated with a space. No category column - the pipeline
/// assigns "Uncategorized" and applies user rules.
/// </summary>
public sealed class RbcImporter : IBankImporter
{
    public string BankType => "RBC";
    public bool ApplyRules => true;
    public bool UseFuzzyDateMatch => false;

    private static readonly string[] ExpectedColumns =
        ["Account Type", "Account Number", "Transaction Date", "Cheque Number", "Description 1", "Description 2", "CAD$", "USD$"];

    public void Validate(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var headerLine = reader.ReadLine();
        stream.Position = 0;

        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("RBC CSV file is empty or missing header row.");

        var actualColumns = headerLine.Split(',').Select(c => c.Trim().Replace("\"", "")).ToArray();
        var missing = ExpectedColumns.Where(expected => !actualColumns.Contains(expected)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"RBC CSV file does not have the expected structure. Missing columns: {string.Join(", ", missing)}. " +
                $"Expected columns: {string.Join(", ", ExpectedColumns)}.");
    }

    public IEnumerable<NormalizedRow> ReadRows(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var record = new RBCCSV();
        foreach (var r in csv.EnumerateRecords(record))
        {
            // Rows without a CAD$ amount are silently skipped. The pipeline's
            // SkippedCount = TotalCount - ImportedCount arithmetic accounts for
            // them; we do not yield a marker row.
            if (r.AmountCAD == null)
                continue;

            var isDebit = r.AmountCAD < 0;
            var amount = Math.Abs(r.AmountCAD.Value);
            var description = $"{r.Description1} {r.Description2}".Trim();

            yield return new NormalizedRow(
                Date: r.Date,
                Amount: amount,
                IsDebit: isDebit,
                Description: description,
                OriginalDescription: description,
                AccountName: r.AccountNumber ?? string.Empty,
                CategoryHint: null);
        }
    }
}
