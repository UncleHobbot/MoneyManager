namespace MoneyManager.Model.Chart;

public class BalanceChart
{
    public string Month { get; set; } = null!;
    public DateTime FirstDate { get; set; }
    public string MonthLabel { get; set; } = null!;
    public string MonthKey { get; set; } = null!;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income + Expenses;
}