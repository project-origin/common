using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;
using ProjectOrigin.ServiceCommon.UriOptionsLoader;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.AspNetCore.Hosting;
using FluentAssertions;
using ProjectOrigin.TestCommon;
using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.Tests;

public class UriOptionsLoaderTests
{
    private const string ConfigurationSection = "SomeSection";
    private const string Scenario = "MyScenario";
    private const string SecondCallState = "Second Call";

    [Fact]
    public async Task WhenChanged_MonitorCalled()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.json";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:00:10" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .InScenario(Scenario)
            .WillSetStateTo(SecondCallState)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeValue",
                }));

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .InScenario(Scenario)
            .WhenStateIs(SecondCallState)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeOtherValue",
                }));


        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener<TestOption>>();

        TestOption currentOption = listener.MonitorOption.CurrentValue;
        TestOption? newOption = null;
        var taskCompletionSource = new TaskCompletionSource<bool>();

        listener.MonitorOption.OnChange((options, name) =>
        {
            newOption = options;
            taskCompletionSource.SetResult(true);
        });

        await taskCompletionSource.Task;

        Assert.NotEqual(currentOption, newOption);
    }

    [Fact]
    public async Task WhenNotChanged_MonitorNotCalled()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.json";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:00:05" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    SomeKey = "SomeValue",
                }));

        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener<TestOption>>();
        bool called = false;

        listener.MonitorOption.OnChange((options, name) =>
        {
            called = true;
        });

        await Task.Delay(13000);
        _networkMockServer.LogEntries.Should().HaveCount(3);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task CanLoadJson()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.json";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:15:00" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption2>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption2>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        Name = "Mike",
                        Size = 10,
                        Dictionary = new
                        {
                            key1 = new
                            {
                                SomeKey = "bla1"
                            },
                            key2 = new
                            {
                                SomeKey = "bla2"
                            }
                        }
                    }));

        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener<TestOption2>>();
        listener.MonitorOption.CurrentValue.Name.Should().Be("Mike");
        listener.MonitorOption.CurrentValue.Size.Should().Be(10);
        listener.MonitorOption.CurrentValue.Dictionary.Should().HaveCount(2);
        listener.MonitorOption.CurrentValue.Dictionary["key1"].SomeKey.Should().Be("bla1");
        listener.MonitorOption.CurrentValue.Dictionary["key2"].SomeKey.Should().Be("bla2");
    }

    [Fact]
    public async Task CanLoadYaml()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.yaml";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:15:00" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption2>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption2>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("""
                name: "Mike"
                size: 10
                dictionary:
                  key1:
                    someKey: "bla1"
                  key2:
                    someKey: "bla2"
                """));

        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener<TestOption2>>();
        listener.MonitorOption.CurrentValue.Name.Should().Be("Mike");
        listener.MonitorOption.CurrentValue.Size.Should().Be(10);
        listener.MonitorOption.CurrentValue.Dictionary.Should().HaveCount(2);
        listener.MonitorOption.CurrentValue.Dictionary["key1"].SomeKey.Should().Be("bla1");
        listener.MonitorOption.CurrentValue.Dictionary["key2"].SomeKey.Should().Be("bla2");
    }

    [Fact]
    public async Task CanLoadYamlFromFile()
    {
        var yaml = """
                name: "Mike"
                size: 10
                dictionary:
                  key1:
                    someKey: "bla1"
                  key2:
                    someKey: "bla2"
                """;

        var path = TempFile.WriteAllText(yaml, ".yaml");

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  "file://" + path },
                    { $"{ConfigurationSection}:RefreshInterval", "00:15:00" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption2>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption2>>();
        });

        using var host = _builder.Build();
        await Task.Run(host.Start);

        var listener = host.Services.GetRequiredService<NetworkOptionsChangeListener<TestOption2>>();
        listener.MonitorOption.CurrentValue.Name.Should().Be("Mike");
        listener.MonitorOption.CurrentValue.Size.Should().Be(10);
        listener.MonitorOption.CurrentValue.Dictionary.Should().HaveCount(2);
        listener.MonitorOption.CurrentValue.Dictionary["key1"].SomeKey.Should().Be("bla1");
        listener.MonitorOption.CurrentValue.Dictionary["key2"].SomeKey.Should().Be("bla2");
    }

    [Fact]
    public async Task NotSupportedFormat_ThrowsException()
    {
        // Arrange
        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  "some://hello.com" },
                    { $"{ConfigurationSection}:RefreshInterval", "00:00:05" },
                }
            }));


        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption>(ConfigurationSection);
        });

        using var host = _builder.Build();

        // Act
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => Task.Run(host.Start));

        // Assert
        exception.Message.Should().Be("Unsupported URI scheme: some");
    }

    [Fact]
    public async Task VerifyAttribute_ThrowsException()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.json";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:15:00" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption2>(ConfigurationSection);
            services.AddSingleton<NetworkOptionsChangeListener<TestOption2>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        Name = "Mike",
                        Size = 0,
                        Dictionary = new { }
                    }));

        using var host = _builder.Build();
        var task = () => Task.Run(host.Start);

        await task.Should().ThrowAsync<OptionsValidationException>().WithMessage("The field Size must be between 1 and 100.");
    }

    [Fact]
    public async Task VerifyVerifier_ThrowsException()
    {
        var _networkMockServer = WireMockServer.Start();
        var testPath = "/TestPath.json";

        var _builder = new HostBuilder();
        _builder.ConfigureHostConfiguration(config =>
            config.Add(new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    { $"{ConfigurationSection}:ConfigurationUri",  _networkMockServer.Urls[0] + testPath },
                    { $"{ConfigurationSection}:RefreshInterval", "00:15:00" },
                }
            }));

        _builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.ConfigureUriOptionsLoader<TestOption2>(ConfigurationSection);
            services.AddSingleton<IValidateOptions<TestOption2>, TestValidator>();
            services.AddSingleton<NetworkOptionsChangeListener<TestOption2>>();
        });

        _networkMockServer.Given(Request.Create().WithPath(testPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        Name = "Mike",
                        Size = 75,
                        Dictionary = new { }
                    }));

        using var host = _builder.Build();
        var task = () => Task.Run(host.Start);

        await task.Should().ThrowAsync<OptionsValidationException>().WithMessage("Size must be less than 50.");
    }

    internal class NetworkOptionsChangeListener<TOption>
    {
        public IOptionsMonitor<TOption> MonitorOption { get; }

        public NetworkOptionsChangeListener(IOptionsMonitor<TOption> optionsMonitor)
        {
            MonitorOption = optionsMonitor;
        }
    }

    internal record TestOption
    {
        public required string SomeKey { get; init; }
    }

    internal record TestOption2
    {
        public required string Name { get; init; }
        [Required, Range(1, 100)]
        public required int Size { get; init; }
        public required Dictionary<string, TestOption> Dictionary { get; init; }
    }

    internal class TestValidator : IValidateOptions<TestOption2>
    {
        public ValidateOptionsResult Validate(string? name, TestOption2 options)
        {
            if (options.Size > 50)
                return ValidateOptionsResult.Fail("Size must be less than 50.");

            return ValidateOptionsResult.Success;
        }
    }
}
