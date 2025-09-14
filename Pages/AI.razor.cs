using Markdig;
using MouseEventArgs = Microsoft.AspNetCore.Components.Web.MouseEventArgs;

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
        AnalysisType = "SpendingGeneral";
        IsButtonVisible = true;
    }

    private void SpendingBudget()
    {
        AnalysisType = "SpendingBudget";
        IsButtonVisible = true;
    }

    private void SpendingTrends()
    {
        AnalysisType = "SpendingTrends";
        IsButtonVisible = true;
    }
}