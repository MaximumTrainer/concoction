using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Application.Workspaces;

public sealed class InstructionVersionService(IWorkspaceService workspaceService) : IInstructionVersionService
{
    private readonly List<InstructionVersion> _versions = [];

    public async Task<InstructionVersion> SaveAsync(Guid workspaceId, string content, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        await RequireEditorAsync(workspaceId, createdByUserId, cancellationToken).ConfigureAwait(false);

        var version = _versions.Where(v => v.WorkspaceId == workspaceId).Select(v => v.Version).DefaultIfEmpty(0).Max() + 1;
        var entry = new InstructionVersion(Guid.NewGuid(), workspaceId, version, content, createdByUserId, DateTimeOffset.UtcNow);
        _versions.Add(entry);
        return entry;
    }

    public Task<InstructionVersion?> GetLatestAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var latest = _versions.Where(v => v.WorkspaceId == workspaceId).OrderByDescending(v => v.Version).FirstOrDefault();
        return Task.FromResult(latest);
    }

    public Task<IReadOnlyList<InstructionVersion>> GetHistoryAsync(Guid workspaceId, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var history = _versions.Where(v => v.WorkspaceId == workspaceId).OrderByDescending(v => v.Version).Take(pageSize).ToArray();
        return Task.FromResult<IReadOnlyList<InstructionVersion>>(history);
    }

    private async Task RequireEditorAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var role = await workspaceService.GetEffectiveRoleAsync(workspaceId, userId, cancellationToken).ConfigureAwait(false);
        if (role < WorkspaceRole.Editor)
        {
            throw new UnauthorizedAccessException("Workspace Editor or Admin role required.");
        }
    }
}
