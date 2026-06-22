using FluentAssertions;
using MoneyManager.Api.Model.Query;

namespace MoneyManager.Api.Tests.Services;

public class ChartPeriodTests
{
    // All date math is verified against a fixed reference date so tests are
    // deterministic. Today's actual date does not affect these assertions.
    private static readonly DateTime ReferenceDate = new(2026, 6, 21); // June 21, 2026

    // ----------------------------------------------------------------
    // Registry behavior
    // ----------------------------------------------------------------

    [Fact]
    public void All_HasFourteenPeriods_IncludingW1Alias()
    {
        // 13 distinct labels in the legacy ChartEndpoints.Periods array,
        // plus the "w1" alias that GetDates accepted but Periods omitted.
        ChartPeriod.All.Should().HaveCount(14);
        ChartPeriod.All.Select(p => p.Code).Should().Contain(new[] { "12", "y1", "y2", "y3", "y12", "m1", "m2", "m1+2", "m1+3", "w", "w1", "w2", "w3", "a" });
    }

    [Fact]
    public void All_CodesAreUnique()
    {
        // Two periods share the label "Last 7 Days" (w + w1 alias), but the
        // codes must be unique so Find returns a single value.
        ChartPeriod.All.Select(p => p.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Find_ReturnsPeriod_ForKnownCode()
    {
        var period = ChartPeriod.Find("y2");

        period.Should().NotBeNull();
        period!.Code.Should().Be("y2");
        period.Label.Should().Be("Last Year");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Find_ReturnsNull_ForNullOrEmpty(string? code)
    {
        // Note: whitespace-only strings are treated as null per the
        // string.IsNullOrEmpty check. If whitespace should resolve to
        // Default like unknown codes, the check should switch to
        // IsNullOrWhiteSpace. Today both paths fall to Default at the
        // call site via `?? ChartPeriod.Default`.
        ChartPeriod.Find(code).Should().BeNull();
    }

    [Theory]
    [InlineData("13")]
    [InlineData("Y1")]        // case-sensitive: capital Y not recognized
    [InlineData("last-year")] // human-readable codes never supported
    [InlineData("M1")]        // case-sensitive
    public void Find_ReturnsNull_ForUnknownCode(string code)
    {
        ChartPeriod.Find(code).Should().BeNull();
    }

    [Fact]
    public void Default_IsThisYearPeriod()
    {
        // Default preserves pre-migration behavior of DataService.GetDates:
        // unknown codes silently fell to "Jan 1 this year -> first of next month",
        // which is exactly what the "y1" period produces.
        ChartPeriod.Default.Code.Should().Be("y1");
        ChartPeriod.Default.Label.Should().Be("This Year");
    }

    // ----------------------------------------------------------------
    // Date math — verified against ReferenceDate = 2026-06-21
    // ----------------------------------------------------------------

    [Fact]
    public void GetDateRange_Last12Months()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "12").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2025, 6, 1));   // first of 12 months ago
        end.Should().Be(new DateTime(2026, 7, 1));     // first of next month
    }

    [Fact]
    public void GetDateRange_ThisYear()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "y1").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 1, 1));
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_LastYear()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "y2").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2025, 1, 1));
        end.Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public void GetDateRange_TwoYearsAgo()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "y3").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2024, 1, 1));
        end.Should().Be(new DateTime(2025, 1, 1));
    }

    [Fact]
    public void GetDateRange_LastPlusThisYear()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "y12").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2025, 1, 1));
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_ThisMonth()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "m1").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 6, 1));
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_LastMonth()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "m2").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 5, 1));
        end.Should().Be(new DateTime(2026, 6, 1));
    }

    [Fact]
    public void GetDateRange_Last2Months()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "m1+2").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 5, 1));
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_Last3Months()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "m1+3").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 4, 1));
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_Last7Days_WCode()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "w").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 6, 14));  // 7 days before June 21
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_Last7Days_W1Alias_ProducesSameRangeAsW()
    {
        // "w1" is an alias for "w". The two must produce identical ranges
        // for any reference date.
        var wRange = ChartPeriod.All.Single(p => p.Code == "w").GetDateRange(ReferenceDate);
        var w1Range = ChartPeriod.All.Single(p => p.Code == "w1").GetDateRange(ReferenceDate);

        w1Range.Should().Be(wRange);
    }

    [Fact]
    public void GetDateRange_Last14Days()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "w2").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 6, 7));   // 14 days before June 21
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_Last31Days()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "w3").GetDateRange(ReferenceDate);
        start.Should().Be(new DateTime(2026, 5, 21));  // 31 days before June 21
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_AllTime()
    {
        var (start, end) = ChartPeriod.All.Single(p => p.Code == "a").GetDateRange(ReferenceDate);
        start.Should().Be(DateTime.MinValue);
        end.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void GetDateRange_DefaultPeriod_MatchesY1()
    {
        // ChartPeriod.Default is documented as equivalent to "y1".
        var defaultRange = ChartPeriod.Default.GetDateRange(ReferenceDate);
        var y1Range = ChartPeriod.All.Single(p => p.Code == "y1").GetDateRange(ReferenceDate);

        defaultRange.Should().Be(y1Range);
    }
}
