using Concoction.Domain.Enums;
using Concoction.Domain.Models;

namespace Concoction.Application.Abstractions;

public interface ISchemaProvider
{
    string ProviderName { get; }
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface ISchemaDiscoveryService
{
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}

public sealed record GeneratorContext(
    string Table,
    string Column,
    DataKind DataKind,
    int RowIndex,
    RuleConfiguration? Rules,
    IReadOnlyDictionary<string, object?> CurrentRow,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReferencePool);

public interface IValueGenerator<in TContext, TValue>
{
    ValueTask<TValue> GenerateAsync(TContext context, CancellationToken cancellationToken = default);
}

public interface IValueGeneratorDispatcher
{
    ValueTask<object?> GenerateAsync(GeneratorContext context, CancellationToken cancellationToken = default);
}

public interface IGeneratorRegistry
{
    void Register(DataKind kind, Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator, string? strategy = null);
    bool TryResolve(DataKind kind, string? strategy, out Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator);
}

public interface IRandomService
{
    int NextInt(string scope, int minInclusive, int maxExclusive);
    long NextLong(string scope, long minInclusive, long maxExclusive);
    double NextDouble(string scope);
    string NextToken(string scope, int length);
    Guid NextGuid(string scope);
}

public interface IConstraintEvaluator
{
    IReadOnlyList<ValidationIssue> Evaluate(TableSchema table, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);
}

public interface IGenerationPlanner
{
    GenerationPlan BuildPlan(DatabaseSchema schema);
}

public interface IRowMaterializer
{
    Task<TableData> MaterializeAsync(
        TableSchema table,
        int rowCount,
        RuleConfiguration? rules,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> keyPool,
        CancellationToken cancellationToken = default);
}

public interface IExporter
{
    string Name { get; }
    Task ExportAsync(IReadOnlyList<TableData> tables, string target, CancellationToken cancellationToken = default);
}

public interface ISensitiveFieldPolicy
{
    ComplianceDecision Evaluate(string table, ColumnSchema column, ComplianceProfile profile = ComplianceProfile.Default);
}

public interface IRuleConfigurationService
{
    RuleConfiguration Load(string path);
    IReadOnlyList<string> Validate(RuleConfiguration configuration);
    RuleConfiguration Merge(RuleConfiguration defaults, RuleConfiguration schemaDerived, RuleConfiguration user);
}

public interface ISyntheticDataOrchestrator
{
    Task<(GenerationResult Result, RunSummary Summary)> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default);
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}

// ── #13: Schema profiling ports ───────────────────────────────────────────────

public interface ISchemaProfiler
{
    Task<ProfileSnapshot> ProfileAsync(DatabaseSchema schema, CancellationToken cancellationToken = default);
}

