namespace MoneyManager.Data;

public class Seed(IDbContextFactory<DataContext> contextFactory)
{
    public async Task SeedData()
    {
        var context = await contextFactory.CreateDbContextAsync();

        var categories = new HashSet<Category>();
        var id = 1;
        var cAuto = new Category { Id = id++, Name = "Auto & Transport", Icon = nameof(Icons.Car) };
        categories.Add(cAuto);
        categories.Add(new Category { Id = id++, Name = "Auto Insurance", Parent = cAuto });
        categories.Add(new Category { Id = id++, Name = "Auto Payment", Parent = cAuto });
        categories.Add(new Category { Id = id++, Name = "Gas & Fuel", Parent = cAuto });
        categories.Add(new Category { Id = id++, Name = "Parking", Parent = cAuto });
        categories.Add(new Category { Id = id++, Name = "Service & Parts", Parent = cAuto });
        var cBills = new Category { Id = id++, Name = "Bills & Utilities", Icon = nameof(Icons.Bill) };
        categories.Add(cBills);
        categories.Add(new Category { Id = id++, Name = "Internet", Parent = cBills });
        categories.Add(new Category { Id = id++, Name = "Mobile Phone", Parent = cBills });
        categories.Add(new Category { Id = id++, Name = "Utilities", Parent = cBills });
        categories.Add(new Category { Id = id++, Name = "HydroQuebec", Parent = cBills });
        var cIncome = new Category { Id = id++, Name = "Income", Icon = nameof(Icons.Income) };
        categories.Add(new Category { Id = id++, Name = "Paycheck", Parent = cIncome });
        categories.Add(new Category { Id = id++, Name = "Interest Income", Parent = cIncome });
        categories.Add(new Category { Id = id++, Name = "Bonus", Parent = cIncome });
        categories.Add(new Category { Id = id++, Name = "Reimbursement", Parent = cIncome });
        categories.Add(cIncome);

        foreach (var category in categories)
            if (!context.Categories.Any(c => c.Name == category.Name))
                await context.Categories.AddAsync(category);

        await context.SaveChangesAsync();
    }

}