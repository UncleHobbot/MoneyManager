using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;
using MoneyManager.Api.Services.Ai;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

/// <summary>
/// Exercises the domain logic of <see cref="AIService"/> — prompt assembly,
/// provider resolution, and result mapping — against a fake
/// <see cref="IChatCompletion"/>. The transport itself is covered separately by
/// <see cref="OpenAiCompatibleChatCompletionTests"/>.
/// </summary>
public class AIServiceTests
{
    private sealed class FakeChatCompletion : IChatCompletion
    {
        public ChatRequest? Captured;
        public int Calls;
        public ChatResult Result = new(true, "analysis text", 123);

        public Task<ChatResult> CompleteAsync(ChatRequest request)
        {
            Captured = request;
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private static AiProvider SeedProvider(
        TestDbContextFactory factory,
        string url,
        string key,
        string model,
        bool isDefault)
    {
        using var ctx = factory.CreateDbContext();
        var provider = new AiProvider
        {
            Name = isDefault ? "Default" : "Secondary",
            ProviderType = "OpenAI",
            ApiUrl = url,
            EncryptedApiKey = key,
            Model = model,
            IsDefault = isDefault,
        };
        ctx.AiProviders.Add(provider);
        ctx.SaveChanges();
        return provider;
    }

    [Fact]
    public async Task GetAnalysis_AssemblesRequestFromProviderAndAnalysisType()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        SeedProvider(bundle.Factory, "https://api.deepseek.com/chat/completions", "sk-test", "deepseek-chat", isDefault: true);
        var fake = new FakeChatCompletion();
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        var expectedType = MoneyManager.Api.Model.AI.AnalysisType.Find("DebtAnalysis")!;
        var expectedCsv = await bundle.DataService.AIGetTransactionsCSVAsync("a");

        var result = await service.GetAnalysisAsync("a", "DebtAnalysis");

        fake.Captured.Should().NotBeNull();
        fake.Captured!.Endpoint.Should().Be("https://api.deepseek.com/chat/completions");
        fake.Captured.ApiKey.Should().Be("sk-test");
        fake.Captured.Model.Should().Be("deepseek-chat");
        fake.Captured.UserPrompt.Should().Be(expectedType.Prompt);
        fake.Captured.Temperature.Should().Be(expectedType.Temperature);
        fake.Captured.SystemPrompt.Should().Contain("certified financial advisor");
        fake.Captured.Data.Should().Be(expectedCsv);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetAnalysis_UnknownType_UsesEmptyPromptAndDefaultTemperature()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        SeedProvider(bundle.Factory, "https://x/chat", "k", "m", isDefault: true);
        var fake = new FakeChatCompletion();
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        await service.GetAnalysisAsync("a", "NotARealAnalysisType");

        fake.Captured!.UserPrompt.Should().BeEmpty();
        fake.Captured.Temperature.Should().Be(0.7);
    }

    [Fact]
    public async Task GetAnalysis_NoProvider_ReturnsFailureAndSkipsCompletion()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        var fake = new FakeChatCompletion();
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        var result = await service.GetAnalysisAsync("a", "DebtAnalysis");

        result.IsSuccess.Should().BeFalse();
        result.Result.Should().Contain("No AI provider configured");
        fake.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GetAnalysis_Success_MapsContentAndTokens()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        SeedProvider(bundle.Factory, "https://x/chat", "k", "m", isDefault: true);
        var fake = new FakeChatCompletion { Result = new ChatResult(true, "answer", 123) };
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        var result = await service.GetAnalysisAsync("a", "DebtAnalysis");

        result.IsSuccess.Should().BeTrue();
        result.Result.Should().Be("answer");
        result.TotalTokens.Should().Be(123);
    }

    [Fact]
    public async Task GetAnalysis_TransportFailure_MapsToFailure()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        SeedProvider(bundle.Factory, "https://x/chat", "k", "m", isDefault: true);
        var fake = new FakeChatCompletion { Result = new ChatResult(false, "boom", 0) };
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        var result = await service.GetAnalysisAsync("a", "DebtAnalysis");

        result.IsSuccess.Should().BeFalse();
        result.Result.Should().Be("boom");
        result.TotalTokens.Should().Be(0);
    }

    [Fact]
    public async Task GetAnalysis_WithProviderId_SelectsThatProvider()
    {
        using var bundle = DbContextHelper.CreateServiceBundle();
        SeedProvider(bundle.Factory, "https://default/chat", "k1", "m1", isDefault: true);
        var secondary = SeedProvider(bundle.Factory, "https://secondary/chat", "k2", "m2", isDefault: false);
        var fake = new FakeChatCompletion();
        var service = new AIService(new AiProviderService(bundle.Factory), bundle.DataService, fake);

        await service.GetAnalysisAsync("a", "DebtAnalysis", providerId: secondary.Id);

        fake.Captured!.Endpoint.Should().Be("https://secondary/chat");
        fake.Captured.ApiKey.Should().Be("k2");
        fake.Captured.Model.Should().Be("m2");
    }
}
