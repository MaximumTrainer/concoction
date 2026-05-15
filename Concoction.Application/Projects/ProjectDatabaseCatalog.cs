using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Application.Projects;

public sealed class ProjectDatabaseCatalog(IWorkspaceService workspaceService) : IProjectDatabaseCatalog
{
    private readonly List<ProjectDatabase> _databases = [];

    public async Task<ProjectDatabase> AddAsync(AddDatabaseCommand command, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectWorkspaceAsync(command.ProjectId, cancellationToken).ConfigureAwait(false);
        await RequireEditorAsync(project, command.RequestingUserId, cancellationToken).ConfigureAwait(false);

        var db = new ProjectDatabase(Guid.NewGuid(), command.ProjectId, command.Name, command.Type, command.Provider, "active", command.ConnectionRefId, DateTimeOffset.UtcNow);
        _databases.Add(db);
        return db;
    }

    public async Task<IReadOnlyList<ProjectDatabase>> ListAsync(Guid projectId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false);
        var role = await workspaceService.GetEffectiveRoleAsync(project, requestingUserId, cancellationToken).ConfigureAwait(false);
        if (!role.HasValue) throw new UnauthorizedAccessException("Access denied.");
        return _databases.Where(d => d.ProjectId == projectId).ToArray();
    }

    public async Task RemoveAsync(Guid databaseId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var db = _databases.Find(d => d.Id == databaseId);
        if (db is null) throw new InvalidOperationException($"Database '{databaseId}' not found.");
        var project = await GetProjectWorkspaceAsync(db.ProjectId, cancellationToken).ConfigureAwait(false);
        await RequireEditorAsync(project, requestingUserId, cancellationToken).ConfigureAwait(false);
        _databases.RemoveAll(d => d.Id == databaseId);
    }

    // Returns the WorkspaceId for a project — placeholder for project repository look-up.
    // In the full persistence implementation this would query IProjectRepository.
    private static Task<Guid> GetProjectWorkspaceAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult(projectId); // substituted by real repository in infrastructure

    private async Task RequireEditorAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await workspaceService.GetEffectiveRoleAsync(workspaceId, userId, cancellationToken).ConfigureAwait(false);
        if (role < WorkspaceRole.Editor)
        {
            throw new UnauthorizedAccessException("Workspace Editor or Admin role required.");
        }
    }
}
