using FluentAssertions;
using MoneyManager.Api.Model.AI;

namespace MoneyManager.Api.Tests.Services;

public class AnalysisTypeTests
{
    // ----------------------------------------------------------------
    // Catalog shape
    // ----------------------------------------------------------------

    [Fact]
    public void All_HasFourteenAnalysisTypes()
    {
        AnalysisType.All.Should().HaveCount(14);
    }

    [Fact]
    public void All_KeysAreUnique()
    {
        AnalysisType.All.Select(a => a.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_FieldsAreNonEmpty()
    {
        // Every catalog entry must have a non-empty Name, Group, Description,
        // and Prompt. Empty fields would surface as broken UI labels or as
        // empty AI prompts at runtime.
        AnalysisType.All.Should().OnlyContain(a =>
            !string.IsNullOrWhiteSpace(a.Key)
            && !string.IsNullOrWhiteSpace(a.Name)
            && !string.IsNullOrWhiteSpace(a.Group)
            && !string.IsNullOrWhiteSpace(a.Description)
            && !string.IsNullOrWhiteSpace(a.Prompt));
    }

    [Fact]
    public void All_TemperaturesAreInAllowedSet()
    {
        // The pre-migration code used three discrete temperature values
        // mapped to "creativity levels". Arbitrary other values would
        // change AI behavior without explanation.
        AnalysisType.All.Should().OnlyContain(a =>
            a.Temperature == 0.3 || a.Temperature == 0.5 || a.Temperature == 0.7);
    }

    // ----------------------------------------------------------------
    // Find behavior
    // ----------------------------------------------------------------

    [Fact]
    public void Find_ReturnsType_ForKnownKey()
    {
        var type = AnalysisType.Find("DebtAnalysis");

        type.Should().NotBeNull();
        type!.Key.Should().Be("DebtAnalysis");
        type.Name.Should().Be("Debt Analysis");
        type.Group.Should().Be("Debt & Savings");
        type.Temperature.Should().Be(0.3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Find_ReturnsNull_ForNullOrEmpty(string? key)
    {
        AnalysisType.Find(key).Should().BeNull();
    }

    [Theory]
    [InlineData("SpendingGeneral123")]
    [InlineData("spendinggeneral")]  // case-sensitive
    [InlineData("unknown-type")]
    public void Find_ReturnsNull_ForUnknownKey(string key)
    {
        AnalysisType.Find(key).Should().BeNull();
    }

    // ----------------------------------------------------------------
    // Spot-checks per documented type (guards against accidental reordering)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("SpendingGeneral",          0.7, "Spending Analysis")]
    [InlineData("SpendingTrends",           0.7, "Spending Analysis")]
    [InlineData("BehavioralInsights",       0.7, "Behavior & Optimization")]
    [InlineData("SubscriptionsOptimization",0.7, "Behavior & Optimization")]
    [InlineData("SpendingBudget",           0.5, "Spending Analysis")]
    [InlineData("SavingsEmergencyFund",     0.5, "Debt & Savings")]
    [InlineData("GoalBasedPlanning",        0.5, "Planning & Forecasting")]
    [InlineData("TaxEfficiency",            0.5, "Canadian-Specific")]
    [InlineData("RegisteredAccounts",       0.5, "Canadian-Specific")]
    [InlineData("DebtAnalysis",             0.3, "Debt & Savings")]
    [InlineData("CashFlowForecast",         0.3, "Planning & Forecasting")]
    [InlineData("AnomalyDetection",         0.3, "Behavior & Optimization")]
    [InlineData("RecurringIncome",          0.3, "Planning & Forecasting")]
    [InlineData("SeasonalAnalysis",         0.3, "Canadian-Specific")]
    public void Find_DocumentedType_HasExpectedTemperatureAndGroup(
        string key, double expectedTemperature, string expectedGroup)
    {
        var type = AnalysisType.Find(key);

        type.Should().NotBeNull($"'{key}' is a documented analysis type");
        type!.Temperature.Should().Be(expectedTemperature);
        type.Group.Should().Be(expectedGroup);
    }
}
