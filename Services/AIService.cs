using System.Text;
using Microsoft.Extensions.Options;
using MoneyManager.Model.AI;

namespace MoneyManager.Services;

public static class AnalysisTypePrompts
{
    public const string SpendingGeneral = "SpendingGeneral";
    public const string SpendingBudget = "SpendingBudget";
    public const string SpendingTrends = "SpendingTrends";
    public const string DebtAnalysis = "DebtAnalysis";
    public const string SavingsEmergencyFund = "SavingsEmergencyFund";
    public const string CashFlowForecast = "CashFlowForecast";
    public const string GoalBasedPlanning = "GoalBasedPlanning";
    public const string RecurringIncome = "RecurringIncome";
    public const string BehavioralInsights = "BehavioralInsights";
    public const string SubscriptionsOptimization = "SubscriptionsOptimization";
    public const string AnomalyDetection = "AnomalyDetection";
    public const string TaxEfficiency = "TaxEfficiency";
    public const string RegisteredAccounts = "RegisteredAccounts";
    public const string SeasonalAnalysis = "SeasonalAnalysis";

    public static string GetPrompt(string analysisType)
    {
        return analysisType switch
        {
            SpendingGeneral => """
                Analyze my transactions comprehensively. 
                - What are my top spending categories by amount and frequency?
                - Give me a summary of my overall spending habits and patterns.
                - Identify any outliers or unusual spending patterns.
                - Calculate my daily and weekly spending averages.
                - Include spending velocity metrics.
                """,
            SpendingBudget => """
                Create a comprehensive budget analysis based on my average monthly income and expenses.
                - Recommend and explain the 50/30/20 budgeting method tailored to my situation.
                - Also provide alternative budgeting methods (zero-based budgeting, envelope method) and explain which might work best for me.
                - Show actual vs recommended allocation for each category.
                - Identify areas where I'm overspending compared to the budget.
                """,
            SpendingTrends => """
                Perform a detailed monthly spending trend analysis.
                - Compare my spending month-over-month with percentage changes.
                - Compare year-over-year trends where applicable.
                - Adjust for inflation to show real spending changes.
                - Produce a clean table showing: Month, Total Spending, Fixed/Regular Expenses, Baseline Spending, Irregular/One-off Expenses.
                - Identify which categories are driving changes.
                - Highlight any concerning patterns or positive trends.
                """,
            DebtAnalysis => """
                Analyze my debt situation comprehensively.
                - Review my credit card balances, loan balances, and payment history.
                - Explain the difference between avalanche (highest interest first) and snowball (smallest balance first) debt repayment methods.
                - Recommend which method is most cost-effective for my situation.
                - Estimate how long it will take me to pay off my debt with current payments.
                - Calculate total interest I will pay.
                - Suggest an optimized debt repayment schedule that fits within my budget.
                - Provide a debt payoff timeline.
                """,
            SavingsEmergencyFund => """
                Evaluate my savings and emergency fund position.
                - Based on my monthly expenses, calculate how much I should have in my emergency fund (recommend 3-6 months).
                - Evaluate my current savings rate over the analyzed period.
                - Determine if I'm on track to meet my savings goals.
                - Calculate how long it would take me to build a full emergency fund at my current savings rate.
                - Suggest strategies to accelerate emergency fund building.
                - Identify opportunities to optimize my savings rate.
                """,
            CashFlowForecast => """
                Perform a comprehensive cash flow forecast.
                - Forecast my cash flow over the next 6 months based on historical trends and patterns.
                - Identify any recurring patterns in income and expenses.
                - Predict which months might have tight cash flow or potential shortfalls.
                - Identify any months in the past year where expenses exceeded income and explain what caused it.
                - Account for known recurring large expenses based on historical data.
                - Provide recommendations for smoothing cash flow.
                """,
            GoalBasedPlanning => """
                Help me create a goal-based savings plan.
                - Analyze my current surplus (income minus expenses).
                - Based on my spending patterns, suggest realistic monthly savings amounts.
                - For each goal type (car down payment, home down payment, vacation, major purchase), calculate:
                  * How long it would take to reach the goal at current savings rate
                  * How much I need to save monthly to reach specific targets
                  * Optimal account allocation (TFSA, RRSP, FHSA, or non-registered)
                - Provide actionable recommendations for prioritizing multiple goals.
                """,
            RecurringIncome => """
                Analyze my income patterns and sources.
                - Identify all recurring income sources and their amounts.
                - Calculate average monthly income, noting any irregularity.
                - Identify bonus, commission, or irregular income patterns.
                - Analyze timing of income deposits relative to expenses.
                - Note any income seasonality or patterns.
                - Provide recommendations for managing irregular income (buffer strategy).
                """,
            BehavioralInsights => """
                Provide deep behavioral insights into my spending patterns.
                - Identify patterns: Do I spend more on weekends, weekdays, or around payday?
                - Analyze spending by day of week and time of month.
                - Which expenses appear to be emotionally driven or impulsive?
                - Identify trigger events or situations that lead to increased spending.
                - Look for patterns like "spending more on Friday nights" or "post-payday splurge."
                - Provide awareness-building insights to help improve financial behavior.
                """,
            SubscriptionsOptimization => """
                Identify and analyze all subscription and recurring service payments.
                - List all subscription services I'm paying for with amounts and frequency.
                - Identify any services I rarely use based on transaction patterns.
                - Find duplicate charges or double-billing issues.
                - Suggest which subscriptions to keep, downgrade, or cancel.
                - Calculate potential savings from optimization.
                - Look for opportunities to automate bill payments.
                """,
            AnomalyDetection => """
                Perform a thorough anomaly detection on my transactions.
                - Identify unusual or one-off transactions.
                - Flag transactions that are significantly larger than normal for that category.
                - Find duplicate transactions or potential errors.
                - Identify spending spikes or sudden increases in any category.
                - Look for suspicious or unrecognized transactions to investigate.
                - Provide a prioritized list of items requiring attention.
                """,
            TaxEfficiency => """
                Provide Canadian tax efficiency recommendations.
                - Identify recurring expenses that might qualify as tax deductions.
                - Analyze my RRSP contributions and estimate potential tax savings.
                - Review my TFSA contributions and contribution room usage.
                - Suggest tax-efficient investment strategies.
                - Identify expenses that could be tax-deductible (medical, work-related, charitable).
                - Note any eligible FHSA contributions for first-time home buying.
                """,
            RegisteredAccounts => """
                Analyze my registered savings accounts (RRSP, TFSA, FHSA).
                - Review contributions and withdrawals from registered accounts.
                - Evaluate my contribution room usage and remaining room.
                - Suggest optimal allocation between RRSP (tax-deferred) vs TFSA (tax-free).
                - Provide FHSA guidance if applicable (first-time home buyer).
                - Identify opportunities for tax-efficient savings.
                - Explain when to use each account type for specific goals.
                """,
            SeasonalAnalysis => """
                Perform a seasonal spending analysis.
                - Identify spending patterns across seasons and holidays.
                - Analyze holiday spending peaks (Christmas, etc.) and their impact.
                - Look for tax season patterns (tax refunds, tax payments).
                - Identify seasonal expenses (utilities, vacation, winter equipment, etc.).
                - Compare current year's seasonal spending to previous patterns.
                - Provide recommendations for planning and managing seasonal expenses.
                """,
            _ => string.Empty
        };
    }

