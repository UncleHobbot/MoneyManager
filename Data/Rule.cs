namespace MoneyManager.Data;

public class Rule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string OriginalDescription { get; set; } = null!;
    public string NewDescription { get; set; } = null!;
    public RuleCompareType CompareType { get; set; }
    [NotMapped] public string CompareTypeString => CompareType.ToString();
    public Category Category { get; set; } = null!;
}

public enum RuleCompareType
{
    Contains,
    StartsWith,
    EndsWith,
    Equals
}

public static class RuleHelper
{
}