using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MoneyManager.Data;

public class DataContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseSqlite(@"Data Source=c:\Projects\MoneyManager\Data\MoneyManager.db")
        .AddInterceptors([new MMQueryInterceptor(), new MMSaveChangeInterceptor()]);

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Balance> Balances { get; set; }
    public DbSet<Rule> Rules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().ToTable("Categories")
            .Property(f => f.Id).ValueGeneratedOnAdd();
    }
}

public class MMQueryInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Console.WriteLine(command.CommandText);
        return result;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        Console.WriteLine(command.CommandText);
        base.CommandFailed(command, eventData);
    }
}

public class MMSaveChangeInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
    {
        var q = eventData.Context.ChangeTracker.DebugView.LongView;
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}