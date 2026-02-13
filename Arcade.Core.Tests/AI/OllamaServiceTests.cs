using System.Net;
using Arcade.Core.AI;
using Arcade.Core.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arcade.Core.Tests.AI;

[TestFixture]
public class OllamaServiceTests
{
    private AiOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new AiOptions
        {
            OllamaEndpoint = "http://localhost:11434",
            OllamaDefaultModel = "llama3.2",
            AssistantMaxTokens = 150,
            AssistantTimeoutSeconds = 5
        };
    }

    [Test]
    public async Task GenerateAsync_SuccessfulResponse_ReturnsString()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"response\": \"Hello!\"}")
        });
        var service = CreateService(handler);

        var result = await service.GenerateAsync("system", "user");

        Assert.That(result, Is.EqualTo("Hello!"));
    }

    [Test]
    public async Task GenerateAsync_ErrorStatus_ReturnsNull()
    {
        var handler = new FakeHttpHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = CreateService(handler);

        var result = await service.GenerateAsync("system", "user");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GenerateAsync_Exception_ReturnsNull()
    {
        var handler = new FakeHttpHandler(new HttpRequestException("Connection refused"));
        var service = CreateService(handler);

        var result = await service.GenerateAsync("system", "user");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GenerateAsync_UsesSpecifiedModel()
    {
        string? capturedBody = null;
        var handler = new FakeHttpHandler(req =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"response\": \"ok\"}")
            };
        });
        var service = CreateService(handler);

        await service.GenerateAsync("system", "user", "custom-model");

        Assert.That(capturedBody, Does.Contain("custom-model"));
    }

    [Test]
    public async Task GenerateAsync_UsesDefaultModel_WhenNoneSpecified()
    {
        string? capturedBody = null;
        var handler = new FakeHttpHandler(req =>
        {
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"response\": \"ok\"}")
            };
        });
        var service = CreateService(handler);

        await service.GenerateAsync("system", "user");

        Assert.That(capturedBody, Does.Contain("llama3.2"));
    }

    private OllamaService CreateService(DelegatingHandler handler)
    {
        handler.InnerHandler = new HttpClientHandler();
        var client = new HttpClient(handler);
        return new OllamaService(client, Options.Create(_options), NullLogger<OllamaService>.Instance);
    }

    private class FakeHttpHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
        private readonly HttpResponseMessage? _fixedResponse;
        private readonly Exception? _exception;

        public FakeHttpHandler(HttpResponseMessage response) => _fixedResponse = response;
        public FakeHttpHandler(Exception exception) => _exception = exception;
        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception != null) throw _exception;
            if (_handler != null) return Task.FromResult(_handler(request));
            return Task.FromResult(_fixedResponse!);
        }
    }
}
