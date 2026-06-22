using System.Globalization;
using CsvHelper;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services.Import;

/// <summary>
/// Mint.com CSV importer. Mint files have a header row and a "Transaction Type"
/// column that distinguishes debits from credits. Categories are provided per
/// row. No fuzzy date matching; rules are not auto-applied (Mint gives us
/// categories directly).
/// </summary>
public sealed class MintImporter : IBankImporter
{
    public string BankType => "Mint";
    public bool ApplyRules => false;
    public bool UseFuzzyDateMatch => false;
    public bool HasHeaderRecord => true;

    public void Validate(Stream stream)
    {
        // Mint has historically been the only format without a validator.
        // Malformed Mint CSVs fail inside CsvHelper with an opaque parse
        // error. Adding a validator is a separate cleanup; the interface
        // requires the method to exist.
        stream.Position = 0;
    }

    public IEnumerable<NormalizedRow> ReadRows(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var record = new MintCSV();
        foreach (var r in csv.EnumerateRecords(record))
        {
            var isDebit = r.TransactionType == "debit";
            yield return new NormalizedRow(
                Date: r.Date,
                Amount: r.Amount,
                IsDebit: isDebit,
                Description: r.Description ?? string.Empty,
                OriginalDescription: r.OriginalDescription ?? string.Empty,
                AccountName: r.AccountName ?? string.Empty,
                CategoryHint: r.Category);
        }
    }
}
