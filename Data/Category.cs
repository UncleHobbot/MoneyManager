using Icon = Microsoft.FluentUI.AspNetCore.Components.Icon;

namespace MoneyManager.Data;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Category? Parent { get; set; }
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public bool IsNew { get; set; }

    public string? pIcon => Parent == null ? Icon : Parent.Icon;
    public Icon objIcon => CategoryHelper.CategoryIcon(Parent == null ? Icon : Parent.Icon);
    public override string ToString() => Name;
}

public class CategoryTree
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public HashSet<CategoryTree> Children { get; set; } = [];
}

public class CategoryDropItem(Category c, string parentId)
{
    public int Id { get; set; } = c.Id;
    public string Name { get; set; } = c.Name;
    public string ParentCategory { get; set; } = parentId;
}

public static class CategoryHelper
{
    public static List<string> CategoryIcons => Enum.GetNames(typeof(CategoryIconEnum)).ToList();

    public static Icon CategoryIcon(string? icon) => string.IsNullOrWhiteSpace(icon)
        ? new Icons.Regular.Size20.BorderNone()
        : CategoryIcon(Enum.TryParse(icon, out CategoryIconEnum categoryIcon) ? categoryIcon : CategoryIconEnum.Uncategorized);

    private static Icon CategoryIcon(CategoryIconEnum icon) => icon switch
    {
        CategoryIconEnum.Auto => new Icons.Filled.Size20.VehicleCarProfile(),
        CategoryIconEnum.Bills => new Icons.Filled.Size20.Lightbulb(),
        CategoryIconEnum.Business => new Icons.Regular.Size20.Briefcase(),
        CategoryIconEnum.Education => new Icons.Regular.Size20.HatGraduation(),
        CategoryIconEnum.Entertainment => new Icons.Regular.Size20.MoviesAndTv(),
        CategoryIconEnum.Fees => new Icons.Regular.Size20.MoneyHand(),
        CategoryIconEnum.Financial => new Icons.Regular.Size20.BuildingBank(),
        CategoryIconEnum.Food => new Icons.Regular.Size20.Food(),
        CategoryIconEnum.Gifts => new Icons.Regular.Size20.Gift(),
        CategoryIconEnum.Health => new Icons.Regular.Size20.Doctor(),
        CategoryIconEnum.Home => new Icons.Regular.Size20.Home(),
        CategoryIconEnum.Income => new Icons.Filled.Size20.Money(),
        CategoryIconEnum.Investment => new Icons.Filled.Size20.ArrowTrendingLines(),
        CategoryIconEnum.Kids => new Icons.Filled.Size20.PersonRunning(),
        CategoryIconEnum.Loans => new Icons.Filled.Size20.Handshake(),
        CategoryIconEnum.Misc => new Icons.Filled.Size20.PuzzlePiece(),
        CategoryIconEnum.Personal => new Icons.Filled.Size20.PersonHeart(),
        CategoryIconEnum.Pets => new Icons.Filled.Size20.AnimalCat(),
        CategoryIconEnum.Shopping => new Icons.Filled.Size20.Cart(),
        CategoryIconEnum.Taxes => new Icons.Filled.Size20.DocumentPercent(),
        CategoryIconEnum.Transfer => new Icons.Filled.Size20.ArrowSwap(),
        CategoryIconEnum.Travel => new Icons.Filled.Size20.AirplaneTakeOff(),
        _ => new Icons.Regular.Size20.StackStar()
    };
}

public enum CategoryIconEnum
{
    Auto,
    Bills,
    Business,
    Education,
    Entertainment,
    Fees,
    Financial,
    Food,
    Gifts,
    Health,
    Home,
    Income,
    Investment,
    Kids,
    Loans,
    Misc,
    Personal,
    Pets,
    Shopping,
    Taxes,
    Transfer,
    Travel,
    Uncategorized
}