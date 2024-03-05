namespace MoneyManager.Data;

public class Account
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShownName { get; set; }
    public string Description { get; set; }
    public int Type { get; set; }
    public string Number { get; set; }
    public bool IsHideFromGraph { get; set; }
}