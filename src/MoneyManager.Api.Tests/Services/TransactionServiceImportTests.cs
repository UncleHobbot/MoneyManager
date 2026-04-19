using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class TransactionServiceImportTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly string _backupDir;

    public TransactionServiceImportTests()
    {
        _factory = DbContextHelper.CreateFactory();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _dataService = new DataService(_factory, _cache);

        // Create a temp backup directory for DBService
        _backupDir = Path.Combine(Path.GetTempPath(), $"mm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDir);

        // Seed a minimal account and category for import tests
        using var ctx = _factory.CreateDbContext();
        ctx.Accounts.Add(new Account { Name = "Chequing", ShownName = "Chequing", Type = 0, Number = "12345" });
        ctx.Categories.Add(new Category { Name = "Uncategorized", Icon = "Misc" });
        ctx.Categories.Add(new Category { Name = "Groceries" });
        ctx.SaveChanges();
    }

    public void Dispose()
    {
        _factory.Dispose();
        (_cache as IDisposable)?.Dispose();
        try { Directory.Delete(_backupDir, true); } catch { /* cleanup best effort */ }
    }

    private TransactionService CreateTransactionService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "BackupPath", _backupDir } })
            .Build();
        var dbService = new DBService(_factory, config);
        return new TransactionService(_factory, _dataService, dbService);
    }

    private static Stream ToStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    // --- Mint Import Tests ---

    [Fact]
    public async Task ImportMintCsvAsync_ImportsValidRecords()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee Shop,STARBUCKS #1234,5.50,debit,Food,Chequing,,
            1/16/2025,Salary,PAYROLL,3000.00,credit,Income,Chequing,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportMintCsvAsync(ToStream(csv));

        result.ImportedCount.Should().Be(2);
        result.BankType.Should().Be("Mint");
    }

    [Fact]
    public async Task ImportMintCsvAsync_SkipsDuplicates()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,Chequing,,
            """;

        var service = CreateTransactionService();

        // Import once
        await service.ImportMintCsvAsync(ToStream(csv));

        // Import again — should be skipped
        var result = await service.ImportMintCsvAsync(ToStream(csv));

        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportMintCsvAsync_CreatesNewAccount()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,NewAccount,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportMintCsvAsync(ToStream(csv), isCreateAccounts: true);

        result.ImportedCount.Should().Be(1);

        using var ctx = _factory.CreateDbContext();
        var account = await ctx.Accounts.FirstOrDefaultAsync(a => a.Name == "NewAccount");
        account.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportMintCsvAsync_SkipsWhenAccountNotFoundAndCreateDisabled()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,UnknownAccount,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportMintCsvAsync(ToStream(csv), isCreateAccounts: false);

        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportMintCsvAsync_SetsDebitFlag()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,Chequing,,
            1/16/2025,Refund,REFUND,10.00,credit,Food,Chequing,,
            """;

        var service = CreateTransactionService();
        await service.ImportMintCsvAsync(ToStream(csv));

        using var ctx = _factory.CreateDbContext();
        var debit = await ctx.Transactions.FirstAsync(t => t.OriginalDescription == "STARBUCKS");
        var credit = await ctx.Transactions.FirstAsync(t => t.OriginalDescription == "REFUND");

        debit.IsDebit.Should().BeTrue();
        credit.IsDebit.Should().BeFalse();
    }

    // --- RBC Import Tests ---

    [Fact]
    public async Task ImportRbcCsvAsync_ImportsValidRecords()
    {
        var csv = """
            Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$
            Chequing,12345,1/20/2025,,GROCERY STORE,LOCATION,-45.00,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportRbcCsvAsync(ToStream(csv));

        result.ImportedCount.Should().Be(1);
        result.BankType.Should().Be("RBC");
    }

    [Fact]
    public async Task ImportRbcCsvAsync_NegativeAmountIsDebit()
    {
        var csv = """
            Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$
            Chequing,12345,1/20/2025,,PURCHASE,STORE,-25.00,
            Chequing,12345,1/21/2025,,DEPOSIT,,100.00,
            """;

        var service = CreateTransactionService();
        await service.ImportRbcCsvAsync(ToStream(csv));

        using var ctx = _factory.CreateDbContext();
        var transactions = await ctx.Transactions.ToListAsync();
        var purchase = transactions.First(t => t.OriginalDescription.Contains("PURCHASE"));
        var deposit = transactions.First(t => t.OriginalDescription.Contains("DEPOSIT"));

        purchase.IsDebit.Should().BeTrue();
        purchase.Amount.Should().Be(25.00m);
        deposit.IsDebit.Should().BeFalse();
        deposit.Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task ImportRbcCsvAsync_ThrowsOnInvalidCsvStructure()
    {
        var csv = """
            Bad,Header,Row
            1,2,3
            """;

        var service = CreateTransactionService();
        var act = () => service.ImportRbcCsvAsync(ToStream(csv));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Missing column*");
    }

    // --- CIBC Import Tests ---

    [Fact]
    public async Task ImportCibcCsvAsync_ImportsValidRecords()
    {
        var csv = "1/25/2025,GROCERY STORE,30.00,,12345\n1/26/2025,SALARY,,2000.00,12345\n";

        var service = CreateTransactionService();
        var result = await service.ImportCibcCsvAsync(ToStream(csv));

        result.ImportedCount.Should().Be(2);
        result.BankType.Should().Be("CIBC");
    }

    [Fact]
    public async Task ImportCibcCsvAsync_DebitAndCreditLogic()
    {
        var csv = "1/25/2025,PURCHASE,50.00,,12345\n1/26/2025,REFUND,,25.00,12345\n";

        var service = CreateTransactionService();
        await service.ImportCibcCsvAsync(ToStream(csv));

        using var ctx = _factory.CreateDbContext();
        var transactions = await ctx.Transactions.ToListAsync();
        var purchase = transactions.First(t => t.Description == "PURCHASE");
        var refund = transactions.First(t => t.Description == "REFUND");

        purchase.IsDebit.Should().BeTrue();
        purchase.Amount.Should().Be(50.00m);
        refund.IsDebit.Should().BeFalse();
        refund.Amount.Should().Be(25.00m);
    }
}
