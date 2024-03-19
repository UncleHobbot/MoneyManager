using Icon = Microsoft.FluentUI.AspNetCore.Components.Icon;

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

public class CategoryTree
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
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

    public static Icon CategoryIcon(string icon) => string.IsNullOrWhiteSpace(icon)
        ? new Icons.Regular.Size20.BorderNone()
        : CategoryIcon(Enum.TryParse(icon, out CategoryIconEnum categoryIcon) ? categoryIcon : CategoryIconEnum.Uncategorized);

    public static Icon CategoryIcon(CategoryIconEnum icon) => icon switch
    {
        CategoryIconEnum.Auto => new Icons.Regular.Size20.VehicleCarProfile(),
        CategoryIconEnum.Bills => new Icons.Regular.Size20.Lightbulb(),
        CategoryIconEnum.Business => new Icons.Regular.Size20.Briefcase(),
        CategoryIconEnum.Education => new Icons.Regular.Size20.HatGraduation(),
        CategoryIconEnum.Entertainment => new Icons.Regular.Size20.MoviesAndTv(),
        CategoryIconEnum.Fees => new Icons.Regular.Size20.MoneyHand(),
        CategoryIconEnum.Financial => new Icons.Regular.Size20.BuildingBank(),
        CategoryIconEnum.Food => new Icons.Regular.Size20.Food(),
        CategoryIconEnum.Gifts => new Icons.Regular.Size20.Gift(),
        CategoryIconEnum.Health => new Icons.Regular.Size20.Doctor(),
        CategoryIconEnum.Home => new Icons.Regular.Size20.Home(),
        CategoryIconEnum.Income => new Icons.Regular.Size20.Money(),
        CategoryIconEnum.Investment => new Icons.Regular.Size20.ArrowTrendingLines(),
        CategoryIconEnum.Kids => new Icons.Regular.Size20.PersonRunning(),
        CategoryIconEnum.Loans => new Icons.Regular.Size20.Handshake(),
        CategoryIconEnum.Misc => new Icons.Regular.Size20.PuzzlePiece(),
        CategoryIconEnum.Personal => new Icons.Filled.Size20.PersonHeart(),
        CategoryIconEnum.Pets => new Icons.Regular.Size20.AnimalCat(),
        CategoryIconEnum.Shopping => new Icons.Regular.Size20.Cart(),
        CategoryIconEnum.Taxes => new Icons.Regular.Size20.DocumentPercent(),
        CategoryIconEnum.Transfer => new Icons.Filled.Size24.ArrowSwap(),
        CategoryIconEnum.Travel => new Icons.Regular.Size20.AirplaneTakeOff(),
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