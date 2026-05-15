using Concoction.Application.Abstractions;
using Concoction.Application.Accounts;
using Concoction.Application.ApiKeys;
using Concoction.Application.Chat;
using Concoction.Application.Compliance;
using Concoction.Application.Configuration;
using Concoction.Application.Constraints;
using Concoction.Application.Generation;
using Concoction.Application.Governance;
using Concoction.Application.Orchestration;
using Concoction.Application.Planning;
using Concoction.Application.Projects;
using Concoction.Application.Schema;
using Concoction.Application.Workflows;
using Concoction.Application.Workspaces;
using Concoction.Infrastructure.Configuration;
using Concoction.Infrastructure.Export;
using Concoction.Infrastructure.Repositories;
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
        services.AddSingleton<ISchemaReviewService, SchemaReviewService>();
        services.AddSingleton<IGenerationPlanService, GenerationPlanService>();
        services.AddSingleton<RunLifecycleService>();

        // #26 — accounts
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<IInvitationService, InvitationService>();
        services.AddSingleton<IUserProfileService, UserProfileService>();

        // #27 — governance
        services.AddSingleton<IAccountGroupService, AccountGroupService>();
        services.AddSingleton<IAllowedDomainService, AllowedDomainService>();
        services.AddSingleton<IAuditLogService, AuditLogService>();

        // #28 — workspaces
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddSingleton<IConnectionCatalogService, ConnectionCatalogService>();
        services.AddSingleton<IInstructionVersionService, InstructionVersionService>();

        // #29 — projects
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IProjectDatabaseCatalog, ProjectDatabaseCatalog>();

        // #30 — chat
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<IAgentChatService, AgentChatService>();

        // #31 — API keys
        services.AddSingleton<IApiKeyService, ApiKeyService>();

        // #24 — workflows
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddSingleton<ISkillRegistry, SkillRegistryService>();
        services.AddSingleton<IApiContractIngestionService, OpenApiContractIngestionService>();

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
                    $"Unsupported schema provider '{options.Provider}'. Supported values: sqlite, postgres, postgresql.")
            };
        });

        services.AddSingleton<IExporter, CsvExporter>();
        services.AddSingleton<IExporter, JsonExporter>();
        services.AddSingleton<IExporter, SqlExporter>();

        services.AddSingleton<IArtifactStore>(sp =>
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "concoction-artifacts");
            return new FileSystemArtifactStore(baseDir);
        });

        // In-memory repositories (default; swap for EF Core adapters in production)
        services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();
        services.AddSingleton<IRunRepository, InMemoryRunRepository>();
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();
        services.AddSingleton<IApiKeyStore, InMemoryApiKeyStore>();
        services.AddSingleton<ISecretProvider, EnvSecretProvider>();

        return services;
    }
}
