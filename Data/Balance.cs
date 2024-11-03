namespace MoneyManager.Data;

public class Balance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Account? Account { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}