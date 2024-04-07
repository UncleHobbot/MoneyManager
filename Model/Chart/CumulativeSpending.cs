namespace MoneyManager.Model.Chart;

public class CumulativeSpendingChart
{
    public int DayNumber { get; set; }
    public decimal Expenses { get; set; }
    public decimal? LastMonthExpenses { get; set; }
    public decimal? ThisMonthExpenses { get; set; }
}