using System.Text;
using FluentAssertions;
using MoneyManager.Api.Services.Import;

namespace MoneyManager.Api.Tests.Services.Import;

public class CibcImporterTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void Properties_MatchCibcSemantics()
    {
        var importer = new CibcImporter();

        importer.BankType.Should().Be("CIBC");
        importer.ApplyRules.Should().BeTrue("CIBC CSV has no category column");
        importer.UseFuzzyDateMatch.Should().BeTrue("CIBC posting dates can drift from transaction dates");
    }

    [Fact]
    public void Validate_AcceptsValidRecords_AndResetsPosition()
    {
        // CIBC has no header row. Columns by index: Date(0), Description(1),
        // AmountDebit(2), AmountCredit(3), AccountNumber(4).
        var csv = """
            2025-01-15,LOBLAWS #1234,85.50,,12345
            2025-01-20,PAYROLL DEPOSIT,,3000.00,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var act = () => importer.Validate(stream);

        act.Should().NotThrow();
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Validate_ThrowsOnEmptyStream()
    {
        var importer = new CibcImporter();
        using var stream = ToStream("");

        var act = () => importer.Validate(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Validate_ThrowsOnNoValidRecords()
    {
        // Five lines of garbage - no parseable CIBC records.
        var csv = """
            garbage,garbage,garbage,garbage,garbage
            more,garbage,garbage,garbage,garbage
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var act = () => importer.Validate(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*expected structure*");
    }

    [Fact]
    public void ReadRows_AmountDebit_IsDebitTrue()
    {
        var csv = """
            2025-01-15,LOBLAWS #1234,85.50,,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().ContainSingle();
        var row = rows[0];
        row.IsDebit.Should().BeTrue();
        row.Amount.Should().Be(85.50m);
    }

    [Fact]
    public void ReadRows_AmountCredit_IsDebitFalse()
    {
        var csv = """
            2025-01-20,PAYROLL DEPOSIT,,3000.00,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().ContainSingle();
        var row = rows[0];
        row.IsDebit.Should().BeFalse();
        row.Amount.Should().Be(3000.00m);
    }

    [Fact]
    public void ReadRows_BothAmountsNull_YieldsAmountZero()
    {
        // CIBC files sometimes have rows with both debit and credit null
        // (e.g. informational lines). The adapter yields them with Amount=0;
        // the pipeline's dedup/save handles them as zero-amount transactions.
        var csv = """
            2025-01-15,INFO LINE,,,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().ContainSingle();
        rows[0].Amount.Should().Be(0m);
        rows[0].IsDebit.Should().BeFalse();
    }

    [Fact]
    public void ReadRows_DescriptionUsedForBothDescriptionAndOriginal()
    {
        // CIBC has a single description column. The adapter copies it into
        // both Description and OriginalDescription fields of NormalizedRow.
        var csv = """
            2025-01-15,LOBLAWS #1234,85.50,,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Single().Description.Should().Be("LOBLAWS #1234");
        rows.Single().OriginalDescription.Should().Be("LOBLAWS #1234");
    }

    [Fact]
    public void ReadRows_CategoryHint_IsNull()
    {
        var csv = """
            2025-01-15,LOBLAWS #1234,85.50,,12345
            """;

        var importer = new CibcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Single().CategoryHint.Should().BeNull();
    }
}
