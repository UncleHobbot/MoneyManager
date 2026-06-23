using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MoneyManager.Api.Endpoints;
using MoneyManager.Api.Services.Import;
using NSubstitute;

namespace MoneyManager.Api.Tests.Endpoints;

/// <summary>
/// Tests for <see cref="ImportEndpoints.DetectBankType"/>. The frontend's
/// default upload mode is "Auto" (<c>bankType=Auto</c>), which falls through
/// <c>ParseBankType</c> and lands here. A regression that rejects real CIBC
/// files (whose first data row contains text descriptions) breaks every CIBC
/// import that doesn't explicitly pick the bank from the dropdown.
/// </summary>
public class BankDetectionTests
{
    private static IFormFile FormFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var formFile = Substitute.For<IFormFile>();
        formFile.OpenReadStream().Returns(new MemoryStream(bytes));
        return formFile;
    }

    [Fact]
    public void DetectCibc_RealExportFirstLine_WithQuotedDescription()
    {
        // Regression: first line of "cibc black 2026-06-14.csv". The quoted
        // description contains letters, which used to trip the heuristic
        // "CIBC first line has no letters". CIBC rows always contain text.
        var firstLine = """2026-06-08,"COSTCO WHOLESALE W521 BROSSARD, QC",152.06,,5268********5605""";
        var importer = ImportEndpoints.DetectBankType(FormFile(firstLine));

        importer.Should().BeOfType<CibcImporter>();
    }

    [Fact]
    public void DetectCibc_PaymentRow_WithLettersAndSlash()
    {
        // Another real CIBC line shape: unquoted description with a slash.
        var firstLine = """2026-05-19,PAYMENT THANK YOU/PAIEMEN T MERCI,,524.02,5268********4825""";

        ImportEndpoints.DetectBankType(FormFile(firstLine))
            .Should().BeOfType<CibcImporter>();
    }

    [Fact]
    public void DetectMint_HeaderSignature()
    {
        var firstLine = """Date,Description,Original Description,Transaction Type,Category,Amount,Currency""";

        ImportEndpoints.DetectBankType(FormFile(firstLine))
            .Should().BeOfType<MintImporter>();
    }

    [Fact]
    public void DetectRbc_HeaderSignature()
    {
        var firstLine = """Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$""";

        ImportEndpoints.DetectBankType(FormFile(firstLine))
            .Should().BeOfType<RbcImporter>();
    }

    [Fact]
    public void Detect_EmptyOrWhitespaceFirstLine_ReturnsNull()
    {
        ImportEndpoints.DetectBankType(FormFile("")).Should().BeNull();
        ImportEndpoints.DetectBankType(FormFile("   \n")).Should().BeNull();
    }

    [Fact]
    public void Detect_GarbageThatIsNotMintOrRbc_FallsThroughToCibc()
    {
        // Anything without Mint/RBC header keywords is treated as CIBC and
        // handed to CibcImporter.Validate, which rejects genuine garbage
        // with a clear "expected structure" error.
        ImportEndpoints.DetectBankType(FormFile("garbage,not,a,bank,format"))
            .Should().BeOfType<CibcImporter>();
    }
}
