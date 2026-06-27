namespace MoneyManager.Api.Model.AI;

/// <summary>
/// The result of an AI analysis request. <see cref="IsSuccess"/> distinguishes a
/// completed analysis from a failure; on failure <see cref="Result"/> carries the
/// error message. <see cref="TotalTokens"/> is reported for billing even on failure.
/// </summary>
public class AnalysisResult(bool isSuccess, string result, int totalTokens)
{
    /// <summary>True if the analysis completed; false if an error occurred (the message is in <see cref="Result"/>).</summary>
    public bool IsSuccess { get; set; } = isSuccess;

    /// <summary>The AI-generated analysis text on success, or the error message on failure.</summary>
    public string Result { get; set; } = result;

    /// <summary>Total tokens consumed (prompt + completion); 0 when the request never reached the model.</summary>
    public int TotalTokens { get; set; } = totalTokens;
}