public interface IProfileSnapshotRepository
{
    Task SaveAsync(ProfileSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<ProfileSnapshot?> GetLatestAsync(string databaseName, CancellationToken cancellationToken = default);
    Task<ProfileSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISchemaSnapshotRepository
{
    Task SaveAsync(SchemaSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<SchemaSnapshot?> GetLatestAsync(string databaseName, CancellationToken cancellationToken = default);
    Task<SchemaSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISchemaReviewService
{
    SchemaReviewReport Review(DatabaseSchema schema);
}

// ── #14: Strategy registry and plan engine ports ──────────────────────────────

public sealed record StrategyOverride(string? GlobalStrategy, IReadOnlyDictionary<string, string>? TableStrategies, IReadOnlyDictionary<string, string>? ColumnStrategies);

public sealed record ColumnPlanEntry(string Table, string Column, DataKind DataKind, string ResolvedStrategy, string StrategyProvenance);

public sealed record PlanDiagnosticsReport(IReadOnlyList<ColumnPlanEntry> Columns, IReadOnlyList<string> Warnings);

public interface IStrategyRegistry
{
    void Register(string strategyName, DataKind kind, Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator);
    bool TryResolve(string strategyName, DataKind kind, out Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator);
    IReadOnlyList<string> GetRegisteredStrategies(DataKind kind);
}

public interface IGenerationPlanService
{
    PlanDiagnosticsReport BuildDiagnosticsReport(DatabaseSchema schema, RuleConfiguration? rules = null);
}

// ── #22: Run management ports ─────────────────────────────────────────────────

public interface IRunRepository
{
    Task<DatasetRun> CreateAsync(DatasetRun run, CancellationToken cancellationToken = default);
    Task<DatasetRun> UpdateAsync(DatasetRun run, CancellationToken cancellationToken = default);
    Task<DatasetRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatasetRun>> ListAsync(int pageSize = 20, int page = 1, CancellationToken cancellationToken = default);
}

public interface IArtifactStore
{
    Task<string> StoreAsync(string runId, string name, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> RetrieveAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
}

// ── #26: Account foundation ports ────────────────────────────────────────────

public sealed record CreateAccountCommand(string Name, Guid OwnerId);
public sealed record InviteUserCommand(Guid AccountId, Guid InvitedByUserId, string InviteeEmail, TimeSpan Expiry);
public sealed record AcceptInvitationCommand(string Token, Guid UserId);
public sealed record RevokeInvitationCommand(Guid InvitationId, Guid RequestingUserId);
public sealed record UpdateProfileCommand(Guid UserId, string DisplayName);

public interface IAccountService
{
    Task<Account> CreateAccountAsync(CreateAccountCommand command, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountMembership>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task EnsureMemberAsync(Guid accountId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IInvitationService
{
    Task<Invitation> InviteAsync(InviteUserCommand command, CancellationToken cancellationToken = default);
    Task<AccountMembership> AcceptAsync(AcceptInvitationCommand command, CancellationToken cancellationToken = default);
    Task RevokeAsync(RevokeInvitationCommand command, CancellationToken cancellationToken = default);
}

public interface IUserProfileService
{
    Task<UserProfile> GetOrCreateAsync(Guid userId, string email, string displayName, CancellationToken cancellationToken = default);
    Task<UserProfile> UpdateAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAccountRepository
{
    Task<Account> SaveAsync(Account account, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountMembership> AddMemberAsync(AccountMembership membership, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountMembership>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<AccountMembership?> GetMembershipAsync(Guid accountId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<UserProfile> SaveAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Invitation> SaveInvitationAsync(Invitation invitation, CancellationToken cancellationToken = default);
    Task<Invitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Invitation?> GetInvitationByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

// ── #27: Governance ports ─────────────────────────────────────────────────────

public interface IAccountGroupService
{
    Task<AccountGroup> CreateGroupAsync(Guid accountId, string name, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task AddGroupMemberAsync(Guid groupId, Guid userId, Guid requestingUserId, Guid accountId, CancellationToken cancellationToken = default);
    Task RemoveGroupMemberAsync(Guid groupId, Guid userId, Guid requestingUserId, Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountGroup>> ListGroupsAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public interface IAllowedDomainService
{
    Task<AllowedDomain> AddDomainAsync(Guid accountId, string domain, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task RemoveDomainAsync(Guid domainId, Guid requestingUserId, Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> IsEmailAllowedAsync(Guid accountId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllowedDomain>> ListDomainsAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public sealed record AuditPage(IReadOnlyList<AuditEvent> Events, int TotalCount, int Page, int PageSize);

public interface IAuditLogService
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<AuditPage> QueryAsync(Guid accountId, int page = 1, int pageSize = 50, string? actionFilter = null, CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> QueryAsync(Guid accountId, int skip, int take, string? actionFilter, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid accountId, string? actionFilter, CancellationToken cancellationToken = default);
}

// ── #28: Workspace ports ──────────────────────────────────────────────────────

public sealed record CreateWorkspaceCommand(Guid AccountId, string Name, Guid CreatedByUserId);
public sealed record GrantWorkspaceAccessCommand(Guid WorkspaceId, Guid PrincipalId, bool IsGroup, WorkspaceRole Role, Guid RequestingUserId);

public interface IWorkspaceService
{
    Task<Workspace> CreateAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByIdAsync(Guid workspaceId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task GrantAccessAsync(GrantWorkspaceAccessCommand command, CancellationToken cancellationToken = default);
    Task RevokeAccessAsync(Guid workspaceId, Guid principalId, bool isGroup, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<WorkspaceRole?> GetEffectiveRoleAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IConnectionCatalogService
{
    Task<Connection> AddConnectionAsync(Guid workspaceId, string name, string provider, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Connection> UpdateStatusAsync(Guid connectionId, string status, Guid requestingUserId, Guid workspaceId, CancellationToken cancellationToken = default);
    Task RemoveConnectionAsync(Guid connectionId, Guid requestingUserId, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Connection>> ListAsync(Guid workspaceId, Guid requestingUserId, CancellationToken cancellationToken = default);
}

public interface ISecretProvider
{
    Task<string> ResolveAsync(string secretName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string secretName, CancellationToken cancellationToken = default);
}

public interface IInstructionVersionService
{
    Task<InstructionVersion> SaveAsync(Guid workspaceId, string content, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<InstructionVersion?> GetLatestAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstructionVersion>> GetHistoryAsync(Guid workspaceId, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── #29: Project ports ────────────────────────────────────────────────────────

public sealed record CreateProjectCommand(Guid WorkspaceId, string Name, Guid CreatedByUserId);
public sealed record AddDatabaseCommand(Guid ProjectId, string Name, ProjectDatabaseType Type, string Provider, Guid? ConnectionRefId, Guid RequestingUserId);

public interface IProjectService
{
    Task<Project> CreateAsync(CreateProjectCommand command, CancellationToken cancellationToken = default);
    Task<Project> RenameAsync(Guid projectId, string newName, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Project> ArchiveAsync(Guid projectId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(Guid projectId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> ListAsync(Guid workspaceId, Guid requestingUserId, CancellationToken cancellationToken = default);
}

public interface IProjectDatabaseCatalog
{
    Task<ProjectDatabase> AddAsync(AddDatabaseCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDatabase>> ListAsync(Guid projectId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid databaseId, Guid requestingUserId, CancellationToken cancellationToken = default);
}

// ── #30: Agent chat ports ─────────────────────────────────────────────────────

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(string inputJson, Guid sessionId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? Resolve(string toolName);
    IReadOnlyList<string> AllowedTools(Guid workspaceId);
}

public sealed record CreateChatSessionCommand(Guid WorkspaceId, Guid? ProjectId, Guid UserId, string Name, ChatMode Mode = ChatMode.Guided);
public sealed record SendMessageCommand(Guid SessionId, Guid UserId, string Content);

public interface IAgentChatService
{
    Task<ChatSession> CreateSessionAsync(CreateChatSessionCommand command, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ChatSession> ArchiveSessionAsync(Guid sessionId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ChatSession> ChangeMode(Guid sessionId, ChatMode mode, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(SendMessageCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid sessionId, Guid requestingUserId, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<string> GetComposedInstructionsAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface ISessionRepository
{
    Task<ChatSession> SaveAsync(ChatSession session, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChatMessage> SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid sessionId, int skip, int take, CancellationToken cancellationToken = default);
    Task<ToolInvocation> SaveInvocationAsync(ToolInvocation invocation, CancellationToken cancellationToken = default);
}

// ── #31: API key ports ────────────────────────────────────────────────────────

public sealed record CreateApiKeyCommand(Guid AccountId, string Name, IReadOnlyList<string> Scopes, TimeSpan? Expiry = null);

public interface IApiKeyService
{
    /// <summary>Creates a new API key. Returns the record and the plaintext secret (only visible once).</summary>
    Task<(ApiKey Key, string PlaintextSecret)> CreateAsync(CreateApiKeyCommand command, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ApiKey> RevokeAsync(Guid keyId, Guid requestingUserId, Guid accountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> ListAsync(Guid accountId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<ApiKey?> ValidateAsync(string plaintextSecret, CancellationToken cancellationToken = default);
}

public interface IApiKeyStore
{
    Task<ApiKey> SaveAsync(ApiKey key, CancellationToken cancellationToken = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> ListByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<ApiKey?> FindByHashAsync(string hashedSecret, CancellationToken cancellationToken = default);
    Task<ApiKey> UpdateAsync(ApiKey key, CancellationToken cancellationToken = default);
}

// ── #24: Workflow ports ───────────────────────────────────────────────────────

public sealed record CreateWorkflowCommand(Guid WorkspaceId, string Name, IReadOnlyList<WorkflowStepDefinition> Steps);
public sealed record WorkflowStepDefinition(int StepOrder, string StepType, string? Configuration);

public interface IWorkflowService
{
    Task<Workflow> CreateAsync(CreateWorkflowCommand command, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<WorkflowRun> RunAsync(Guid workflowId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<WorkflowRun?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowStepRun>> GetStepRunsAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<Workflow> DisableAsync(Guid workflowId, Guid requestingUserId, CancellationToken cancellationToken = default);
}

public interface ISkillRegistry
{
    Task RegisterSkillAsync(Skill skill, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<Skill?> GetSkillAsync(Guid skillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Skill>> ListSkillsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> IsToolAllowedAsync(Guid skillId, string toolName, CancellationToken cancellationToken = default);
}

public interface IApiContractIngestionService
{
    Task<IReadOnlyList<GeneratedApiEndpoint>> IngestAsync(string openApiJson, Guid workspaceId, Guid requestingUserId, CancellationToken cancellationToken = default);
}

