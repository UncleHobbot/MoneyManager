using Icon = Microsoft.FluentUI.AspNetCore.Components.Icon;

namespace MoneyManager.Data;

public class Account
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string ShownName { get; set; } = null!;
    public string? Description { get; set; }
    public int Type { get; set; }
    public string? Number { get; set; }
    public bool IsHideFromGraph { get; set; }
    public string? AlternativeName1 { get; set; }
    public string? AlternativeName2 { get; set; }

    [NotMapped]
    public Icon TypeIcon => AccountHelper.TypeIcon(Type);

    public override string ToString() => ShownName;
}

public static class AccountHelper
{
    public static Icon TypeIcon(int accountType) => accountType switch
    {
        0 => new Icons.Regular.Size20.MoneyHand(), //Cash
        1 => new Icons.Regular.Size20.Payment(), //CreditCard
        2 => new Icons.Regular.Size20.ArrowTrendingLines(), // Investment
        _ => new Icons.Regular.Size20.CurrencyDollarEuro()
    };
}