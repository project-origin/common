using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Npgsql.Replication;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public sealed class UriOptionsLoaderService<TOption> : BackgroundService, IDisposable where TOption : class
{
    private readonly ILogger<UriOptionsLoaderService<TOption>> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UriOptions _originOptions;
    private readonly PropertyInfo[] _optionProperties;
    private CancellationTokenSource _changeTokenSource;
    private TOption _option;

    public IChangeToken OptionChangeToken { get; private set; }

    public UriOptionsLoaderService(
        ILogger<UriOptionsLoaderService<TOption>> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<UriOptions> originOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _originOptions = originOptions.Value;
        _optionProperties = typeof(TOption).GetProperties();
        _changeTokenSource = new CancellationTokenSource();
        OptionChangeToken = new CancellationChangeToken(_changeTokenSource.Token);

        _option = LoadRemoteOptions(CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Configure(TOption target)
    {
        foreach (var property in _optionProperties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var targetValue = property.GetValue(target);
                var sourceValue = property.GetValue(_option);

                if (!Equals(targetValue, sourceValue))
                {
                    property.SetValue(target, sourceValue);
                }
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_originOptions.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var newOptions = await LoadRemoteOptions(stoppingToken);

                if (!newOptions.Equals(_option))
                {
                    _option = newOptions;
                    await NotifyChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while loading options from {uri}", _originOptions.ConfigurationUri);
            }
        }
    }

    private async Task NotifyChanges()
    {
        var newTokenSource = new CancellationTokenSource();
        OptionChangeToken = new CancellationChangeToken(newTokenSource.Token);
        await _changeTokenSource.CancelAsync();
        _changeTokenSource.Dispose();
        _changeTokenSource = newTokenSource;
    }

    private async Task<TOption> LoadRemoteOptions(CancellationToken stoppingToken)
    {
        if (_originOptions.ConfigurationUri.Scheme == Uri.UriSchemeHttp ||
    _originOptions.ConfigurationUri.Scheme == Uri.UriSchemeHttps)
        {
            return await LoadFromHttp(stoppingToken);
        }
        else if (_originOptions.ConfigurationUri.Scheme == Uri.UriSchemeFile)
        {
            return await LoadFromFile(stoppingToken);
        }
        else
        {
            throw new NotSupportedException($"Unsupported URI scheme: {_originOptions.ConfigurationUri.Scheme}");
        }
    }

    private async Task<TOption> LoadFromHttp(CancellationToken stoppingToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(_originOptions.ConfigurationUri, stoppingToken);
        response.EnsureSuccessStatusCode();
        var stringContent = await response.Content.ReadAsStringAsync(stoppingToken);

        return Deserialize<TOption>(stringContent, _originOptions.ConfigurationUri.GetExtension());
    }
    private async Task<TOption> LoadFromFile(CancellationToken stoppingToken)
    {
        var stringContent = await File.ReadAllTextAsync(_originOptions.ConfigurationUri.LocalPath, stoppingToken);

        return Deserialize<TOption>(stringContent, _originOptions.ConfigurationUri.GetExtension());
    }

    private static T Deserialize<T>(string content, string extension)
    {
        switch (extension)
        {
            case ".json":
                return JsonSerializer.Deserialize<T>(content)
                    ?? throw new JsonException("Failed to read options from response.");

            case ".yaml":
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                return deserializer.Deserialize<T>(content)
                    ?? throw new YamlDotNet.Core.SyntaxErrorException("Failed to read options from response.");
            default:
                throw new NotSupportedException($"Unsupported file extension: {extension}");
        }
    }

    public override void Dispose()
    {
        _changeTokenSource.Dispose();
        base.Dispose();
    }
}

public static class UriExtensions
{
    public static string GetExtension(this Uri uri)
    {
        return Path.GetExtension(uri.Segments[^1]).ToLowerInvariant();
    }
}
