using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Import;
using MoneyManager.Api.Services;
using MoneyManager.Api.Services.Import;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services.Import;

/// <summary>
/// Pipeline-level tests for <see cref="TransactionService.ImportAsync"/>.
/// Uses a <see cref="FakeImporter"/> to control rows independently of CSV
/// parsing - the pipeline's account/category resolution, dedup, and rule
/// routing are exercised in isolation from CsvHelper.
/// </summary>
public class TransactionServicePipelineTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly DataService _dataService;
    private readonly IMemoryCache _cache;
    private readonly string _backupDir;

    public TransactionServicePipelineTests()
    {
        _factory = DbContextHelper.CreateFactory();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _dataService = new DataService(_factory, _cache, new TransactionQueryService(_factory));

        _backupDir = Path.Combine(Path.GetTempPath(), $"mm_pipeline_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDir);

        using var ctx = _factory.CreateDbContext();
        ctx.Accounts.Add(new Account { Name = "Chequing", ShownName = "Chequing", Type = 0, Number = "12345" });
        ctx.Categories.Add(new Category { Name = "Uncategorized", Icon = "Misc" });
        ctx.Categories.Add(new Category { Name = "Groceries", Icon = "Food" });
        ctx.Categories.Add(new Category { Name = "Income", Icon = "Income" });
        ctx.SaveChanges();
    }

    public void Dispose()
    {
        _factory.Dispose();
        (_cache as IDisposable)?.Dispose();
        try { Directory.Delete(_backupDir, true); } catch { /* best effort */ }
    }

    private TransactionService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "BackupPath", _backupDir } })
            .Build();
        var dbService = new DBService(_factory, config);
        return new TransactionService(_factory, _dataService, dbService);
    }

    private static Stream ToStream(string content = "header\n") =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    // ----------------------------------------------------------------
    // Empty / single-row baseline
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_WithNoRows_ReturnsZeroCounts_AndStillBacksUp()
    {
        var service = CreateService();
        var importer = new FakeImporter { Rows = [] };

        var result = await service.ImportAsync(ToStream(""), importer);

        result.ImportedCount.Should().Be(0);
        result.TotalCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.BankType.Should().Be("Fake");

        // Backup happens regardless of row count.
        Directory.GetFiles(_backupDir, "*.db").Should().HaveCount(1);
    }

    [Fact]
    public async Task ImportAsync_WithOneValidRow_ImportsOne()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(date: new DateTime(2025, 1, 15), amount: 50m, isDebit: true, account: "Chequing")],
        };

        var result = await service.ImportAsync(ToStream(), importer);

        result.ImportedCount.Should().Be(1);
        result.BankType.Should().Be("Fake");

        using var ctx = _factory.CreateDbContext();
        ctx.Transactions.Should().ContainSingle(t => t.Description == "test");
    }

    // ----------------------------------------------------------------
    // Account resolution
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_CreatesUnknownAccount_WhenCreateAccountsTrue()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(account: "BrandNewAccount")],
        };

        await service.ImportAsync(ToStream(), importer, isCreateAccounts: true);

        using var ctx = _factory.CreateDbContext();
        ctx.Accounts.Should().Contain(a => a.Name == "BrandNewAccount");
    }

    [Fact]
    public async Task ImportAsync_SkipsRow_WhenAccountUnknownAndCreateDisabled()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(account: "NonExistent")],
        };

        var result = await service.ImportAsync(ToStream(), importer, isCreateAccounts: false);

        result.ImportedCount.Should().Be(0);
        using var ctx = _factory.CreateDbContext();
        ctx.Transactions.Should().BeEmpty();
    }

    // ----------------------------------------------------------------
    // Category resolution
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_AssignsDefaultUncategorized_WhenCategoryHintIsNull()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(categoryHint: null)],
        };

        await service.ImportAsync(ToStream(), importer);

        using var ctx = _factory.CreateDbContext();
        var tx = ctx.Transactions.Include(t => t.Category).First();
        tx.Category.Should().NotBeNull();
        tx.Category!.Name.Should().Be("Uncategorized");
    }

    [Fact]
    public async Task ImportAsync_ResolvesCategoryByName_WhenCategoryHintProvided()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(categoryHint: "Groceries")],
        };

        await service.ImportAsync(ToStream(), importer);

        using var ctx = _factory.CreateDbContext();
        var tx = ctx.Transactions.Include(t => t.Category).First();
        tx.Category!.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task ImportAsync_CreatesCategory_WhenHintDoesNotExist()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            Rows = [Row(categoryHint: "NewCategory")],
        };

        await service.ImportAsync(ToStream(), importer);

        using var ctx = _factory.CreateDbContext();
        var tx = ctx.Transactions.Include(t => t.Category).First();
        tx.Category!.Name.Should().Be("NewCategory");
        tx.Category!.IsNew.Should().BeTrue("newly-created categories are flagged for user review");
    }

    // ----------------------------------------------------------------
    // Dedup
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_SkipsExactDateDuplicate()
    {
        var service = CreateService();
        var importer = new FakeImporter
        {
            UseFuzzyDateMatch = false,
            Rows = [Row(date: new DateTime(2025, 1, 15), originalDescription: "DUP")],
        };

        // First import succeeds.
        await service.ImportAsync(ToStream(), importer);

        // Second import with identical row is skipped.
        var result = await service.ImportAsync(ToStream(), importer);

        result.ImportedCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_SkipsFuzzyDateDuplicate_WithinFiveDayWindow()
    {
        var service = CreateService();
        // First import with exact date.
        await service.ImportAsync(ToStream(), new FakeImporter
        {
            UseFuzzyDateMatch = true,
            Rows = [Row(date: new DateTime(2025, 1, 15), originalDescription: "DUP")],
        });

        // Second import with date 3 days later, fuzzy match should catch it.
        var result = await service.ImportAsync(ToStream(), new FakeImporter
        {
            UseFuzzyDateMatch = true,
            Rows = [Row(date: new DateTime(2025, 1, 18), originalDescription: "DUP")],
        });

        result.ImportedCount.Should().Be(0, "fuzzy match within ±5 days should detect the duplicate");
    }

    [Fact]
    public async Task ImportAsync_DoesNotFuzzyMatch_WhenUseFuzzyDateMatchFalse()
    {
        var service = CreateService();
        await service.ImportAsync(ToStream(), new FakeImporter
        {
            UseFuzzyDateMatch = false,
            Rows = [Row(date: new DateTime(2025, 1, 15), originalDescription: "DUP")],
        });

        // Same description, different date, fuzzy disabled - should import.
        var result = await service.ImportAsync(ToStream(), new FakeImporter
        {
            UseFuzzyDateMatch = false,
            Rows = [Row(date: new DateTime(2025, 1, 18), originalDescription: "DUP")],
        });

        result.ImportedCount.Should().Be(1);
    }

    // ----------------------------------------------------------------
    // Rule application
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_AppliesRules_WhenAdapterEnablesIt()
    {
        // Seed a rule that matches the row's original description.
        using (var seedCtx = _factory.CreateDbContext())
        {
            var groceries = seedCtx.Categories.First(c => c.Name == "Groceries");
            seedCtx.Rules.Add(new Rule
            {
                OriginalDescription = "COFFEE",
                NewDescription = "Coffee Shop",
                CompareType = RuleCompareType.Contains,
                Category = groceries,
            });
            seedCtx.SaveChanges();
        }

        var service = CreateService();
        var importer = new FakeImporter
        {
            ApplyRules = true,
            Rows = [Row(originalDescription: "STARBUCKS COFFEE #1234", description: "STARBUCKS COFFEE #1234")],
        };

        await service.ImportAsync(ToStream(), importer);

        using var ctx = _factory.CreateDbContext();
        var tx = ctx.Transactions.Include(t => t.Category).First();
        tx.Description.Should().Be("Coffee Shop", "rule should rewrite description");
        tx.Category!.Name.Should().Be("Groceries", "rule should assign category");
        tx.IsRuleApplied.Should().BeTrue();
    }

    [Fact]
    public async Task ImportAsync_DoesNotApplyRules_WhenAdapterDisablesIt()
    {
        using (var seedCtx = _factory.CreateDbContext())
        {
            var groceries = seedCtx.Categories.First(c => c.Name == "Groceries");
            seedCtx.Rules.Add(new Rule
            {
                OriginalDescription = "COFFEE",
                NewDescription = "Coffee Shop",
                CompareType = RuleCompareType.Contains,
                Category = groceries,
            });
            seedCtx.SaveChanges();
        }

        var service = CreateService();
        var importer = new FakeImporter
        {
            ApplyRules = false,
            Rows = [Row(originalDescription: "STARBUCKS COFFEE #1234", description: "STARBUCKS COFFEE #1234")],
        };

        await service.ImportAsync(ToStream(), importer);

        using var ctx = _factory.CreateDbContext();
        var tx = ctx.Transactions.First();
        tx.Description.Should().Be("STARBUCKS COFFEE #1234", "rule should NOT rewrite description");
        tx.IsRuleApplied.Should().BeFalse();
    }

    // ----------------------------------------------------------------
    // SkippedCount arithmetic
    // ----------------------------------------------------------------

    [Fact]
    public async Task ImportAsync_SkippedCount_IsTotalMinusImported()
    {
        // Stream has 2 lines (no header per FakeImporter.HasHeaderRecord=false).
        // Adapter yields 2 rows. One row's account is unknown, createAccounts=false.
        // Imported=1, Total=2, Skipped=2-1=1.
        var service = CreateService();
        var importer = new FakeImporter
        {
            HasHeaderRecord = false,
            Rows = [
                Row(account: "Chequing"),
                Row(account: "NonExistent"),
            ],
        };

        var result = await service.ImportAsync(ToStream("line1\nline2\n"), importer, isCreateAccounts: false);

        result.TotalCount.Should().Be(2);
        result.ImportedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static NormalizedRow Row(
        DateTime? date = null,
        decimal amount = 10m,
        bool isDebit = true,
        string description = "test",
        string? originalDescription = null,
        string account = "Chequing",
        string? categoryHint = null)
        => new(
            Date: date ?? new DateTime(2025, 1, 15),
            Amount: amount,
            IsDebit: isDebit,
            Description: description,
            OriginalDescription: originalDescription ?? description,
            AccountName: account,
            CategoryHint: categoryHint);

    private sealed class FakeImporter : IBankImporter
    {
        public string BankType { get; set; } = "Fake";
        public bool ApplyRules { get; set; } = false;
        public bool UseFuzzyDateMatch { get; set; } = false;
        public bool HasHeaderRecord { get; set; } = false;
        public IReadOnlyList<NormalizedRow> Rows { get; set; } = [];
        public bool WasValidated { get; private set; }

        public void Validate(Stream stream)
        {
            WasValidated = true;
            stream.Position = 0;
        }

        public IEnumerable<NormalizedRow> ReadRows(Stream stream)
        {
            WasValidated.Should().BeTrue("pipeline must call Validate before ReadRows");
            return Rows;
        }
    }
}
