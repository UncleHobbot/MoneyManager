using Markdig;
using MouseEventArgs = Microsoft.AspNetCore.Components.Web.MouseEventArgs;
using MoneyManager.Services;

namespace MoneyManager.Pages;

public partial class AI : ComponentBase
{
    [Inject] private AIService aiService { get; set; } = null!;
    private string ChartPeriod { get; set; } = "y1";
    private string? AnalysisType { get; set; }
    private string? Result { get; set; }
    private string? ResultHTML { get; set; }
    private string? ResultTokens { get; set; }
    private bool IsButtonVisible { get; set; }
    private bool IsRunning { get; set; }

    private async Task Action()
    {
        if (!string.IsNullOrEmpty(AnalysisType))
        {
            IsButtonVisible = false;
            IsRunning = true;
            var result = await aiService.GetAnalysis(ChartPeriod, AnalysisType);
            Result = result.Result;
            ResultTokens = $"Used tokens: <b>{result.TotalTokens:n0}</b>";
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            ResultHTML = Markdown.ToHtml(Result, pipeline);
            IsRunning = false;
            StateHasChanged();
        }
    }

    private void SpendingGeneral(MouseEventArgs arg)
    {
        AnalysisType = AnalysisTypePrompts.SpendingGeneral;
        IsButtonVisible = true;
    }

    private void SpendingBudget()
    {
        AnalysisType = AnalysisTypePrompts.SpendingBudget;
        IsButtonVisible = true;
    }

    private void SpendingTrends()
    {
        AnalysisType = AnalysisTypePrompts.SpendingTrends;
        IsButtonVisible = true;
    }

    private void DebtAnalysis()
    {
        AnalysisType = AnalysisTypePrompts.DebtAnalysis;
        IsButtonVisible = true;
    }

    private void SavingsEmergencyFund()
    {
        AnalysisType = AnalysisTypePrompts.SavingsEmergencyFund;
        IsButtonVisible = true;
    }

    private void CashFlowForecast()
    {
        AnalysisType = AnalysisTypePrompts.CashFlowForecast;
        IsButtonVisible = true;
    }

    private void GoalBasedPlanning()
    {
        AnalysisType = AnalysisTypePrompts.GoalBasedPlanning;
        IsButtonVisible = true;
    }

    private void RecurringIncome()
    {
        AnalysisType = AnalysisTypePrompts.RecurringIncome;
        IsButtonVisible = true;
    }

    private void BehavioralInsights()
    {
        AnalysisType = AnalysisTypePrompts.BehavioralInsights;
        IsButtonVisible = true;
    }

    private void SubscriptionsOptimization()
    {
        AnalysisType = AnalysisTypePrompts.SubscriptionsOptimization;
        IsButtonVisible = true;
    }

    private void AnomalyDetection()
    {
        AnalysisType = AnalysisTypePrompts.AnomalyDetection;
        IsButtonVisible = true;
    }

    private void TaxEfficiency()
    {
        AnalysisType = AnalysisTypePrompts.TaxEfficiency;
        IsButtonVisible = true;
    }

    private void RegisteredAccounts()
    {
        AnalysisType = AnalysisTypePrompts.RegisteredAccounts;
        IsButtonVisible = true;
    }

    private void SeasonalAnalysis()
    {
        AnalysisType = AnalysisTypePrompts.SeasonalAnalysis;
        IsButtonVisible = true;
    }
}