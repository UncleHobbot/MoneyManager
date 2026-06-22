using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services.Import;

/// <summary>
/// A bank-specific CSV importer. Encapsulates everything that varies per
/// bank format (CSV schema, sign convention, description composition,
/// category source, rule-application policy, fuzzy-date matching) behind
/// two operations: <see cref="Validate"/> the stream structure, and
/// <see cref="ReadRows"/> yield normalized rows.
/// </summary>
/// <remarks>
/// Adapters are pure: they read the stream and produce <see cref="NormalizedRow"/>
/// values; they do not touch the database, the cache, or rule application.
/// The import pipeline (<c>TransactionService.ImportAsync</c>) owns backup,
/// account/category resolution, dedup, rule application, and persistence.
/// See <c>CONTEXT.md</c> ("Bank Import Adapter") and Candidate 4 grilling.
/// </remarks>
public interface IBankImporter
{
    /// <summary>
    /// The wire-format identifier used in <see cref="ImportResult.BankType"/>
    /// and in archive filenames. Today: "Mint", "RBC", "CIBC".
    /// </summary>
    string BankType { get; }

    /// <summary>
    /// If <c>true</c>, the pipeline applies auto-categorization rules to
    /// each imported transaction. RBC and CIBC enable this (their CSVs have
    /// no category column); Mint disables it (its CSV provides categories).
    /// </summary>
    bool ApplyRules { get; }

    /// <summary>
    /// If <c>true</c>, dedup allows a ±5 day window for date matching.
    /// CIBC enables this (posting dates can drift); Mint and RBC disable it.
    /// </summary>
    bool UseFuzzyDateMatch { get; }

    /// <summary>
    /// If <c>true</c>, the CSV has a header row that the pipeline should
    /// subtract from the line count when computing <see cref="ImportResult.TotalCount"/>.
    /// Mint and RBC have headers; CIBC does not.
    /// </summary>
    bool HasHeaderRecord { get; }

    /// <summary>
    /// Validates that <paramref name="stream"/> has the expected structure
    /// for this bank's CSV format. Throws <see cref="InvalidOperationException"/>
    /// with a human-readable message if the structure is wrong. Resets the
    /// stream position to 0 on return.
    /// </summary>
    void Validate(Stream stream);

    /// <summary>
    /// Enumerates the data rows of <paramref name="stream"/> as
    /// <see cref="NormalizedRow"/> values. Rows with missing required
    /// fields (e.g. RBC rows with null <c>CAD$</c>) are silently skipped
    /// here; the pipeline's <c>TotalCount - ImportedCount</c> arithmetic
    /// accounts for them in the user-facing skip count.
    /// </summary>
    IEnumerable<NormalizedRow> ReadRows(Stream stream);
}

/// <summary>
/// A bank CSV row projected into a bank-agnostic shape. The import pipeline
/// consumes <see cref="NormalizedRow"/> values; it never sees the raw
/// <c>MintCSV</c>/<c>RBCCSV</c>/<c>CIBCCSV</c> types.
/// </summary>
/// <param name="Date">The transaction date.</param>
/// <param name="Amount">Always positive; direction is <paramref name="IsDebit"/>.</param>
/// <param name="IsDebit"><c>true</c> for expenses (debits), <c>false</c> for income (credits).</param>
/// <param name="Description">The display description.</param>
/// <param name="OriginalDescription">The raw description used for dedup and rule matching.</param>
/// <param name="AccountName">The account name/number to resolve via <c>GetAccountAsync</c>.</param>
/// <param name="CategoryHint">
/// Optional category name to resolve via <c>GetCategoryAsync</c>. If <c>null</c>,
/// the pipeline assigns the default "Uncategorized" category.
/// </param>
public sealed record NormalizedRow(
    DateTime Date,
    decimal Amount,
    bool IsDebit,
    string Description,
    string OriginalDescription,
    string AccountName,
    string? CategoryHint);
