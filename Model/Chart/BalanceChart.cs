namespace MoneyManager.Model.Chart;

public class BalanceChart
{
    public string Month { get; set; }
    public DateTime FirstDate { get; set; }
    public string MonthLabel { get; set; }
    public string MonthKey { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Balance => Income + Expenses;
}