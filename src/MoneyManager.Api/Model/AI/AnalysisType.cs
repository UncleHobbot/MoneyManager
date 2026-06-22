namespace MoneyManager.Api.Model.AI;

/// <summary>
/// A financial-analysis type: a key (the wire-format identifier the
/// frontend sends), the user prompt that drives the analysis, and the
/// temperature (creativity) setting for the AI request. Replaces the
/// legacy <c>AnalysisTypePrompts</c> static class with its two parallel
/// switches over the same vocabulary.
/// </summary>
/// <remarks>
/// <para>
/// To add a new analysis type, append one entry to <see cref="All"/>. The
/// pipeline picks it up automatically - no switch to update, no constant
/// to declare.
/// </para>
/// <para>
/// Temperatures follow the three-tier convention from the pre-migration
/// code: <c>0.3</c> for analyses requiring precision (debt, forecasts,
/// anomalies), <c>0.5</c> for balanced recommendations (budgets, savings,
/// tax strategies), <c>0.7</c> for creative exploratory insights (general
/// spending, trends, behavioral patterns).
/// </para>
/// <para>
/// All prompts assume Canadian financial context (CAD, RRSP, TFSA, FHSA,
/// taxes) and request bilingual output (English + Russian). The system
/// prompt in <c>AIService.GetAIResponseAsync</c> establishes the persona;
/// the per-type prompt here specifies what the AI should analyze.
/// </para>
/// </remarks>
public sealed record AnalysisType(
    string Key,
    string Name,
    string Group,
    string Description,
    string Prompt,
    double Temperature)
{
    /// <summary>
    /// All known analysis types, in the order they were defined. To add a
    /// type, append one entry.
    /// </summary>
    public static readonly IReadOnlyList<AnalysisType> All = new[]
    {
        // ── Spending Analysis ────────────────────────────────────────
        new AnalysisType(
            Key: "SpendingGeneral",
            Name: "General Spending",
            Group: "Spending Analysis",
            Description: "Comprehensive overview of spending habits, top categories, outliers, and daily/weekly averages.",
            Prompt: """
                Analyze my transactions comprehensively.
                - What are my top spending categories by amount and frequency?
                - Give me a summary of my overall spending habits and patterns.
                - Identify any outliers or unusual spending patterns.
                - Calculate my daily and weekly spending averages.
                - Include spending velocity metrics.
                """,
            Temperature: 0.7),

        new AnalysisType(
            Key: "SpendingBudget",
            Name: "Budget Analysis",
            Group: "Spending Analysis",
            Description: "Compares actual spending to budgeting methods (50/30/20, zero-based, envelope) with recommendations.",
            Prompt: """
                Create a comprehensive budget analysis based on my average monthly income and expenses.
                - Recommend and explain the 50/30/20 budgeting method tailored to my situation.
                - Also provide alternative budgeting methods (zero-based budgeting, envelope method) and explain which might work best for me.
                - Show actual vs recommended allocation for each category.
                - Identify areas where I'm overspending compared to the budget.
                """,
            Temperature: 0.5),

        new AnalysisType(
            Key: "SpendingTrends",
            Name: "Spending Trends",
            Group: "Spending Analysis",
            Description: "Month-over-month and year-over-year comparisons with inflation-adjusted real spending changes.",
            Prompt: """
                Perform a detailed monthly spending trend analysis.
                - Compare my spending month-over-month with percentage changes.
                - Compare year-over-year trends where applicable.
                - Adjust for inflation to show real spending changes.
                - Produce a clean table showing: Month, Total Spending, Fixed/Regular Expenses, Baseline Spending, Irregular/One-off Expenses.
                - Identify which categories are driving changes.
                - Highlight any concerning patterns or positive trends.
                """,
            Temperature: 0.7),

        // ── Debt & Savings ───────────────────────────────────────────
        new AnalysisType(
            Key: "DebtAnalysis",
            Name: "Debt Analysis",
            Group: "Debt & Savings",
            Description: "Evaluates debt situation and recommends avalanche vs snowball payoff strategies with timelines.",
            Prompt: """
                Analyze my debt situation comprehensively.
                - Review my credit card balances, loan balances, and payment history.
                - Explain the difference between avalanche (highest interest first) and snowball (smallest balance first) debt repayment methods.
                - Recommend which method is most cost-effective for my situation.
                - Estimate how long it will take me to pay off my debt with current payments.
                - Calculate total interest I will pay.
                - Suggest an optimized debt repayment schedule that fits within my budget.
                - Provide a debt payoff timeline.
                """,
            Temperature: 0.3),

        new AnalysisType(
            Key: "SavingsEmergencyFund",
            Name: "Savings & Emergency Fund",
            Group: "Debt & Savings",
            Description: "Evaluates emergency fund position, savings rate, and strategies to reach 3–6 months of expenses.",
            Prompt: """
                Evaluate my savings and emergency fund position.
                - Based on my monthly expenses, calculate how much I should have in my emergency fund (recommend 3-6 months).
                - Evaluate my current savings rate over the analyzed period.
                - Determine if I'm on track to meet my savings goals.
                - Calculate how long it would take me to build a full emergency fund at my current savings rate.
                - Suggest strategies to accelerate emergency fund building.
                - Identify opportunities to optimize my savings rate.
                """,
            Temperature: 0.5),

        // ── Planning & Forecasting ───────────────────────────────────
        new AnalysisType(
            Key: "CashFlowForecast",
            Name: "Cash Flow Forecast",
            Group: "Planning & Forecasting",
            Description: "Predicts cash flow over the next 6 months, identifying potential shortfalls and surplus periods.",
            Prompt: """
                Perform a comprehensive cash flow forecast.
                - Forecast my cash flow over the next 6 months based on historical trends and patterns.
                - Identify any recurring patterns in income and expenses.
                - Predict which months might have tight cash flow or potential shortfalls.
                - Identify any months in the past year where expenses exceeded income and explain what caused it.
                - Account for known recurring large expenses based on historical data.
                - Provide recommendations for smoothing cash flow.
                """,
            Temperature: 0.3),

        new AnalysisType(
            Key: "GoalBasedPlanning",
            Name: "Goal-Based Planning",
            Group: "Planning & Forecasting",
            Description: "Creates savings plans for specific goals (car, house, vacation) with timelines and account allocation.",
            Prompt: """
                Help me create a goal-based savings plan.
                - Analyze my current surplus (income minus expenses).
                - Based on my spending patterns, suggest realistic monthly savings amounts.
                - For each goal type (car down payment, home down payment, vacation, major purchase), calculate:
                  * How long it would take to reach the goal at current savings rate
                  * How much I need to save monthly to reach specific targets
                  * Optimal account allocation (TFSA, RRSP, FHSA, or non-registered)
                - Provide actionable recommendations for prioritizing multiple goals.
                """,
            Temperature: 0.5),

        new AnalysisType(
            Key: "RecurringIncome",
            Name: "Recurring Income",
            Group: "Planning & Forecasting",
            Description: "Analyzes income patterns, sources, and irregularities for better forecasting.",
            Prompt: """
                Analyze my income patterns and sources.
                - Identify all recurring income sources and their amounts.
                - Calculate average monthly income, noting any irregularity.
                - Identify bonus, commission, or irregular income patterns.
                - Analyze timing of income deposits relative to expenses.
                - Note any income seasonality or patterns.
                - Provide recommendations for managing irregular income (buffer strategy).
                """,
            Temperature: 0.3),

        // ── Behavior & Optimization ──────────────────────────────────
        new AnalysisType(
            Key: "BehavioralInsights",
            Name: "Behavioral Insights",
            Group: "Behavior & Optimization",
            Description: "Analyzes weekend/weekday patterns, trigger events, and psychological spending behaviors.",
            Prompt: """
                Provide deep behavioral insights into my spending patterns.
                - Identify patterns: Do I spend more on weekends, weekdays, or around payday?
                - Analyze spending by day of week and time of month.
                - Which expenses appear to be emotionally driven or impulsive?
                - Identify trigger events or situations that lead to increased spending.
                - Look for patterns like "spending more on Friday nights" or "post-payday splurge."
                - Provide awareness-building insights to help improve financial behavior.
                """,
            Temperature: 0.7),

        new AnalysisType(
            Key: "SubscriptionsOptimization",
            Name: "Subscriptions Optimization",
            Group: "Behavior & Optimization",
            Description: "Identifies all subscriptions, evaluates usage value, and suggests cancellations or downgrades.",
            Prompt: """
                Identify and analyze all subscription and recurring service payments.
                - List all subscription services I'm paying for with amounts and frequency.
                - Identify any services I rarely use based on transaction patterns.
                - Find duplicate charges or double-billing issues.
                - Suggest which subscriptions to keep, downgrade, or cancel.
                - Calculate potential savings from optimization.
                - Look for opportunities to automate bill payments.
                """,
            Temperature: 0.7),

        new AnalysisType(
            Key: "AnomalyDetection",
            Name: "Anomaly Detection",
            Group: "Behavior & Optimization",
            Description: "Identifies unusual, duplicate, or suspicious transactions and spending spikes.",
            Prompt: """
                Perform a thorough anomaly detection on my transactions.
                - Identify unusual or one-off transactions.
                - Flag transactions that are significantly larger than normal for that category.
                - Find duplicate transactions or potential errors.
                - Identify spending spikes or sudden increases in any category.
                - Look for suspicious or unrecognized transactions to investigate.
                - Provide a prioritized list of items requiring attention.
                """,
            Temperature: 0.3),

        // ── Canadian-Specific ────────────────────────────────────────
        new AnalysisType(
            Key: "TaxEfficiency",
            Name: "Tax Efficiency",
            Group: "Canadian-Specific",
            Description: "Analyzes Canadian tax-deductible expenses and registered account (RRSP, TFSA, FHSA) recommendations.",
            Prompt: """
                Provide Canadian tax efficiency recommendations.
                - Identify recurring expenses that might qualify as tax deductions.
                - Analyze my RRSP contributions and estimate potential tax savings.
                - Review my TFSA contributions and contribution room usage.
                - Suggest tax-efficient investment strategies.
                - Identify expenses that could be tax-deductible (medical, work-related, charitable).
                - Note any eligible FHSA contributions for first-time home buying.
                """,
            Temperature: 0.5),

        new AnalysisType(
            Key: "RegisteredAccounts",
            Name: "Registered Accounts",
            Group: "Canadian-Specific",
            Description: "RRSP vs TFSA vs FHSA optimization, contribution room analysis, and allocation strategy.",
            Prompt: """
                Analyze my registered savings accounts (RRSP, TFSA, FHSA).
                - Review contributions and withdrawals from registered accounts.
                - Evaluate my contribution room usage and remaining room.
                - Suggest optimal allocation between RRSP (tax-deferred) vs TFSA (tax-free).
                - Provide FHSA guidance if applicable (first-time home buyer).
                - Identify opportunities for tax-efficient savings.
                - Explain when to use each account type for specific goals.
                """,
            Temperature: 0.5),

        new AnalysisType(
            Key: "SeasonalAnalysis",
            Name: "Seasonal Analysis",
            Group: "Canadian-Specific",
            Description: "Identifies seasonal spending patterns, holiday impacts, and weather-related expense fluctuations.",
            Prompt: """
                Perform a seasonal spending analysis.
                - Identify spending patterns across seasons and holidays.
                - Analyze holiday spending peaks (Christmas, etc.) and their impact.
                - Look for tax season patterns (tax refunds, tax payments).
                - Identify seasonal expenses (utilities, vacation, winter equipment, etc.).
                - Compare current year's seasonal spending to previous patterns.
                - Provide recommendations for planning and managing seasonal expenses.
                """,
            Temperature: 0.3),
    };

    /// <summary>
    /// Looks up an <see cref="AnalysisType"/> by its key. Returns null for
    /// unknown keys. The single caller (<c>AIService.GetAnalysisAsync</c>)
    /// preserves pre-migration behavior by treating null as "empty prompt,
    /// 0.7 temperature" - i.e., the AI gets called with an empty user
    /// message and the highest-creativity setting, matching what the
    /// legacy <c>AnalysisTypePrompts</c> switches returned for default cases.
    /// </summary>
    public static AnalysisType? Find(string? key) =>
        string.IsNullOrEmpty(key) ? null : All.FirstOrDefault(a => a.Key == key);
}
