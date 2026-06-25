using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;
using MoneyManager.Api.Services.Import;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

/// <summary>
/// Integration tests: real CSV file through the matching adapter through the
/// real pipeline. Per-bank parsing is covered by <c>MintImporterTests</c>,
/// <c>RbcImporterTests</c>, <c>CibcImporterTests</c>; pipeline mechanics by
/// <c>TransactionServicePipelineTests</c> (with a fake adapter). These tests
/// verify the wiring - that the right adapter feeds the right CSV into the
/// pipeline and produces the expected database state.
/// </summary>
public class TransactionServiceImportTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly CategorizationService _categorization;
    private readonly ReferenceDataCache _refCache;
    private readonly IMemoryCache _cache;

    public TransactionServiceImportTests()
    {
        _factory = DbContextHelper.CreateFactory();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _refCache = new ReferenceDataCache(_factory, _cache);
        _categorization = new CategorizationService(_factory);

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
    }

    private TransactionService CreateTransactionService()
        => new(_factory, _categorization, _refCache);

    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    // --- Mint Import Tests ---

    [Fact]
    public async Task MintImport_ImportsValidRecords()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee Shop,STARBUCKS #1234,5.50,debit,Food,Chequing,,
            1/16/2025,Salary,PAYROLL,3000.00,credit,Income,Chequing,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportAsync(ToStream(csv), new MintImporter());

        result.ImportedCount.Should().Be(2);
        result.BankType.Should().Be("Mint");
    }

    [Fact]
    public async Task MintImport_SkipsDuplicates()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,Chequing,,
            """;

        var service = CreateTransactionService();

        await service.ImportAsync(ToStream(csv), new MintImporter());
        var result = await service.ImportAsync(ToStream(csv), new MintImporter());

        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task MintImport_CreatesNewAccount()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,NewAccount,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportAsync(ToStream(csv), new MintImporter(), isCreateAccounts: true);

        result.ImportedCount.Should().Be(1);

        using var ctx = _factory.CreateDbContext();
        var account = await ctx.Accounts.FirstOrDefaultAsync(a => a.Name == "NewAccount");
        account.Should().NotBeNull();
    }

    [Fact]
    public async Task MintImport_SkipsWhenAccountNotFoundAndCreateDisabled()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,UnknownAccount,,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportAsync(ToStream(csv), new MintImporter(), isCreateAccounts: false);

        result.ImportedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task MintImport_SetsDebitFlag()
    {
        var csv = """
            Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
            1/15/2025,Coffee,STARBUCKS,5.50,debit,Food,Chequing,,
            1/16/2025,Refund,REFUND,10.00,credit,Food,Chequing,,
            """;

        var service = CreateTransactionService();
        await service.ImportAsync(ToStream(csv), new MintImporter());

        using var ctx = _factory.CreateDbContext();
        var debit = await ctx.Transactions.FirstAsync(t => t.OriginalDescription == "STARBUCKS");
        var credit = await ctx.Transactions.FirstAsync(t => t.OriginalDescription == "REFUND");

        debit.IsDebit.Should().BeTrue();
        credit.IsDebit.Should().BeFalse();
    }

    // --- RBC Import Tests ---

    [Fact]
    public async Task RbcImport_ImportsValidRecords()
    {
        var csv = """
            Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$
            Chequing,12345,1/20/2025,,GROCERY STORE,LOCATION,-45.00,
            """;

        var service = CreateTransactionService();
        var result = await service.ImportAsync(ToStream(csv), new RbcImporter());

        result.ImportedCount.Should().Be(1);
        result.BankType.Should().Be("RBC");
    }

    [Fact]
    public async Task RbcImport_NegativeAmountIsDebit()
    {
        var csv = """
            Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$
            Chequing,12345,1/20/2025,,PURCHASE,STORE,-25.00,
            Chequing,12345,1/21/2025,,DEPOSIT,,100.00,
            """;

        var service = CreateTransactionService();
        await service.ImportAsync(ToStream(csv), new RbcImporter());

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
    public async Task RbcImport_ThrowsOnInvalidCsvStructure()
    {
        var csv = """
            Bad,Header,Row
            1,2,3
            """;

        var service = CreateTransactionService();
        var act = () => service.ImportAsync(ToStream(csv), new RbcImporter());

        // The adapter throws during Validate; the pipeline propagates.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Missing column*");
    }

    // --- CIBC Import Tests ---

    [Fact]
    public async Task CibcImport_ImportsValidRecords()
    {
        var csv = "1/25/2025,GROCERY STORE,30.00,,12345\n1/26/2025,SALARY,,2000.00,12345\n";

        var service = CreateTransactionService();
        var result = await service.ImportAsync(ToStream(csv), new CibcImporter());

        result.ImportedCount.Should().Be(2);
        result.BankType.Should().Be("CIBC");
    }

    [Fact]
    public async Task CibcImport_DebitAndCreditLogic()
    {
        var csv = "1/25/2025,PURCHASE,50.00,,12345\n1/26/2025,REFUND,,25.00,12345\n";

        var service = CreateTransactionService();
        await service.ImportAsync(ToStream(csv), new CibcImporter());

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
