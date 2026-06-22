using System.Text;
using FluentAssertions;
using MoneyManager.Api.Services.Import;

namespace MoneyManager.Api.Tests.Services.Import;

public class RbcImporterTests
{
    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    private const string ValidHeader = """
        "Account Type","Account Number","Transaction Date","Cheque Number","Description 1","Description 2","CAD$","USD$"
        """;

    [Fact]
    public void Properties_MatchRbcSemantics()
    {
        var importer = new RbcImporter();

        importer.BankType.Should().Be("RBC");
        importer.ApplyRules.Should().BeTrue("RBC CSV has no category column - rules must assign one");
        importer.UseFuzzyDateMatch.Should().BeFalse();
    }

    [Fact]
    public void Validate_AcceptsValidHeader_AndResetsPosition()
    {
        var importer = new RbcImporter();
        using var stream = ToStream(ValidHeader + "\n\"Chequing\",\"12345\",\"1/15/2025\",\"\",\"LOBLAWS\",\"\",\"-85.50\",\"\"");

        var act = () => importer.Validate(stream);

        act.Should().NotThrow();
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Validate_ThrowsOnEmptyStream()
    {
        var importer = new RbcImporter();
        using var stream = ToStream("");

        var act = () => importer.Validate(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Validate_ThrowsOnMissingColumn()
    {
        var importer = new RbcImporter();
        // Header missing "CAD$" and "USD$"
        var badHeader = """
            "Account Type","Account Number","Transaction Date","Cheque Number","Description 1","Description 2"
            """;
        using var stream = ToStream(badHeader);

        var act = () => importer.Validate(stream);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CAD$*");
    }

    [Fact]
    public void ReadRows_NegativeAmount_IsDebitWithPositiveAmount()
    {
        var csv = ValidHeader + "\n" + """
            "Chequing","12345","1/15/2025","","LOBLAWS","#1234","-85.50",""
            """;

        var importer = new RbcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().ContainSingle();
        var row = rows[0];
        row.IsDebit.Should().BeTrue();
        row.Amount.Should().Be(85.50m);
    }

    [Fact]
    public void ReadRows_PositiveAmount_IsCredit()
    {
        var csv = ValidHeader + "\n" + """
            "Chequing","12345","1/20/2025","","PAYROLL","DEPOSIT","3000.00",""
            """;

        var importer = new RbcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().ContainSingle();
        var row = rows[0];
        row.IsDebit.Should().BeFalse();
        row.Amount.Should().Be(3000.00m);
    }

    [Fact]
    public void ReadRows_ConcatenatesDescription1And2_WithSpace_AndOuterTrims()
    {
        // The adapter concatenates Description1 + " " + Description2 and
        // outer-trims the result. Inner whitespace from the source columns
        // is preserved (matches pre-migration behavior - the original code
        // did not normalize inner whitespace either).
        var csv = ValidHeader + "\n" + """
            "Chequing","12345","1/15/2025","","  LOBLAWS "," #1234 ","-85.50",""
            """;

        var importer = new RbcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        // "  LOBLAWS " + " " + " #1234 " = "  LOBLAWS   #1234 "
        // Outer-trim removes leading/trailing whitespace but preserves inner.
        rows.Single().Description.Should().Be("LOBLAWS   #1234");
        rows.Single().OriginalDescription.Should().Be("LOBLAWS   #1234");
    }

    [Fact]
    public void ReadRows_SkipsRowsWithNullAmount()
    {
        // RBC exports sometimes have rows with null CAD$ (e.g. USD-only).
        // The adapter silently skips them; the pipeline's
        // SkippedCount = TotalCount - ImportedCount arithmetic accounts for them.
        var csv = ValidHeader + "\n" + """
            "Chequing","12345","1/15/2025","","LOBLAWS","","-85.50",""
            "Chequing","12345","1/16/2025","","USD ONLY","","","50.00"
            "Chequing","12345","1/17/2025","","PAYROLL","","3000.00",""
            """;

        var importer = new RbcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Should().HaveCount(2);
        rows.Should().NotContain(r => r.Description == "USD ONLY");
    }

    [Fact]
    public void ReadRows_CategoryHint_IsNull()
    {
        // RBC CSV has no category column; the pipeline assigns "Uncategorized"
        // and applies rules. CategoryHint must be null so the pipeline knows
        // to use the default.
        var csv = ValidHeader + "\n" + """
            "Chequing","12345","1/15/2025","","LOBLAWS","","-85.50",""
            """;

        var importer = new RbcImporter();
        using var stream = ToStream(csv);

        var rows = importer.ReadRows(stream).ToList();

        rows.Single().CategoryHint.Should().BeNull();
    }
}