    public static double GetTemperature(string analysisType)
    {
        return analysisType switch
        {
            DebtAnalysis or CashFlowForecast or AnomalyDetection or RecurringIncome or SeasonalAnalysis => 0.3,
            SpendingBudget or SavingsEmergencyFund or GoalBasedPlanning or TaxEfficiency or RegisteredAccounts => 0.5,
            SpendingGeneral or SpendingTrends or BehavioralInsights or SubscriptionsOptimization => 0.7,
            _ => 0.7
        };
    }
}

public class AIService(IOptions<OpenAISettings> options, DataService dataService)
{
    private static readonly HttpClient httpClient = new();

    private async Task<AnalysisResult> GetAIResponse(string prompt, string? data, double temperature = 0.7)
    {
        var messages = new List<OpenAIMessage>
        {
            new()
            {
                role = "system",
                content = """
                          <System>
                          You are a certified financial advisor specializing in practical, non-judgmental, and empowering personal finance guidance. Your role is to help users make informed decisions about budgeting, saving, investing, and debt management, with empathy and clarity.
                          </System>

                          <Context>
                          The user is seeking to understand and improve their financial situation. They may have goals like paying off debt, saving for a large purchase, building an emergency fund, or creating a long-term financial plan. The user values straightforward, supportive advice tailored to their circumstances.
                          The user lives in Canada and uses Canadian dollars (CAD) for all transactions. Also specific canadian rules apply, like registered saving accounts (RRSP, TFSA, FHSA), taxes, etc. Investments to registered accounts are cosidered as savings. Payroll cheques are bi-weekly, and the user is not self-employed.
                          </Context>

                          <Instructions>
                          - Maintain a tone that is concise, encouraging, and free of judgment.
                          - Do not thank the user for their input or express gratitude.
                          - Provide the result in English and Russian.
                          - Use tables for structured data presentation.
                          - Use bold for key figures and important insights.
                          - Be specific with numbers, percentages, and dates.
                          </Instructions>

                          <Constraints>
                          - Avoid financial jargon unless the user explicitly asks for deeper technical insight.
                          - Emphasize flexibility and personalization over rigid rules.
                          - Base all analysis on the provided transaction data.
                          - Flag if data is insufficient for certain analyses.
                          </Constraints>

                          <Output Format>
                          - **Summary (Анотация):** 2-3 sentence recap of key findings.
                          - **Detailed Analysis (Детальный анализ):** Thorough breakdown with tables, charts (as text tables), and specific metrics.
                          - **Key Insights (Ключевые моменты):** 3-5 bullet points of most important discoveries.
                          - **Action Plan (План действий):** Prioritized, actionable steps with timelines if applicable.
                          - **Tips & Recommendations (Советы):** Practical advice for long-term improvement.
                          </Output Format>

                          <Reasoning>
                          Incorporate empathetic reasoning and Theory of Mind to recognize the user's emotional tone and intent. Apply Strategic Chain-of-Thought to reason through options and explain trade-offs clearly. Favor System 2 (analytical) thinking when constructing plans, but maintain a tone that is accessible and reassuring.
                          </Reasoning>
                          """
            },
            new()
            {
                role = "user",
                content = prompt
            }
        };

        if (!string.IsNullOrWhiteSpace(data))
            messages.Add(new OpenAIMessage
            {
                role = "user",
                content = $"Analyze the following CSV data:\n\n{data}"
            });

        var request = new OpenAIChatRequest
        {
            model = options.Value.Model,
            messages = messages,
            temperature = temperature
        };

        var apiKey = options.Value.ApiKey;
        var apiUrl = options.Value.ApiUrl;

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await httpClient.PostAsync(apiUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var doc = JsonSerializer.Deserialize<OpenAIChatResponse>(responseString);
            if (doc is { Choices.Count: > 0 })
            {
                var result = doc.Choices[0].Message.Content;
                return new AnalysisResult(true, result, doc.Usage.TotalTokens);
            }

            return new AnalysisResult(false, "Error: No choices in response.", doc.Usage.TotalTokens);
        }

        return new AnalysisResult(false, responseString, 0);
    }

