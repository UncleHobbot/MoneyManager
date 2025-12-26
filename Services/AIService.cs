using System.Text;
using Microsoft.Extensions.Options;
using MoneyManager.Model.AI;

namespace MoneyManager.Services;

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
                          </Instructions>

                          <Constraints>
                          - Avoid financial jargon unless the user explicitly asks for deeper technical insight.
                          - Emphasize flexibility and personalization over rigid rules.
                          </Constraints>

                          <Output Format>
                          - **Summary:** Brief recap of the user's situation and goals.
                          - **Result:** Detailed analysis of the user's financial data, including trends and insights.
                          - **Action Plan:** Tailored, actionable steps with clear prioritization.
                          - **Tips & Habits:** Practical advice for improving financial behavior over time.
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
        var prompt = analysisType switch
        {
            "SpendingGeneral" => "Analyze my transactions. What are my top spending categories? Give me a summary of my spending habits.",
            "SpendingBudget" => "Create a 50/30/20 budget based on my average monthly income and expenses",
            "SpendingTrends" => "Compare my monthly spending trends. Am I spending more or less month-over-month? Also produce a clean table by month (total spending + baseline vs irregular)",
            _ => string.Empty
        };

        var data = await dataService.AIGetTransactionsCSV(period);
        var result = await GetAIResponse(prompt, data);
        return result;
    }
}

/*
 Budgeting & Spending Analysis
Purpose: Understand where your money goes and how to optimize spending.

Example Prompts:
    “Analyze my last 3 months of transactions. What are my top spending categories, and how can I reduce discretionary expenses by 20%?”
    “Create a 50/30/20 budget based on my average monthly income and expenses.”
    “Compare my monthly spending trends—am I spending more or less month-over-month?”

📉 Debt Analysis & Repayment Strategies
Purpose: Get a clear path to reduce debt efficiently.

Example Prompts:
    “Review my credit card balances and payment history. What’s the most cost-effective way to pay off my debt—avalanche vs snowball?”
    “Estimate how long it will take me to pay off my credit card debt with my current payments. How much interest will I pay?”
    “Suggest an optimized debt repayment schedule that fits within my budget.”

💰 Savings & Emergency Fund Planning
Purpose: Ensure you're prepared for unexpected expenses and future goals.

Example Prompts:
    “Based on my current expenses, how much should I have in my emergency fund?”
    “Evaluate my savings rate over the past year. Am I on track to meet my savings goals?”
    “I want to save $10,000 in the next 12 months. What monthly adjustments do I need to make to get there?”

📈 Cash Flow Forecasting
Purpose: Predict financial trends and spot issues before they arise.

Example Prompts:
    “Forecast my cash flow over the next 6 months based on historical trends.”
    “Identify any months in the past year where my expenses exceeded my income. What caused it?”

📊 Spending Categorization & Visualization
Purpose: Gain better insight through structured data.

Example Prompts:
    “Categorize all my transactions from the last quarter and provide a spending breakdown by category.”
    “Create a monthly graph of my income, fixed expenses, and discretionary spending.”

🎯 Goal-Based Financial Planning
Purpose: Align spending/saving behavior with specific financial goals.

Example Prompts:
    “Help me plan to buy a car worth $15,000 in 18 months. How much should I save monthly?”
    “Based on my current surplus, how long would it take me to afford a down payment on a $300,000 house?”

🛠️ Efficiency & Optimization
Purpose: Improve how money flows through your life.

Example Prompts:
    “Are there any subscription services I’m paying for that I rarely use?”
    “Can you identify duplicate or unusual transactions I should look into?”
    “Find opportunities to automate bill payments or recurring transfers to improve financial discipline.”

🧠 Behavioral & Habit Insights
Purpose: Build awareness of financial habits and triggers.

Example Prompts:
    “Identify any patterns in my spending—do I spend more on weekends or around payday?”
    “Which expenses seem emotionally driven or impulsive?”

📚 Tax-Efficiency Suggestions (if applicable)
Purpose: Avoid overpaying taxes or missing deductions.

Example Prompts:
    “Are there any recurring expenses that might qualify as tax-deductible?”
    “Based on my income and RRSP contributions, can I estimate potential tax savings?”

*/