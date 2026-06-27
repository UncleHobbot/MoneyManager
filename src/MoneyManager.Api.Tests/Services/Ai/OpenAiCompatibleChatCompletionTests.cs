using System.Net;
using FluentAssertions;
using MoneyManager.Api.Services.Ai;

namespace MoneyManager.Api.Tests.Services.Ai;

/// <summary>
/// Covers the transport adapter via a stub <see cref="HttpMessageHandler"/>:
/// the per-request auth header (the race fix), the absolute endpoint, response
/// parsing, and the "never throws" contract on non-2xx and transport failures.
/// </summary>
public class OpenAiCompatibleChatCompletionTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request;
        public string? Body;
        public Exception? ThrowOnSend;
        public HttpResponseMessage Response = new(HttpStatusCode.OK) { Content = new StringContent("{}") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (request.Content is not null)
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (ThrowOnSend is not null)
                throw ThrowOnSend;
            return Response;
        }
    }

    private static ChatRequest SampleRequest(string? data = null) =>
        new("https://api.example.com/v1/chat/completions", "sk-key", "test-model", "sys", "usr", data, 0.5);

    private static HttpResponseMessage Ok(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    [Fact]
    public async Task Complete_SetsBearerOnRequest_NotOnSharedClient()
    {
        var stub = new StubHandler { Response = Ok("""{"choices":[{"message":{"content":"x"}}],"usage":{"total_tokens":1}}""") };
        var client = new HttpClient(stub);
        var sut = new OpenAiCompatibleChatCompletion(client);

        await sut.CompleteAsync(SampleRequest());

        stub.Request!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        stub.Request.Headers.Authorization.Parameter.Should().Be("sk-key");
        // The fix: the key lives on the request, never on the shared client.
        client.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Complete_PostsToAbsoluteEndpoint_WithModelAndMessages()
    {
        var stub = new StubHandler { Response = Ok("""{"choices":[{"message":{"content":"x"}}],"usage":{"total_tokens":1}}""") };
        var sut = new OpenAiCompatibleChatCompletion(new HttpClient(stub));

        await sut.CompleteAsync(SampleRequest(data: "date,amount\n2025-01-01,10"));

        stub.Request!.Method.Should().Be(HttpMethod.Post);
        stub.Request.RequestUri.Should().Be(new Uri("https://api.example.com/v1/chat/completions"));
        stub.Body.Should().Contain("\"model\":\"test-model\"");
        stub.Body.Should().Contain("system");
        stub.Body.Should().Contain("usr");
        stub.Body.Should().Contain("Analyze the following CSV data");
    }

    [Fact]
    public async Task Complete_Success_ParsesContentAndTokens()
    {
        var stub = new StubHandler
        {
            Response = Ok("""{"choices":[{"message":{"content":"hello"}}],"usage":{"total_tokens":42}}"""),
        };
        var sut = new OpenAiCompatibleChatCompletion(new HttpClient(stub));

        var result = await sut.CompleteAsync(SampleRequest());

        result.Success.Should().BeTrue();
        result.Content.Should().Be("hello");
        result.TotalTokens.Should().Be(42);
    }

    [Fact]
    public async Task Complete_NonSuccessStatus_ReturnsFailureWithBody_DoesNotThrow()
    {
        var stub = new StubHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("upstream error"),
            },
        };
        var sut = new OpenAiCompatibleChatCompletion(new HttpClient(stub));

        var result = await sut.CompleteAsync(SampleRequest());

        result.Success.Should().BeFalse();
        result.Content.Should().Be("upstream error");
        result.TotalTokens.Should().Be(0);
    }

    [Fact]
    public async Task Complete_TransportThrows_ReturnsFailure_DoesNotThrow()
    {
        var stub = new StubHandler { ThrowOnSend = new HttpRequestException("connection refused") };
        var sut = new OpenAiCompatibleChatCompletion(new HttpClient(stub));

        var result = await sut.CompleteAsync(SampleRequest());

        result.Success.Should().BeFalse();
        result.Content.Should().Be("connection refused");
        result.TotalTokens.Should().Be(0);
    }
}
