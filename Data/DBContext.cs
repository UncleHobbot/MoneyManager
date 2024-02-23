namespace MoneyManager.Data;

public class DataContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        => optionsBuilder.UseSqlite(@"Data Source=c:\Projects\MoneyManager\Data\MoneyManager.db");

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