using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ProjectOrigin.ServiceCommon.UriOptionsLoader;

public static class ConfigureExtensions
{
    /// <summary>
    /// Configures the UriOptionsLoaderService to load the Uri json object into the specified type
    /// </summary>
    /// <typeparam name="TOption">The type to load the Uri json object into</typeparam>
    /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection.</param>
    /// <param name="configSectionPath">The name of the configuration section to bind configuration from</param>
    public static void ConfigureUriOptionsLoader<TOption>(
        this IServiceCollection services,
        string configSectionPath) where TOption : class
    {
        ConfigureUriOptionsLoader<TOption>(services, configSectionPath, builder => builder);
    }

    /// <summary>
    /// Configures the UriOptionsLoaderService to load the Uri json object into the specified type
    /// </summary>
    /// <typeparam name="TOption">The type to load the Uri json object into</typeparam>
    /// <param name="services">The Microsoft.Extensions.DependencyInjection.IServiceCollection.</param>
    /// <param name="configSectionPath">The name of the configuration section to bind configuration from</param>
    /// <param name="deserializerBuilderConfigure">A function to configure the YamlDotNet.Serialization.DeserializerBuilder</param>
    public static void ConfigureUriOptionsLoader<TOption>(
        this IServiceCollection services,
        string configSectionPath,
        Func<DeserializerBuilder, DeserializerBuilder> deserializerBuilderConfigure) where TOption : class
    {
        services.AddOptions<UriOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton<IDeserializer>(
            deserializerBuilderConfigure(new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance))
            .Build());

        services.AddSingleton<IOptionsValidator<TOption>, OptionsValidator<TOption>>();
        services.AddSingleton<IConfigureOptions<TOption>, UriOptionsConfigure<TOption>>();
        services.AddSingleton<IOptionsChangeTokenSource<TOption>, UriOptionsChangeTokenSource<TOption>>();
        services.AddSingleton<UriOptionsLoaderService<TOption>>();
        services.AddHostedService(provider => provider.GetRequiredService<UriOptionsLoaderService<TOption>>());
    }
}
