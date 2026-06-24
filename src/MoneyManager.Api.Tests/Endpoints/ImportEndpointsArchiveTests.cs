using FluentAssertions;
using MoneyManager.Api.Endpoints;

namespace MoneyManager.Api.Tests.Endpoints;

/// <summary>
/// Tests for the archive-filename sanitization used when copying an uploaded CSV
/// into the imported-files archive. The original (client-supplied) name is
/// embedded in the archive filename, so it must be stripped of path separators
/// and other characters illegal in a file name.
/// </summary>
public class ImportEndpointsArchiveTests
{
    [Theory]
    [InlineData("statement", "statement")]
    [InlineData("jan-2026", "jan-2026")]
    public void SanitizeForFileName_KeepsSafeNames(string input, string expected)
    {
        ImportEndpoints.SanitizeForFileName(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeForFileName_FallsBackWhenEmpty(string? input)
    {
        ImportEndpoints.SanitizeForFileName(input).Should().Be("import");
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("a/b\\c")]
    [InlineData("name:with*illegal?chars")]
    public void SanitizeForFileName_StripsPathAndIllegalChars(string input)
    {
        var result = ImportEndpoints.SanitizeForFileName(input);

        // No separators or other illegal characters survive, so the result is a
        // single path segment that cannot traverse out of the archive directory.
        result.Should().NotContainAny("/", "\\");
        result.ToCharArray().Should().NotIntersectWith(Path.GetInvalidFileNameChars());
        Path.GetFileName(result).Should().Be(result);
    }
}