    public async Task<AnalysisResult> GetAnalysis(string period, string analysisType)
    {
        var prompt = AnalysisTypePrompts.GetPrompt(analysisType);
        var temperature = AnalysisTypePrompts.GetTemperature(analysisType);

        var data = await dataService.AIGetTransactionsCSV(period);
        var result = await GetAIResponse(prompt, data, temperature);
        return result;
    }
}

/*
  Available Analysis Types in AnalysisTypePrompts:
  
  SpendingGeneral (SpendingGeneral)
    - Top spending categories by amount and frequency
    - Overall spending habits and patterns
    - Outlier identification
    - Daily/weekly spending averages
    - Spending velocity metrics
  
  SpendingBudget (SpendingBudget)
    - 50/30/20 budgeting method tailored to situation
    - Alternative methods: zero-based, envelope
    - Actual vs recommended allocation
    - Overspending identification
  
  SpendingTrends (SpendingTrends)
    - Month-over-month spending comparison
    - Year-over-year trends
    - Inflation-adjusted changes
    - Table: Month | Total Spending | Fixed/Regular | Baseline | Irregular
    - Category drivers of change
  
  DebtAnalysis (DebtAnalysis)
    - Credit card and loan balance analysis
    - Avalanche vs snowball method comparison
    - Payoff timeline at current payments
    - Total interest calculation
    - Optimized repayment schedule
  
  SavingsEmergencyFund (SavingsEmergencyFund)
    - Emergency fund target (3-6 months)
    - Current savings rate evaluation
    - Goal tracking
    - Time to build emergency fund
    - Acceleration strategies
  
  CashFlowForecast (CashFlowForecast)
    - 6-month cash flow prediction
    - Recurring pattern identification
    - Tight cash flow months
    - Historical income/expense mismatches
    - Large expense planning
  
  GoalBasedPlanning (GoalBasedPlanning)
    - Current surplus analysis
    - Monthly savings recommendations
    - Goal timelines (car, house, vacation)
    - Account allocation (TFSA, RRSP, FHSA)
    - Goal prioritization
  
  RecurringIncome (RecurringIncome)
    - Income source identification
    - Monthly average and irregularity
    - Bonus/commission patterns
    - Income timing vs expenses
    - Seasonality patterns
    - Irregular income buffer strategy
  
  BehavioralInsights (BehavioralInsights)
    - Weekend/weekday spending patterns
    - Payday cycle spending
    - Day of week/month analysis
    - Impulse/emotional purchases
    - Trigger event identification
    - Spending habit awareness
  
  SubscriptionsOptimization (SubscriptionsOptimization)
    - Complete subscription list
    - Rarely used services
    - Duplicate charges
    - Keep/downgrade/cancel recommendations
    - Potential savings calculation
    - Automation opportunities
  
  AnomalyDetection (AnomalyDetection)
    - Unusual/one-off transactions
    - Category outliers
    - Duplicate transactions
    - Spending spikes
    - Suspicious/unrecognized charges
  
  TaxEfficiency (TaxEfficiency)
    - Tax-deductible expenses
    - RRSP contribution tax savings
    - TFSA contribution room
    - Tax-efficient investment strategies
    - Eligible deductions (medical, work, charitable)
    - FHSA guidance
  
  RegisteredAccounts (RegisteredAccounts)
    - RRSP/TFSA/FHSA analysis
    - Contribution room usage
    - Optimal account allocation
    - RRSP vs TFSA timing
    - Goal-based account selection
  
  SeasonalAnalysis (SeasonalAnalysis)
    - Seasonal spending patterns
    - Holiday spending peaks
    - Tax season patterns
    - Seasonal expenses (utilities, vacation, winter)
    - Year-over-year seasonal comparison
    - Seasonal planning recommendations
*/