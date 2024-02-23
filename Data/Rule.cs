namespace MoneyManager.Data;

public class Rule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string OriginalDescription { get; set; }
    public string NewDescription { get; set; }
    public RuleCompareType CompareType { get; set; }
    public Category Category { get; set; }
}

public enum RuleCompareType
{
    Contains,
    StartsWith,
    EndsWith,
    Equals
}