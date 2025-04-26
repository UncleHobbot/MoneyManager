namespace MoneyManager.Model.AI;

public class TransactionAI
{
    public int Id { get; set; }
    public string Account { get; set; } = null!;
    public string Date { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Category { get; set; } = null!;
    public string ParentCategory { get; set; } = null!;
}