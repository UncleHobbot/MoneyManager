namespace MoneyManager.Data;

public class Rule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string OriginalDescription { get; set; }
    public string NewDescription { get; set; }
    public RuleCompareType CompareType { get; set; }
    [NotMapped] public string CompareTypeString => CompareType.ToString();
    public Category Category { get; set; }
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