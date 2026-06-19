using KnowledgeOps.Application.Chat;
using KnowledgeOps.Infrastructure;
using KnowledgeOps.Infrastructure.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeOps.IntegrationTests;

public sealed class AiProviderDiTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IServiceProvider BuildProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        // IConfiguration is needed by OpenAIAnswerGenerator constructor and by BindConfiguration
        services.AddSingleton<IConfiguration>(config);
        services.AddAiAnswerInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddAiAnswerInfrastructure_DefaultsToDemo_WhenProviderNotConfigured()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var provider = BuildProvider(config);

        var generator = provider.CreateScope().ServiceProvider
            .GetRequiredService<IAiAnswerGenerator>();

        Assert.IsType<DemoGroundedAnswerGenerator>(generator);
        Assert.Equal("Demo", generator.ProviderName);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_RegistersDemo_WhenProviderIsDemo()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "Demo"
        });
        var provider = BuildProvider(config);

        var generator = provider.CreateScope().ServiceProvider
            .GetRequiredService<IAiAnswerGenerator>();

        Assert.IsType<DemoGroundedAnswerGenerator>(generator);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_RegistersOpenAI_WhenProviderIsOpenAIAndKeyPresent()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "OpenAI",
            ["Ai:OpenAI:ApiKey"] = "sk-test-key"
        });
        var provider = BuildProvider(config);

        var generator = provider.CreateScope().ServiceProvider
            .GetRequiredService<IAiAnswerGenerator>();

        Assert.IsType<OpenAIAnswerGenerator>(generator);
        Assert.Equal("OpenAI", generator.ProviderName);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_ThrowsAtStartup_WhenProviderIsOpenAIButKeyMissing()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "OpenAI"
            // No Ai:OpenAI:ApiKey
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddAiAnswerInfrastructure(config);
        });

        // Safe message: must not contain key values; must be actionable
        Assert.Contains("Ai:OpenAI:ApiKey", ex.Message);
        Assert.Contains("user-secrets", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_DoesNotThrow_WhenDemoAndKeyMissing()
    {
        // Missing OpenAI key must not fail when provider is Demo
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "Demo"
        });

        // Should not throw
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddAiAnswerInfrastructure(config);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_ThrowsAtStartup_WhenProviderIsUnknown()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "GPT-5-Fantasy"
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddAiAnswerInfrastructure(config);
        });

        Assert.Contains("GPT-5-Fantasy", ex.Message);
        Assert.Contains("Demo", ex.Message);
        Assert.Contains("OpenAI", ex.Message);
        Assert.Contains("LocalOpenAICompatible", ex.Message);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_RegistersLocalOpenAICompatible_WhenConfigured()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "LocalOpenAICompatible",
            ["Ai:LocalOpenAICompatible:BaseUrl"] = "http://localhost:11434/v1"
        });
        var provider = BuildProvider(config);

        var generator = provider.CreateScope().ServiceProvider
            .GetRequiredService<IAiAnswerGenerator>();

        Assert.IsType<LocalOpenAICompatibleAnswerGenerator>(generator);
        Assert.Equal("QwenLocal", generator.ProviderName);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_ThrowsAtStartup_WhenLocalBaseUrlMissing()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "LocalOpenAICompatible"
            // No Ai:LocalOpenAICompatible:BaseUrl
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddAiAnswerInfrastructure(config);
        });

        Assert.Contains("BaseUrl", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LocalOpenAICompatible", ex.Message);
    }

    [Fact]
    public void AddAiAnswerInfrastructure_DoesNotRequireApiKey_ForLocalOpenAICompatible()
    {
        // Local provider (Ollama) does not require an API key — must not fail startup.
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Ai:AnswerProvider"] = "LocalOpenAICompatible",
            ["Ai:LocalOpenAICompatible:BaseUrl"] = "http://localhost:11434/v1"
            // No ApiKey
        });

        // Should not throw
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(config);
        services.AddAiAnswerInfrastructure(config);
    }
}
