using System.Text;
using FluentAssertions;
using MoneyManager.Api.Services.Import;

namespace MoneyManager.Api.Tests.Services.Import;

public class MintImporterTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Properties_MatchMintSemantics()
    {
        var importer = new MintImporter();

        importer.BankType.Should().Be("Mint");
        importer.ApplyRules.Should().BeFalse("Mint CSV provides categories directly");
        importer.UseFuzzyDateMatch.Should().BeFalse("Mint transaction dates are exact");
    }

    [Fact]
    public void Validate_DoesNotThrow_AndResetsPosition()
    {
        // Mint has no validator today (tracked as debt in CONTEXT.md).
        // The contract still requires Validate to leave the stream at position 0.
        var importer = new MintImporter();
        using var stream = ToStream("anything");

        var act = () => importer.Validate(stream);

        act.Should().NotThrow();
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void ReadRows_ParsesValidCsv_WithExpectedFieldMapping()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee Shop,STARBUCKS #1234,5.50,debit,Food,Chequing,,
            1/16/2025,Salary,PAYROLL,3000.00,credit,Income,Chequing,,
            """;

        var importer = new MintImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().HaveCount(2);

        var debit = rows[0];
        debit.Date.Should().Be(new DateTime(2025, 1, 15));
        debit.Amount.Should().Be(5.50m);
        debit.IsDebit.Should().BeTrue();
        debit.Description.Should().Be("Coffee Shop");
        debit.OriginalDescription.Should().Be("STARBUCKS #1234");
        debit.AccountName.Should().Be("Chequing");
        debit.CategoryHint.Should().Be("Food");

        var credit = rows[1];
        credit.Amount.Should().Be(3000.00m);
        credit.IsDebit.Should().BeFalse();
        credit.CategoryHint.Should().Be("Income");
    }

    [Fact]
    public void ReadRows_TreatsUnknownTransactionType_AsCredit()
    {
        // TransactionType == "debit" is the only debit marker. Anything else
        // (including "transfer" or typos) falls through to IsDebit = false.
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Mystery,MYSTERY,10.00,transfer,Food,Chequing,,
            """;

        var importer = new MintImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Single().IsDebit.Should().BeFalse();
    }

    [Fact]
    public void ReadRows_NullDescriptionFields_BecomeEmptyString()
    {
        // MintCSV allows null for Description and OriginalDescription.
        // The adapter normalizes to string.Empty to satisfy non-nullable
        // downstream contracts (Transaction.Description).
        // Column order: Date, Description, Original Description, Amount,
        // Transaction Type, Category, Account Name, Labels, Notes.
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,,,5.00,debit,Food,Chequing,,
            """;

        var importer = new MintImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Single().Description.Should().BeEmpty();
        rows.Single().OriginalDescription.Should().BeEmpty();
        rows.Single().AccountName.Should().Be("Chequing");
    }
}
