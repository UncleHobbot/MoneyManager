namespace MoneyManager.Data;

public class Transaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Account Account { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
    public string OriginalDescription { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountExt => IsDebit ? -Amount : Amount;
    public bool IsDebit { get; set; }
    public Category Category { get; set; }
    public bool IsRuleApplied { get; set; }
    
    public override string ToString() => $"{Date:d}: {Amount:C} {(IsRuleApplied?"[x] ":"")}{Description}";
}
