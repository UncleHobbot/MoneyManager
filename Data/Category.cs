namespace MoneyManager.Data;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Category Parent { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool IsNew { get; set; }
}