namespace MoneyManager.Data;

public class Transaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Account Account { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Description { get; set; } = null!;
    public string OriginalDescription { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal AmountExt => IsDebit ? -Amount : Amount;
    public bool IsDebit { get; set; }
    public Category? Category { get; set; }
    public bool IsRuleApplied { get; set; }

    public override string ToString() => $"{Date:d}: {Amount:C} {(IsRuleApplied ? "[x] " : "")}{Description}";

    public TransactionDto ToDto() => new()
    {
        Id = Id,
        Transaction = this,
        Account = Account,
        Date = Date,
        Description = Description,
        Amount = Amount,
        AmountExt = AmountExt,
        IsDebit = IsDebit,
        Category = Category,
        OriginalDescription = OriginalDescription,
    };
}

public class TransactionDto
{
    public int Id { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Description { get; set; } = null!;
    public string OriginalDescription { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal AmountExt { get; set; }
    public bool IsDebit { get; set; }
    public Category? Category { get; set; }
    public bool IsRuleApplied { get; set; }

    public override string ToString() => $"{Date:d}: {Amount:C} {(IsRuleApplied ? "[x] " : "")}{Description}";
}