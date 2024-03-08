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
    public HashSet<CategoryTree> Children { get; set; }
}

public class CategoryDropItem
{
    public CategoryDropItem() { }

    public CategoryDropItem(Category c, string parentId)
    {
        Id = c.Id;
        Name = c.Name;
        ParentCategory = parentId;
    }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string ParentCategory { get; set; }
}