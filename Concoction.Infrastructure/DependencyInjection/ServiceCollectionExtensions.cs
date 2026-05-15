using Concoction.Application.Abstractions;
using Concoction.Application.Compliance;
using Concoction.Application.Configuration;
using Concoction.Application.Constraints;
using Concoction.Application.Generation;
using Concoction.Application.Orchestration;
using Concoction.Application.Planning;
using Concoction.Application.Schema;
using Concoction.Infrastructure.Configuration;
using Concoction.Infrastructure.Export;
using Concoction.Infrastructure.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Concoction.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcoctionApplication(this IServiceCollection services, long seed)
    {
        services.AddSingleton<IRandomService>(_ => new DeterministicRandomService(seed));

        // Register the concrete GeneratorRegistry as a singleton, initializing defaults in the factory.
        // Both IGeneratorRegistry and IValueGeneratorDispatcher forward to the same instance to avoid
        // a circular dependency and fragile cast.
        services.AddSingleton<GeneratorRegistry>(sp =>
        {
            var random = sp.GetRequiredService<IRandomService>();
            var registry = new GeneratorRegistry();
            registry.RegisterDefaults(random);
            return registry;
        });
        services.AddSingleton<IGeneratorRegistry>(sp => sp.GetRequiredService<GeneratorRegistry>());
        services.AddSingleton<IValueGeneratorDispatcher>(sp => sp.GetRequiredService<GeneratorRegistry>());

        services.AddSingleton<IConstraintEvaluator, ConstraintEvaluator>();
        services.AddSingleton<IGenerationPlanner, DependencyGraphPlanner>();
        services.AddSingleton<ISensitiveFieldPolicy, DefaultSensitiveFieldPolicy>();
        services.AddSingleton<IRuleConfigurationService, RuleConfigurationService>();
        services.AddSingleton<ISchemaDiscoveryService, SchemaDiscoveryService>();
        services.AddSingleton<IRowMaterializer, ReferentialRowMaterializer>();
        services.AddSingleton<ISyntheticDataOrchestrator, SyntheticDataOrchestrator>();

        return services;
    }

    public static IServiceCollection AddConcoctionInfrastructure(this IServiceCollection services, Action<SchemaProviderOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ISchemaProvider>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SchemaProviderOptions>>().Value;
            return options.Provider.ToLowerInvariant() switch
            {
                "postgres" or "postgresql" => ActivatorUtilities.CreateInstance<PostgreSqlSchemaProvider>(sp),
                "sqlite" => ActivatorUtilities.CreateInstance<SqliteSchemaProvider>(sp),
                _ => throw new InvalidOperationException(
                    $"Unsupported schema provider '{options.Provider}'. Supported values: sqlite, postgres.")
            };
        });

        services.AddSingleton<IExporter, CsvExporter>();
        services.AddSingleton<IExporter, JsonExporter>();

        return services;
    }
}
